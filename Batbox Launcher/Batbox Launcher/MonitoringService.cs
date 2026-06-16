using System.Diagnostics;
using System.Net.NetworkInformation;

namespace BatboxLauncher
{
    public class MonitoringService
    {
        private readonly Func<AppConfig> _getConfig;
        private readonly Logger _log;
        private bool _hasLaunchedKiosk = false; // Track if we've launched Kiosk at least once

        public MonitoringService(Func<AppConfig> getConfig, Logger log)
        {
            _getConfig = getConfig;
            _log = log;
        }

        /// <summary>
        /// Reset the launch state (call when starting a new launch session)
        /// </summary>
        public void Reset()
        {
            _hasLaunchedKiosk = false;
        }

        /// <summary>
        /// Runs one monitoring cycle. 
        /// Returns: (success, shouldStop)
        /// success = all checks passed and app is running
        /// shouldStop = fatal error, don't retry
        /// </summary>
        public async Task<(bool success, bool shouldStop)> TickAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cfg = _getConfig();

            _log.Info("Checking conditions...");

            string? monitorError = null;

            // Log monitor setup
            var allScreens = Screen.AllScreens;
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                _log.Error("No primary monitor detected!");
                return (false, shouldStop: true);
            }
            int monitorCount = allScreens.Length;
            
            _log.Info($"Monitors detected: {monitorCount}");
            foreach (var screen in allScreens.OrderBy(s => s.Bounds.X))
            {
                string isPrimary = screen.Primary ? " [PRIMARY]" : "";
                _log.Info($"  {screen.DeviceName}: {screen.Bounds.Width}x{screen.Bounds.Height} at ({screen.Bounds.X},{screen.Bounds.Y}){isPrimary}");
            }
            
            // Check if primary is leftmost
            bool primaryIsLeftmost = allScreens.All(s => s.Bounds.X >= primaryScreen.Bounds.X);
            if (primaryIsLeftmost)
                _log.Info("Primary monitor is LEFTMOST - OK");
            else
                _log.Warn("Primary monitor is NOT leftmost - windows may not position correctly!");

            if (cfg.SkipMonitorCheck)
            {
                _log.Warn("Skipping monitor count check (testing mode).");
            }
            else
            {
                if (monitorCount < cfg.MinMonitors)
                    monitorError = $"Monitor count low ({monitorCount} detected - Minimum required is {cfg.MinMonitors}).";
            }

            using var ping = new Ping();
            var failed = new List<string>();

            foreach (var d in cfg.Devices.Where(d => !string.IsNullOrWhiteSpace(d.Ip)))
            {
                if (d.Skip)
                {
                    _log.Warn($"Skipping ping for {d.Name} ({d.Ip})");
                    continue;
                }

                bool ok = await PingOnceAsync(ping, d.Ip, 1000);
                if (ok) _log.Info($"{d.Name} ({d.Ip}) - OK");
                else
                {
                    _log.Warn($"{d.Name} ({d.Ip}) - NOT FOUND");
                    failed.Add(d.Name);
                }
            }

            string? pingError = null;
            if (failed.Count > 0)
                pingError = $"Devices not found: {string.Join(", ", failed)}";

            // Get the launch path (with fallback to LocalAppData)
            string? launchPath = GetLaunchPath(cfg);
            if (launchPath == null)
            {
                _log.Error($"Shortcut not found: {cfg.LnkPath}");
                _log.Error($"Fallback not found: {GetFallbackKioskPath()}");
                _log.Error("Neither the configured shortcut nor the fallback Kiosk.exe was found.");
                return (false, shouldStop: true);
            }

            if (monitorError != null || pingError != null)
            {
                _log.Error("FAILED - Will retry...");
                if (monitorError != null) _log.Error("  " + monitorError);
                if (pingError != null) _log.Error("  " + pingError);
                return (false, shouldStop: false);
            }

            // Check if already running
            bool kioskRunning = IsProcessRunning(cfg.KioskExeName);
            bool baseballRunning = IsProcessRunning(cfg.BaseballExeName);

            if (kioskRunning && baseballRunning)
            {
                _log.Info($"{cfg.KioskExeName} and {cfg.BaseballExeName} are already running.");
                EnsureOnPrimary(cfg.KioskExeName);
                EnsureOnPrimary(cfg.BaseballExeName);
                
                if (!primaryIsLeftmost)
                    _log.Warn("Reminder: Primary monitor is NOT leftmost - check window positions!");
                
                _hasLaunchedKiosk = true;
                return (true, shouldStop: true); // Success - both running
            }

            // If we already launched Kiosk but it's no longer running, user closed it - don't relaunch
            if (_hasLaunchedKiosk && !kioskRunning)
            {
                _log.Info($"{cfg.KioskExeName} was closed by user. Stopping monitoring (not relaunching).");
                return (false, shouldStop: true); // Stop monitoring, but NOT success (don't exit app)
            }

            if (!kioskRunning)
            {
                _log.Info($"All conditions met. Launching: {launchPath}");
                if (!LaunchApplication(launchPath))
                {
                    return (false, shouldStop: true);
                }
                _hasLaunchedKiosk = true; // Mark that we've launched
            }

            // Wait for both processes to start (up to 30 seconds)
            _log.Info("Waiting for apps to start...");
            bool kioskLogged = kioskRunning;
            bool baseballLogged = baseballRunning;
            
            for (int i = 0; i < 30; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);
                
                kioskRunning = IsProcessRunning(cfg.KioskExeName);
                baseballRunning = IsProcessRunning(cfg.BaseballExeName);
                
