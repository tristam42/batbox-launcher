using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace BatboxLauncher
{
    public partial class Form1 : Form
    {
        private AppConfig _config = null!;
        private Logger _log = null!;
        private MonitoringService _monitor = null!;
        private System.Windows.Forms.Timer _timer = null!;
        private System.Windows.Forms.Timer _pingTimer = null!;
        private System.Windows.Forms.Timer _gameProcessTimer = null!;
        private System.Windows.Forms.Timer? _serverRetryTimer;
        private BindingList<DeviceConfig> _deviceList = null!;
        private CancellationTokenSource? _cts;
        private bool _settingsExpanded = false;
        private bool _isExecuting = false;
        private const int CollapsedHeight = 518;
        private const int ExpandedHeight = 775;

        // Device status tracking
        private Dictionary<string, Panel> _deviceIndicators = new();
        private Dictionary<string, bool> _deviceStatus = new();
        private bool _monitorCheckPassed = false;
        private bool _initialCheckComplete = false;
        private bool _serverOnline = false;
        private (bool online, bool? hasInternet)? _lastServerState = null; // Track previous server state for log suppression
        private bool? _lastAllGreenState = null; // Track previous state to suppress duplicate logs
        private bool _isFirstStatusCheck = true; // Don't log status on startup
        private Dictionary<string, bool> _lastDeviceStatus = new(); // Track previous device status for log suppression
        private (int count, bool primaryOnLeft)? _lastMonitorState = null; // Track previous monitor state
        private bool? _lastTrayMonitorOkState = null; // Track tray monitor notification state
        private bool _launchCompleted = false; // True after successful launch sequence
        private bool? _lastGameProcessState = null; // Track game process state for log suppression
        private TopMostNotificationForm? _activeTopNotification;
        private string _persistentOfflineMessage = string.Empty;
        private const int NormalPingIntervalMs = 60000;
        private const int OfflinePingIntervalMs = 5000;
        private const int GameProcessCheckIntervalMs = 30000;
        private const string ServerUrl = "api.batbox.com";
        private WindowSizeEnforcer? _windowEnforcer; // Window size enforcement

        public Form1()
        {
            InitializeComponent();
            LoadConfiguration();
            SetupUI();
            SetupDeviceStatusIndicators();
            StartDevicePingMonitoring();

            // Handle cursor for disabled buttons
            this.MouseMove += Form1_MouseMove;
            grpControl.MouseMove += Form1_MouseMove;

            // Subscribe to display settings changes for instant updates
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            this.Resize += Form1_Resize;
        }

        private void Form1_MouseMove(object? sender, MouseEventArgs e)
        {
            // Check if mouse is over a disabled button
            var control = sender as Control;
            if (control == null) return;

            Point screenPoint = control.PointToScreen(e.Location);

            // Check each button that can be disabled
            if (IsMouseOverDisabledButton(btnStart, screenPoint) ||
                IsMouseOverDisabledButton(btnStop, screenPoint))
            {
                this.Cursor = Cursors.No;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        private bool IsMouseOverDisabledButton(Button btn, Point screenPoint)
        {
            if (btn.Enabled) return false;

            Rectangle btnRect = btn.RectangleToScreen(btn.ClientRectangle);
            return btnRect.Contains(screenPoint);
        }

        private void LoadConfiguration()
        {
            _config = AppConfig.LoadOrDefault();
            _log = new Logger(AppendLog);
            _monitor = new MonitoringService(() => _config, _log);
            _windowEnforcer = new WindowSizeEnforcer(() => _config, _log);
            _timer = new System.Windows.Forms.Timer { Interval = _config.IntervalSeconds * 1000 };
            _timer.Tick += async (s, e) => await RunMonitoringCycle();
            _gameProcessTimer = new System.Windows.Forms.Timer { Interval = GameProcessCheckIntervalMs };
            _gameProcessTimer.Tick += (s, e) => SyncGameRunningState();
        }

        private async Task RunMonitoringCycle()
        {
            // Prevent concurrent executions
            if (_isExecuting || _cts == null || _cts.IsCancellationRequested) return;

            _isExecuting = true;
            _timer.Stop(); // Stop timer while executing

            try
            {
                var (success, shouldStop) = await _monitor.TickAsync(_cts.Token);

                if (_cts.IsCancellationRequested) return;

                if (shouldStop)
                {
                    if (success)
                    {
                        _log.Info("SUCCESS - All checks passed, app launched.");
                        TransitionToEnforcementMode();
                        return;
                    }
                    StopMonitoring();
                }
                else
                {
                    // Restart timer for next retry only if not stopped
                    if (!_cts.IsCancellationRequested)
                        _timer.Start();
                }
            }
            catch (OperationCanceledException)
            {
                // User cancelled - already handled
            }
            catch (Exception ex)
            {
                _log.Error($"Monitoring error: {ex.Message}");
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void SetupUI()
        {
            // Populate UI from config
            txtLnkPath.Text = _config.LnkPath;
            numMinMonitors.Value = _config.MinMonitors;
            numInterval.Value = _config.IntervalSeconds;
            chkSkipMonitorCheck.Checked = _config.SkipMonitorCheck;
            chkEnforceWindowSize.Checked = _config.EnforceWindowSize;
            chkAutoKillSocketBindings.Checked = _config.AutoKillCameraSocketBindings;

            // Setup device grid
            _deviceList = new BindingList<DeviceConfig>(_config.Devices);
            dataGridDevices.DataSource = _deviceList;

            // Rename Skip column header to be clearer
            dataGridDevices.DataBindingComplete += (s, e) =>
            {
                var skipColumn = dataGridDevices.Columns["Skip"];
                if (skipColumn != null)
                    skipColumn.HeaderText = "Skip on Launch";
            };

            // Wire up buttons
            btnBrowseLnk.Click += btnBrowseLnk_Click;
            btnRestoreDefaults.Click += btnRestoreDefaults_Click;
            btnToggleSettings.Click += btnToggleSettings_Click;

            // Track changes to show/hide restore defaults button
            txtLnkPath.TextChanged += (s, e) => CheckForNonDefaultConfig();
            numMinMonitors.ValueChanged += (s, e) => CheckForNonDefaultConfig();
            numInterval.ValueChanged += (s, e) => CheckForNonDefaultConfig();
            chkSkipMonitorCheck.CheckedChanged += (s, e) =>
            {
                _config.SkipMonitorCheck = chkSkipMonitorCheck.Checked;
                CheckForNonDefaultConfig();
            };
            chkEnforceWindowSize.CheckedChanged += (s, e) =>
            {
                _config.EnforceWindowSize = chkEnforceWindowSize.Checked;
                CheckForNonDefaultConfig();
            };
            chkAutoKillSocketBindings.CheckedChanged += (s, e) =>
            {
                _config.AutoKillCameraSocketBindings = chkAutoKillSocketBindings.Checked;
                CheckForNonDefaultConfig();
            };
            dataGridDevices.CellValueChanged += (s, e) => CheckForNonDefaultConfig();

            btnStop.Enabled = false;
            CheckForNonDefaultConfig();
        }

        private void CheckForNonDefaultConfig()
        {
            var defaults = new AppConfig();
            bool isDifferent =
                txtLnkPath.Text != defaults.LnkPath ||
                numMinMonitors.Value != defaults.MinMonitors ||
                numInterval.Value != defaults.IntervalSeconds ||
                chkSkipMonitorCheck.Checked != defaults.SkipMonitorCheck ||
                chkEnforceWindowSize.Checked != defaults.EnforceWindowSize ||
                chkAutoKillSocketBindings.Checked != defaults.AutoKillCameraSocketBindings ||
                HasDeviceChanges(defaults);

            btnRestoreDefaults.Visible = isDifferent;
        }

        private bool HasDeviceChanges(AppConfig defaults)
        {
            if (_deviceList.Count != defaults.Devices.Count) return true;
            for (int i = 0; i < _deviceList.Count; i++)
            {
                if (_deviceList[i].Name != defaults.Devices[i].Name ||
                    _deviceList[i].Ip != defaults.Devices[i].Ip ||
                    _deviceList[i].Skip != defaults.Devices[i].Skip)
                    return true;
            }
            return false;
        }

        private void btnRestoreDefaults_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Restore all settings to defaults?",
                "Restore Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var defaults = new AppConfig();
                txtLnkPath.Text = defaults.LnkPath;
                numMinMonitors.Value = defaults.MinMonitors;
                numInterval.Value = defaults.IntervalSeconds;
                chkSkipMonitorCheck.Checked = defaults.SkipMonitorCheck;
                chkEnforceWindowSize.Checked = defaults.EnforceWindowSize;
                chkAutoKillSocketBindings.Checked = defaults.AutoKillCameraSocketBindings;

                _deviceList.Clear();
                foreach (var device in defaults.Devices)
                    _deviceList.Add(new DeviceConfig { Name = device.Name, Ip = device.Ip, Skip = device.Skip });

                _log.Info("Configuration restored to defaults.");
                CheckForNonDefaultConfig();
            }
        }

        private void btnToggleSettings_Click(object? sender, EventArgs e)
        {
            _settingsExpanded = !_settingsExpanded;

            if (_settingsExpanded)
            {
                // Expand
                pnlSettings.Visible = true;
                lblLog.Location = new Point(14, 466);
                txtLog.Location = new Point(14, 484);
                txtLog.Size = new Size(750, 270); // Taller log when expanded
                this.ClientSize = new Size(780, ExpandedHeight);
                lblVersion.Location = new Point(700, ExpandedHeight - 20);
                btnToggleSettings.Text = "▲ Hide Settings";
                btnToggleSettings.BackColor = Color.FromArgb(80, 80, 80);
            }
            else
            {
                // Collapse
                pnlSettings.Visible = false;
                lblLog.Location = new Point(14, 186);
                txtLog.Location = new Point(14, 204);
                txtLog.Size = new Size(750, 287); // More log space without device panel
                this.ClientSize = new Size(780, CollapsedHeight);
                lblVersion.Location = new Point(700, CollapsedHeight - 20);
                btnToggleSettings.Text = "⚙ Settings";
                btnToggleSettings.BackColor = Color.FromArgb(60, 60, 60);
            }
        }

        private void AppendLog(string line, string level)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendLog(line, level));
                return;
            }

            // Set color based on level (dark theme)
            Color color = level switch
            {
                "ERROR" => Color.FromArgb(255, 100, 100),  // Light red
                "WARN" => Color.FromArgb(255, 180, 100),   // Orange
                _ => Color.FromArgb(200, 200, 200)          // Light gray for INFO
            };

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(line + Environment.NewLine);
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        private void btnBrowseLnk_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Shortcut files (*.lnk)|*.lnk|All files (*.*)|*.*",
                Title = "Select Kiosk Shortcut"
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtLnkPath.Text = dlg.FileName ?? string.Empty;
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                _launchCompleted = false;
                _lastGameProcessState = null;
                SaveConfigFromUI();
                _cts = new CancellationTokenSource();
                _isExecuting = false;
                _monitor.Reset(); // Reset launch tracking for new session
                SetMonitoringState(true);

                _log.Info("=== Starting launch sequence ===");

                // Run once immediately
                var (success, shouldStop) = await _monitor.TickAsync(_cts.Token);

                if (_cts.IsCancellationRequested)
                {
                    // Already logged by btnStop_Click
                    return;
                }

                if (shouldStop)
                {
                    if (success)
                    {
                        _log.Info("SUCCESS - All checks passed, app launched.");
                        TransitionToEnforcementMode();
                        return;
                    }
                    StopMonitoring(); // Use StopMonitoring to properly reset UI
                    return;
                }

                // Start timer for retries
                _timer.Interval = _config.IntervalSeconds * 1000;
                _timer.Start();
            }
            catch (OperationCanceledException)
            {
                // Already logged by btnStop_Click
            }
            catch (Exception ex)
            {
                _log.Error($"Start error: {ex.Message}");
                SetMonitoringState(false);
            }
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            _cts?.Cancel();
            _isExecuting = false;
            SetMonitoringState(false);
            _log.Info("Launch aborted by user.");
        }

        private void StopMonitoring()
        {
            _cts?.Cancel(); // Cancel so UpdateReadyStatus knows we're not launching
            _cts = null; // Clear it completely
            _timer.Stop();

            // Force UI update on UI thread - directly set label to avoid any race conditions
            if (InvokeRequired)
            {
                Invoke(() => ResetToReadyState());
            }
            else
            {
                ResetToReadyState();
            }
        }

        private void TransitionToEnforcementMode()
        {
            StopMonitoring();

            // Start window size enforcement if enabled
            if (_config.EnforceWindowSize)
            {
                _windowEnforcer?.Start();
                _log.Info("Game launched. Minimizing to tray - monitoring window size.");
            }
            else
            {
                _log.Info("Game launched successfully. Minimizing to tray.");
            }

            // Minimize to system tray
            if (InvokeRequired)
            {
                Invoke(() => MinimizeToTray());
            }
            else
            {
                MinimizeToTray();
            }
        }

        private void MinimizeToTray()
        {
            _launchCompleted = true;

            // Update UI state
            btnStart.Enabled = false;
            btnStart.Text = "GAME RUNNING";
            btnStart.BackColor = Color.FromArgb(60, 120, 60); // Green
            btnStop.Enabled = false;

            // Show tray icon and hide window
            notifyIcon.Visible = true;
            _lastTrayMonitorOkState = GetMonitorTrayOkState();
            ShowTrayBalloon(
                "Batbox Launcher",
                _config.EnforceWindowSize ? "Running in background. Monitoring window size." : "Running in background.",
                ToolTipIcon.Info,
                3000);
            this.Hide();
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // After a successful launch, minimizing from restored/maximized should always go to tray.
            if (_launchCompleted && this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                _lastTrayMonitorOkState = GetMonitorTrayOkState();
                ShowTrayBalloon("Batbox Launcher", "Still running in background.", ToolTipIcon.Info, 2500);
                this.Hide();
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            notifyIcon.Visible = false;
            _lastTrayMonitorOkState = null;
            HidePersistentOfflineNotification();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayMenu_Show(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayMenu_Exit(object? sender, EventArgs e)
        {
            _windowEnforcer?.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void ResetToReadyState()
        {
            _launchCompleted = false;
            btnStart.Enabled = true;
            btnStart.Text = "▶ Launch Game";
            btnStart.BackColor = Color.FromArgb(180, 60, 60); // Back to red
            btnStop.Enabled = false;
        }

        private void SetMonitoringState(bool running)
        {
            if (!running)
                _timer.Stop();

            if (InvokeRequired)
            {
                Invoke(() => UpdateUI(running));
            }
            else
            {
                UpdateUI(running);
            }
        }

        private void UpdateUI(bool running)
        {
            btnStart.Enabled = !running;
            btnStop.Enabled = running;

            if (running)
            {
                btnStart.Text = "LAUNCHING...";
                btnStart.BackColor = Color.FromArgb(60, 120, 60); // Green when launching
            }
            else
            {
                btnStart.Text = "▶ Launch Game";
                btnStart.BackColor = Color.FromArgb(180, 60, 60); // Red when ready
            }
        }

        private void SetupDeviceStatusIndicators()
        {
            flowDeviceStatus.Controls.Clear();
            _deviceIndicators.Clear();
            _deviceStatus.Clear();
            _initialCheckComplete = false; // Reset until new checks complete

            // Setup monitor light paint event for circle drawing (only once)
            if (pnlMonitorLight.Tag == null)
            {
                pnlMonitorLight.Tag = "initialized";
                pnlMonitorLight.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(pnlMonitorLight.BackColor);
                    e.Graphics.FillEllipse(brush, 0, 0, 13, 13);
                };
            }

            // Setup monitor visualization panel (only once)
            if (pnlMonitorViz.Tag == null)
            {
                pnlMonitorViz.Tag = "initialized";
                pnlMonitorViz.Paint += PaintMonitorVisualization;
            }

            // Setup display settings button (only once)
            if (btnDisplaySettings.Tag == null)
            {
                btnDisplaySettings.Tag = "initialized";
                btnDisplaySettings.Click += (s, e) => OpenDisplaySettings();
            }

            // Setup server status light (only once)
            if (pnlServerLight.Tag == null)
            {
                pnlServerLight.Tag = "initialized";
                pnlServerLight.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(pnlServerLight.BackColor);
                    e.Graphics.FillEllipse(brush, 0, 0, 9, 9);
                };
                toolTip.SetToolTip(pnlServerLight, "Batbox server connectivity");
                toolTip.SetToolTip(lblServerStatus, "Batbox server connectivity");
            }

            for (int i = 0; i < _config.Devices.Count; i++)
            {
                var device = _config.Devices[i];
                bool isLastDevice = (i == _config.Devices.Count - 1);
                
                // Status light (circle) - last device (Camera 2) gets extra top margin to align with Monitors row
                var light = new Panel
                {
                    Width = 10,
                    Height = 10,
                    Margin = new Padding(12, isLastDevice ? 12 : 6, 0, 0),
                    BackColor = Color.FromArgb(80, 80, 80), // Gray = checking
                };
                light.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(light.BackColor);
                    e.Graphics.FillEllipse(brush, 0, 0, 9, 9);
                };

                // Device name label (compact)
                var nameLabel = new Label
                {
                    Text = device.Name,
                    AutoSize = true,
                    Margin = new Padding(2, isLastDevice ? 9 : 3, 0, 0),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    Font = new Font("Segoe UI", 8F)
                };

                flowDeviceStatus.Controls.Add(light);
                flowDeviceStatus.Controls.Add(nameLabel);

                _deviceIndicators[device.Ip] = light;
                _deviceStatus[device.Ip] = false;
            }
        }

        private void StartDevicePingMonitoring()
        {
            _log.Info("=== Batbox Launcher Starting ===");
            _log.Info($"Will check for: {_config.KioskExeName}, {_config.BaseballExeName}");
            
            // Log device configuration
            var skippedDevices = _config.Devices.Where(d => d.Skip).ToList();
            var activeDevices = _config.Devices.Where(d => !d.Skip).ToList();
            
            if (skippedDevices.Count > 0)
            {
                _log.Info($"Devices to check on launch: {activeDevices.Count}, Skipped: {skippedDevices.Count}");
                foreach (var device in skippedDevices)
                {
                    _log.Info($"  [SKIP] {device.Name} ({device.Ip})");
                }
            }
            else
            {
                _log.Info($"Devices to check: {_config.Devices.Count}");
            }
            
            // Log if monitor check is skipped
            if (_config.SkipMonitorCheck)
            {
                _log.Info($"[SKIP] Monitor check disabled");
            }
            
            _log.Info("Running startup checks...");

            // Run initial check
            _ = CheckAllDevicesAsync();

            // Setup timer for periodic checks (every 60 seconds)
            _pingTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            _pingTimer.Tick += async (s, e) => await CheckAllDevicesAsync();
            _pingTimer.Start();
            _gameProcessTimer.Start();
        }

        private async Task CheckAllDevicesAsync()
        {
            // Check monitor count
            CheckMonitorCount();

            // Check server connectivity (starts its own timer for periodic checks)
            if (_serverRetryTimer == null)
            {
                _ = CheckServerAsync();
            }

            // Check ALL devices - skip only affects launch, not status indicators
            var tasks = _config.Devices.Select(device => PingDeviceAsync(device));
            await Task.WhenAll(tasks);

            // If any monitored check is offline, poll aggressively every 5s until recovered.
            int totalMonitored = _config.Devices.Count;
            int monitoredOnline = _config.Devices
                .Count(d => _deviceStatus.TryGetValue(d.Ip, out var status) && status);
            bool allDevicesOnline = totalMonitored == 0 || monitoredOnline == totalMonitored;
            bool allHealthy = _monitorCheckPassed && allDevicesOnline;
            int desiredInterval = allHealthy ? NormalPingIntervalMs : OfflinePingIntervalMs;
            if (_pingTimer.Interval != desiredInterval)
            {
                _pingTimer.Interval = desiredInterval;
                _log.Info(allHealthy
                    ? "All checks recovered. Ping interval restored to 60s."
                    : "Detected offline status. Ping interval set to 5s.");
            }

            // Mark initial check as complete (all pings finished)
            _initialCheckComplete = true;

            // Update last checked time (12-hour format)
            if (InvokeRequired)
                Invoke(() => lblLastChecked.Text = $"Last checked: {DateTime.Now:h:mm:ss tt}");
            else
                lblLastChecked.Text = $"Last checked: {DateTime.Now:h:mm:ss tt}";

            // Update ready status
            UpdateReadyStatus();
        }

        private void SyncGameRunningState()
        {
            bool kioskRunning = IsProcessRunning(_config.KioskExeName);
            bool baseballRunning = IsProcessRunning(_config.BaseballExeName);
            bool gameRunning = kioskRunning && baseballRunning;

            if (_lastGameProcessState != gameRunning)
            {
                _lastGameProcessState = gameRunning;
                if (_launchCompleted && !gameRunning)
                {
                    _log.Info("Game process closed. Returning launcher to ready state.");
                }
            }

            if (!_launchCompleted || gameRunning)
                return;

            _windowEnforcer?.Stop();
            HidePersistentOfflineNotification();
            notifyIcon.Visible = false;
            if (!this.Visible)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
            ResetToReadyState();
        }

        private static bool IsProcessRunning(string exeName)
        {
            var processName = Path.GetFileNameWithoutExtension(exeName);
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            return Process.GetProcessesByName(processName).Any();
        }

        private async Task CheckServerAsync()
        {
            bool isOnline = false;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ServerUrl, 3000);
                isOnline = reply.Status == IPStatus.Success;
            }
            catch
            {
                isOnline = false;
            }

            _serverOnline = isOnline;

            // Check internet if server is offline
            bool? hasInternet = null;
            if (!isOnline)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync("google.com", 3000);
                    hasInternet = reply.Status == IPStatus.Success;
                }
                catch
                {
                    hasInternet = false;
                }
            }

            // Only log if state changed from previous check
            var currentState = (online: isOnline, hasInternet);
            bool stateChanged = !_lastServerState.HasValue ||
                                _lastServerState.Value.online != currentState.online ||
                                _lastServerState.Value.hasInternet != currentState.hasInternet;
            _lastServerState = currentState;

            if (stateChanged)
            {
                if (isOnline)
                {
                    _log.Info($"Pinging Servers ({ServerUrl})... Online");
                }
                else if (hasInternet == true)
                {
                    _log.Warn($"Pinging Servers ({ServerUrl})... OFFLINE! (Server issue - Internet is working, Please contact support.)");
                    _log.Info("Starting the Game will initiate Offline Mode.");
                }
                else
                {
                    _log.Warn($"Pinging Servers ({ServerUrl})... OFFLINE! No Internet Connection!");
                    _log.Info("Starting the Game will initiate Offline Mode.");
                }
            }

            // Set timer interval based on status
            if (isOnline)
            {
                // When online, check again in 3 minutes
                StartServerCheckTimer(180000); // 3 minutes
            }
            else
            {
                // When offline, retry in 10 seconds
                StartServerCheckTimer(10000); // 10 seconds
            }

            // Update server indicator on UI thread
            if (InvokeRequired)
            {
                Invoke(() => UpdateServerIndicator(isOnline));
            }
            else
            {
                UpdateServerIndicator(isOnline);
            }
        }

        private void UpdateServerIndicator(bool isOnline)
        {
            // Update light color like device indicators
            pnlServerLight.BackColor = isOnline
                ? Color.FromArgb(50, 205, 50)   // Green
                : Color.FromArgb(220, 50, 50);  // Red
            pnlServerLight.Invalidate();
        }

        private void StartServerCheckTimer(int intervalMs)
        {
            // Only create timer once
            if (_serverRetryTimer == null)
            {
                _serverRetryTimer = new System.Windows.Forms.Timer();
                _serverRetryTimer.Tick += async (s, e) =>
                {
                    _serverRetryTimer?.Stop(); // Stop before checking (interval may change)
                    await CheckServerAsync();
                };
            }
            
            // Update interval and restart
            _serverRetryTimer.Stop();
            _serverRetryTimer.Interval = intervalMs;
            _serverRetryTimer.Start();
        }

        private void CheckMonitorCount()
        {
            int monitorCount = Screen.AllScreens.Length;
            int minRequired = _config.MinMonitors;
            bool skipCheck = _config.SkipMonitorCheck;

            _monitorCheckPassed = skipCheck || monitorCount >= minRequired;

            // Check if primary monitor is on the leftmost position
            bool primaryOnLeft = IsPrimaryMonitorOnLeft();

            // Only log if state changed
            bool stateChanged = !_lastMonitorState.HasValue ||
                                _lastMonitorState.Value.count != monitorCount ||
                                _lastMonitorState.Value.primaryOnLeft != primaryOnLeft;
            _lastMonitorState = (monitorCount, primaryOnLeft);

            if (stateChanged && !skipCheck)
            {
                // Log monitor check (only when not skipped)
                if (_monitorCheckPassed)
                {
                    _log.Info($"Checking monitors... {monitorCount}/{minRequired} ✓");
                }
                else
                {
                    _log.Warn($"Checking monitors... {monitorCount}/{minRequired} ✗ (need {minRequired})");
                }

                // Log primary position check
                if (!primaryOnLeft && monitorCount > 1)
                {
                    _log.Warn("Primary monitor is not on the left - this may cause issues!");
                }
                else if (monitorCount > 1)
                {
                    _log.Info("Checking monitor positions... Primary on left ✓");
                }
            }

            if (InvokeRequired)
            {
                Invoke(() => UpdateMonitorIndicator(monitorCount, minRequired, skipCheck, primaryOnLeft));
            }
            else
            {
                UpdateMonitorIndicator(monitorCount, minRequired, skipCheck, primaryOnLeft);
            }
        }

        private bool IsPrimaryMonitorOnLeft()
        {
            var screens = Screen.AllScreens;
            if (screens.Length <= 1) return true; // Single monitor is always "on left"

            var primary = Screen.PrimaryScreen;
            if (primary == null) return true;

            // Check if any monitor is to the left of the primary
            int primaryLeft = primary.Bounds.X;
            return !screens.Any(s => s.Bounds.X < primaryLeft);
        }

        private void OpenDisplaySettings()
        {
            try
            {
                // Use cmd /c start to open the settings URI - most reliable method
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c start ms-settings:display",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
                _log.Info("Opening Windows Display Settings...");
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to open display settings: {ex.Message}");
                // Fallback: try control panel
                try
                {
                    System.Diagnostics.Process.Start("control.exe", "desk.cpl");
                }
                catch { }
            }
        }

        private void UpdateMonitorIndicator(int current, int required, bool skipped, bool primaryOnLeft = true)
        {
            bool countOk = skipped || current >= required;
            bool hasPositionWarning = !primaryOnLeft && current > 1;

            // Simple text without check/cross - color indicates status
            lblMonitorStatus.Text = $"Monitors: {current}/{required}";

            // Set light color: Orange if position warning, Red if count fail, Green if all OK
            if (hasPositionWarning)
            {
                pnlMonitorLight.BackColor = Color.FromArgb(255, 165, 0); // Orange - position warning
            }
            else if (!countOk)
            {
                pnlMonitorLight.BackColor = Color.FromArgb(220, 50, 50); // Red - count fail
            }
            else
            {
                pnlMonitorLight.BackColor = Color.FromArgb(50, 205, 50); // Green - all OK
            }

            // Show warning if primary is not on left (and multiple monitors)
            if (hasPositionWarning)
            {
                lblMonitorWarning.Text = "⚠ Primary not on left!";
                lblMonitorWarning.Visible = true;
                toolTip.SetToolTip(lblMonitorWarning, "The primary monitor (with taskbar) should be\nthe leftmost monitor for the game to work correctly.");
            }
            else
            {
                lblMonitorWarning.Visible = false;
            }
            
            // Always show display settings button
            btnDisplaySettings.Visible = true;
            toolTip.SetToolTip(btnDisplaySettings, "Open Windows Display Settings");

            pnlMonitorLight.Invalidate();
            pnlMonitorViz.Invalidate(); // Redraw monitor visualization
            pnlMonitors.Refresh(); // Force immediate UI update for button visibility
        }

        private void PaintMonitorVisualization(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var screens = Screen.AllScreens;
            if (screens.Length == 0) return;

            // Sort screens by X position (left to right)
            var sortedScreens = screens.OrderBy(s => s.Bounds.X).ToArray();

            // Find the bounding box of all monitors
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);
            int maxX = screens.Max(s => s.Bounds.Right);
            int maxY = screens.Max(s => s.Bounds.Bottom);

            int totalWidth = maxX - minX;
            int totalHeight = maxY - minY;

            // Calculate scale to fit in the panel with padding
            float padding = 10;
            float availableWidth = pnlMonitorViz.Width - (padding * 2);
            float availableHeight = pnlMonitorViz.Height - (padding * 2);
            float scale = Math.Min(availableWidth / totalWidth, availableHeight / totalHeight);

            // Center the visualization
            float offsetX = padding + (availableWidth - (totalWidth * scale)) / 2;
            float offsetY = padding + (availableHeight - (totalHeight * scale)) / 2;

            // Draw each monitor (sorted by position)
            for (int i = 0; i < sortedScreens.Length; i++)
            {
                var screen = sortedScreens[i];
                var bounds = screen.Bounds;

                // Calculate scaled position
                float x = offsetX + ((bounds.X - minX) * scale);
                float y = offsetY + ((bounds.Y - minY) * scale);
                float w = bounds.Width * scale;
                float h = bounds.Height * scale;

                // Draw monitor rectangle
                var rect = new RectangleF(x, y, w, h);

                // Different color for primary monitor
                Color fillColor = screen.Primary
                    ? Color.FromArgb(60, 100, 180)   // Blue for primary
                    : Color.FromArgb(50, 50, 50);    // Gray for others

                Color borderColor = screen.Primary
                    ? Color.FromArgb(100, 150, 220)  // Lighter blue border
                    : Color.FromArgb(80, 80, 80);    // Gray border

                using var fillBrush = new SolidBrush(fillColor);
                using var borderPen = new Pen(borderColor, 2);

                g.FillRectangle(fillBrush, rect);
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);

                // Draw resolution text inside each monitor box
                string resolution = $"{bounds.Width}x{bounds.Height}";
                using var resFont = new Font("Segoe UI", 7F);
                using var resBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
                var resSize = g.MeasureString(resolution, resFont);

                if (screen.Primary)
                {
                    // Draw star above resolution for primary
                    string label = "★";
                    using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    using var textBrush = new SolidBrush(Color.White);

                    var textSize = g.MeasureString(label, font);
                    float textX = x + (w - textSize.Width) / 2;
                    float textY = y + (h - textSize.Height - resSize.Height) / 2;
                    g.DrawString(label, font, textBrush, textX, textY);

                    // Resolution below star
                    float resX = x + (w - resSize.Width) / 2;
                    float resY = textY + textSize.Height - 2;
                    g.DrawString(resolution, resFont, resBrush, resX, resY);
                }
                else
                {
                    // Just resolution centered for non-primary
                    float resX = x + (w - resSize.Width) / 2;
                    float resY = y + (h - resSize.Height) / 2;
                    g.DrawString(resolution, resFont, resBrush, resX, resY);
                }
            }
        }
        

        private async Task PingDeviceAsync(DeviceConfig device)
        {
            bool isReachable = false;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(device.Ip, 2000);
                isReachable = reply.Status == IPStatus.Success;
            }
            catch
            {
                isReachable = false;
            }

            _deviceStatus[device.Ip] = isReachable;

            // Only log if status changed from previous check
            bool statusChanged = !_lastDeviceStatus.TryGetValue(device.Ip, out var lastStatus) || lastStatus != isReachable;
            _lastDeviceStatus[device.Ip] = isReachable;

            if (statusChanged)
            {
                if (isReachable)
                {
                    _log.Info($"Pinging {device.Name} ({device.Ip})... Online");
                }
                else
                {
                    _log.Warn($"Pinging {device.Name} ({device.Ip})... OFFLINE!");
                    _ = Task.Run(() => DiagnoseAndReleaseSocketBindings(device));
                }
            }

            // Update UI on UI thread
            if (InvokeRequired)
            {
                Invoke(() => UpdateDeviceIndicator(device.Ip, isReachable));
            }
            else
            {
                UpdateDeviceIndicator(device.Ip, isReachable);
            }
        }

        private void DiagnoseAndReleaseSocketBindings(DeviceConfig device)
        {
            try
            {
                var bindings = SocketDiagnostics.GetBindingsForIp(device.Ip);
                if (bindings.Count == 0)
                {
                    _log.Debug($"Socket diagnostics: no active bindings found for {device.Name} ({device.Ip}).");
                    return;
                }

                _log.Warn($"Socket diagnostics for {device.Name} ({device.Ip}) - {bindings.Count} binding(s) found:");
                foreach (var b in bindings)
                {
                    _log.Warn($"  {b.Protocol} PID:{b.Pid} ({b.ProcessName}) LocalPort:{b.LocalPort} Local:{b.LocalAddress} Remote:{b.ForeignAddress}");
                }

                if (!_config.AutoKillCameraSocketBindings)
                {
                    _log.Warn($"Auto-kill disabled for {device.Name} ({device.Ip}) - diagnostics only.");
                    return;
                }

                var currentPid = Process.GetCurrentProcess().Id;
                var candidatePids = bindings
                    .Select(b => b.Pid)
                    .Where(pid => pid > 4 && pid != currentPid)
                    .Distinct()
                    .ToList();

                if (candidatePids.Count == 0)
                {
                    _log.Warn($"Socket release skipped for {device.Name} ({device.Ip}): no safe PID candidates.");
                    return;
                }

                _log.Warn($"Will kill {candidatePids.Count} process(es) for {device.Name} ({device.Ip}):");
                foreach (var pid in candidatePids)
                {
                    var processName = bindings.FirstOrDefault(b => b.Pid == pid)?.ProcessName ?? "Unknown";
                    var ports = bindings
                        .Where(b => b.Pid == pid)
                        .Select(b => b.LocalPort)
                        .Distinct()
                        .OrderBy(p => p)
                        .Select(p => p.ToString())
                        .ToList();

                    _log.Warn($"  PID:{pid} ({processName}) Ports:[{string.Join(", ", ports)}]");
                }

                foreach (var pid in candidatePids)
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        p.Kill();
                        _log.Warn($"Killed PID:{pid} ({p.ProcessName}) for {device.Name} ({device.Ip}).");
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Failed to kill PID:{pid} for {device.Name} ({device.Ip}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug($"Socket diagnostics failed for {device.Name} ({device.Ip}): {ex.Message}");
            }
        }

        private void UpdateDeviceIndicator(string ip, bool isOnline)
        {
            if (_deviceIndicators.TryGetValue(ip, out var light))
            {
                light.BackColor = isOnline
                    ? Color.FromArgb(50, 205, 50)  // Green
                    : Color.FromArgb(220, 50, 50); // Red
                light.Invalidate();
            }
        }

        private void UpdateReadyStatus()
        {
            if (InvokeRequired)
            {
                Invoke(UpdateReadyStatus);
                return;
            }

            // Check if currently launching
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            // Don't show green until initial check has completed
            if (!_initialCheckComplete)
            {
                lblStatus.Text = "● READY";
                lblStatus.ForeColor = Color.FromArgb(150, 150, 150); // Gray
                return;
            }

            // Get non-skipped devices
            var activeDevices = _config.Devices.Where(d => !d.Skip).ToList();
            var skippedDevices = _config.Devices.Where(d => d.Skip).ToList();

            // Count online devices
            int onlineCount = activeDevices.Count(d => _deviceStatus.TryGetValue(d.Ip, out var status) && status);
            int totalActive = activeDevices.Count;
            int skippedCount = skippedDevices.Count;

            // Check if all non-skipped devices are online
            bool allDevicesOnline = totalActive == 0 || onlineCount == totalActive;

            // ALL conditions must pass for green READY
            bool allGreen = _monitorCheckPassed && allDevicesOnline;

            // Only log if state changed from previous check (suppress duplicate messages)
            // Skip logging on first check (startup) to avoid confusing logs after warnings
            if (_lastAllGreenState != allGreen)
            {
                if (!_isFirstStatusCheck)
                {
                    if (allGreen)
                    {
                        string skippedInfo = skippedCount > 0 ? $" ({skippedCount} device{(skippedCount > 1 ? "s" : "")} skipped)" : "";
                        _log.Info($"Status: ALL CHECKS PASSED - Ready to launch!{skippedInfo}");
                    }
                    else
                    {
                        var issues = new List<string>();
                        if (!_monitorCheckPassed) issues.Add("monitors");
                        if (!allDevicesOnline) issues.Add($"Devices ({onlineCount}/{totalActive} Connected)");
                        _log.Info($"Status: Waiting... ({string.Join(", ", issues)} Not Ready)");
                    }
                }
                _isFirstStatusCheck = false;
                _lastAllGreenState = allGreen;
            }

            // Tray health uses ALL monitored devices (not launch-skip logic),
            // so we never report "healthy" while monitored devices are offline.
            int totalMonitored = _config.Devices.Count;
            int monitoredOnline = _config.Devices
                .Count(d => _deviceStatus.TryGetValue(d.Ip, out var status) && status);
            bool trayAllGreen = _monitorCheckPassed && (totalMonitored == 0 || monitoredOnline == totalMonitored);

            // While running in tray, surface health changes as notifications.
            NotifyTrayHealthChangeIfNeeded(trayAllGreen, monitoredOnline, totalMonitored);

            if (allGreen)
            {
                lblStatus.Text = "● READY";
                lblStatus.ForeColor = Color.FromArgb(50, 205, 50); // Green
            }
            else
            {
                lblStatus.Text = "● READY";
                lblStatus.ForeColor = Color.FromArgb(150, 150, 150); // Gray
            }
        }

        private void NotifyTrayHealthChangeIfNeeded(bool allGreen, int onlineCount, int totalActive)
        {
            if (!notifyIcon.Visible)
                return; // Not in tray mode.

            if (allGreen)
            {
                HidePersistentOfflineNotification();
                return;
            }

            var lines = new List<string>();
            lines.Add("Status: OFFLINE");
            if (!_monitorCheckPassed)
                lines.Add("• Monitor check failed");

            var offlineDevices = GetOfflineDevices();
            if (offlineDevices.Count > 0)
            {
                foreach (var device in offlineDevices)
                {
                    lines.Add($"• {device.Name} ({device.Ip})");
                }
            }

            string issueText = lines.Count > 0
                ? string.Join(Environment.NewLine, lines)
                : "Connectivity issue detected";
            ShowPersistentOfflineNotification(issueText);
        }

        private void ShowTrayBalloon(string title, string message, ToolTipIcon icon, int timeoutMs)
        {
            if (!notifyIcon.Visible) return;

            // Keep on-top in-app notifications only (tray balloon disabled by request).
            ShowTopMostNotification(title, message, icon == ToolTipIcon.Warning, timeoutMs);
        }

        private void ShowPersistentOfflineNotification(string message)
        {
            if (_persistentOfflineMessage == message && _activeTopNotification != null && !_activeTopNotification.IsDisposed)
                return;

            _persistentOfflineMessage = message;
            ShowTopMostNotification("Status Alert", message, true, 0);
        }

        private void HidePersistentOfflineNotification()
        {
            _persistentOfflineMessage = string.Empty;

            if (_activeTopNotification == null) return;

            _activeTopNotification.Close();
            _activeTopNotification.Dispose();
            _activeTopNotification = null;
        }

        private List<DeviceConfig> GetOfflineDevices()
        {
            return _config.Devices
                .Where(d => !_deviceStatus.TryGetValue(d.Ip, out var status) || !status)
                .ToList();
        }

        private void ShowTopMostNotification(string title, string message, bool warning, int timeoutMs)
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowTopMostNotification(title, message, warning, timeoutMs));
                return;
            }

            try
            {
                _activeTopNotification?.Close();
                _activeTopNotification?.Dispose();
                _activeTopNotification = new TopMostNotificationForm(title, message, warning, timeoutMs);
                _activeTopNotification.Show();
            }
            catch
            {
                // Avoid interrupting monitoring if the UI notification cannot be shown.
            }
        }


        private void NotifyTrayDeviceChangeIfNeeded(DeviceConfig device, bool statusChanged, bool isReachable)
        {
            if (!statusChanged || !_launchCompleted || !notifyIcon.Visible)
                return;

            if (isReachable)
            {
                ShowTrayBalloon(
                    "Device Recovered",
                    $"{device.Name} is back online.",
                    ToolTipIcon.Info,
                    3000);
            }
            else
            {
                ShowTrayBalloon(
                    "Device Disconnected",
                    $"{device.Name} is offline.",
                    ToolTipIcon.Warning,
                    4000);
            }
        }

        private bool GetMonitorTrayOkState()
        {
            var monitorCount = Screen.AllScreens.Length;
            bool primaryOnLeft = IsPrimaryMonitorOnLeft();
            bool countOk = _config.SkipMonitorCheck || monitorCount >= _config.MinMonitors;
            bool layoutOk = primaryOnLeft || monitorCount <= 1;
            return countOk && layoutOk;
        }

        private void NotifyTrayMonitorChangeIfNeeded(bool monitorStateChanged, int monitorCount, int minRequired, bool primaryOnLeft)
        {
            if (!monitorStateChanged || !_launchCompleted || !notifyIcon.Visible)
                return;

            bool monitorOk = (_config.SkipMonitorCheck || monitorCount >= minRequired) && (primaryOnLeft || monitorCount <= 1);
            if (_lastTrayMonitorOkState == monitorOk)
                return;

            _lastTrayMonitorOkState = monitorOk;

            if (monitorOk)
            {
                ShowTrayBalloon(
                    "Monitor Recovered",
                    $"Monitor check is healthy ({monitorCount}/{minRequired}).",
                    ToolTipIcon.Info,
                    3000);
                return;
            }

            if (!_config.SkipMonitorCheck && monitorCount < minRequired)
            {
                ShowTrayBalloon(
                    "Monitor Disconnected",
                    $"Detected {monitorCount}/{minRequired} monitors.",
                    ToolTipIcon.Warning,
                    4000);
                return;
            }

            if (monitorCount > 1 && !primaryOnLeft)
            {
                ShowTrayBalloon(
                    "Monitor Layout Warning",
                    "Primary monitor is not on the left.",
                    ToolTipIcon.Warning,
                    4000);
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            SaveConfigFromUI();
            _config.Save();
            _log.Info("Configuration saved.");

            // Refresh device status indicators
            SetupDeviceStatusIndicators();
            _ = CheckAllDevicesAsync();
        }

        private void SaveConfigFromUI()
        {
            _config.LnkPath = txtLnkPath.Text;
            _config.MinMonitors = (int)numMinMonitors.Value;
            _config.IntervalSeconds = (int)numInterval.Value;
            _config.SkipMonitorCheck = chkSkipMonitorCheck.Checked;
            _config.EnforceWindowSize = chkEnforceWindowSize.Checked;
            _config.AutoKillCameraSocketBindings = chkAutoKillSocketBindings.Checked;

            // Update devices from grid
            _config.Devices = _deviceList.ToList();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            // This fires instantly when Windows display settings change
            if (InvokeRequired)
            {
                Invoke(() => OnDisplaySettingsChanged(sender, e));
                return;
            }

            _log.Info("Display settings changed detected.");
            CheckMonitorCount();
            UpdateReadyStatus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unsubscribe from system events
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            _timer?.Stop();
            _timer?.Dispose();
            _pingTimer?.Stop();
            _pingTimer?.Dispose();
            _gameProcessTimer?.Stop();
            _gameProcessTimer?.Dispose();
            _serverRetryTimer?.Stop();
            _serverRetryTimer?.Dispose();
            _windowEnforcer?.Dispose();
            _activeTopNotification?.Close();
            _activeTopNotification?.Dispose();
            notifyIcon.Visible = false;
            base.OnFormClosing(e);
        }

        private void lblLnkPath_Click(object sender, EventArgs e)
        {

        }

        private void btnAdjustWindow_Click(object? sender, EventArgs e)
        {
            // Manually trigger window size adjustment for both Baseball and Kiosk
            if (_windowEnforcer == null)
            {
                _log.Warn("Window enforcer not initialized.");
                return;
            }

            _windowEnforcer.TryEnforceWindowSize(logIfNotFound: true);
        }
    }
}