                if (kioskRunning && !kioskLogged)
                {
                    _log.Info($"{cfg.KioskExeName} started.");
                    kioskLogged = true;
                }
                if (baseballRunning && !baseballLogged)
                {
                    _log.Info($"{cfg.BaseballExeName} started.");
                    baseballLogged = true;
                }
                
                if (kioskRunning && baseballRunning)
                    break;
            }

            // Final check
            kioskRunning = IsProcessRunning(cfg.KioskExeName);
            baseballRunning = IsProcessRunning(cfg.BaseballExeName);

            if (!kioskRunning || !baseballRunning)
            {
                // If Kiosk started but then stopped, don't retry - user closed it
                if (kioskLogged && !kioskRunning)
                {
                    _log.Warn($"{cfg.KioskExeName} stopped running (closed by user or updated). Stopping.");
                    _hasLaunchedKiosk = true; // Mark as launched so we don't relaunch
                    return (false, shouldStop: true); // Stop immediately, don't retry
                }
                
                // Still waiting for apps to start
                if (!kioskRunning)
                    _log.Debug($"Still waiting for {cfg.KioskExeName} to start...");
                if (!baseballRunning && !baseballLogged)
                    _log.Debug($"Still waiting for {cfg.BaseballExeName} to start...");
                else if (!baseballRunning && baseballLogged)
                    _log.Warn($"{cfg.BaseballExeName} stopped running.");
                    
                return (false, shouldStop: false); // Not ready yet, will retry
            }

            // Both running - wait for windows to fully initialize
            _log.Info("Both apps running. Waiting for windows to initialize...");
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(3000, cancellationToken); // Give windows 3 seconds to fully initialize
            
            // Log all visible windows for debugging
            _log.Info("=== All visible windows ===");
            foreach (var win in Win32WindowTools.GetAllVisibleWindows())
            {
                _log.Info($"  {win}");
            }
            _log.Info("===========================");
            
            _log.Info("Moving windows to primary monitor...");
            EnsureOnPrimary(cfg.KioskExeName);
            EnsureOnPrimary(cfg.BaseballExeName);

            // Remind about monitor setup if not ideal
            if (!primaryIsLeftmost)
                _log.Warn("Reminder: Primary monitor is NOT leftmost - check window positions!");

            return (true, shouldStop: true); // Success! Stop monitoring.
        }

        private void EnsureOnPrimary(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName)) return;
            
            var processes = FindProcessesByExeName(exeName).ToList();
            if (processes.Count == 0)
            {
                _log.Warn($"{exeName} - No process found.");
                return;
            }

            foreach (var p in processes)
            {
                var result = Win32WindowTools.TryMoveMainWindowToPrimary(p, out string debugInfo);
                
                switch (result)
                {
                    case MoveResult.AlreadyOnPrimary:
                        _log.Info($"{exeName} - Already on primary monitor. ({debugInfo})");
                        break;
                    case MoveResult.MovedToPrimary:
                        _log.Info($"{exeName} - Moved to primary monitor. ({debugInfo})");
                        break;
                    case MoveResult.NoWindowFound:
                        _log.Warn($"{exeName} - Window not found. ({debugInfo})");
                        break;
                    case MoveResult.FailedToMove:
                        _log.Warn($"{exeName} - Failed to move. ({debugInfo})");
                        break;
                }
            }
        }

        private static bool IsProcessRunning(string exeName) => FindProcessesByExeName(exeName).Any();

        private static IEnumerable<Process> FindProcessesByExeName(string exeName)
        {
            var name = exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? exeName[..^4] : exeName;
            return Process.GetProcessesByName(name);
        }

        private static async Task<bool> PingOnceAsync(Ping ping, string ip, int timeoutMs)
        {
            try
            {
                var reply = await ping.SendPingAsync(ip, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        /// <summary>
        /// Launches an application using multiple fallback methods
        /// </summary>
        private bool LaunchApplication(string path)
        {
            // Method 1: Direct launch with working directory (best for exe files)
            try
            {
                string? workingDir = Path.GetDirectoryName(path);
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = workingDir ?? "",
                    UseShellExecute = true
                };
                Process.Start(psi);
                _log.Info("Launched successfully (direct with working dir)");
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Direct launch failed: {ex.Message}");
            }

            // Method 2: Use cmd /c start (very reliable)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                _log.Info("Launched successfully (cmd /c start)");
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"cmd /c start failed: {ex.Message}");
            }

            // Method 3: Use explorer.exe
            try
            {
                Process.Start("explorer.exe", $"\"{path}\"");
                _log.Info("Launched successfully (explorer)");
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Explorer launch failed: {ex.Message}");
            }

            _log.Error("All launch methods failed!");
            return false;
        }

        /// <summary>
        /// Gets the path to launch. First tries configured LnkPath, then falls back to LocalAppData Kiosk.exe
        /// </summary>
        private string? GetLaunchPath(AppConfig cfg)
        {
            // Try configured shortcut first
            if (File.Exists(cfg.LnkPath))
            {
                return cfg.LnkPath;
            }

            // Try fallback path: %LOCALAPPDATA%\Takoha.Kiosk\current\Kiosk.exe
            string fallbackPath = GetFallbackKioskPath();
            if (File.Exists(fallbackPath))
            {
                _log.Warn($"Configured shortcut not found, using fallback: {fallbackPath}");
                return fallbackPath;
            }

            return null;
        }

        private static string GetFallbackKioskPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Takoha.Kiosk", "current", "Kiosk.exe");
        }
    }
}
