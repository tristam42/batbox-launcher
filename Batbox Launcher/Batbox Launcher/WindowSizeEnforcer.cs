using System.Runtime.InteropServices;

namespace BatboxLauncher
{
    /// <summary>
    /// Monitors a target window and enforces a specific size/position using Windows event hooks.
    /// Event-driven approach - no polling required.
    /// </summary>
    public class WindowSizeEnforcer : IDisposable
    {
        private readonly Func<AppConfig> _getConfig;
        private readonly Logger _log;
        private IntPtr _hookHandle = IntPtr.Zero;
        private GCHandle _delegateHandle;
        private WinEventDelegate? _winEventDelegate;
        private bool _isEnforcing = false;
        private bool _disposed = false;

        // Win32 API imports
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        // Constants
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        // Delegate for the hook callback
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public WindowSizeEnforcer(Func<AppConfig> getConfig, Logger log)
        {
            _getConfig = getConfig;
            _log = log;
        }

        /// <summary>
        /// Start monitoring for window size/position changes
        /// </summary>
        public void Start()
        {
            if (_hookHandle != IntPtr.Zero)
                return; // Already running

            var cfg = _getConfig();
            if (!cfg.EnforceWindowSize)
            {
                _log.Info("Window size enforcement is disabled.");
                return;
            }

            // Create the delegate and prevent it from being garbage collected
            _winEventDelegate = new WinEventDelegate(WinEventProc);
            _delegateHandle = GCHandle.Alloc(_winEventDelegate);

            // Set up the event hook for location changes
            _hookHandle = SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE,
                EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                _winEventDelegate,
                0, // All processes
                0, // All threads
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (_hookHandle != IntPtr.Zero)
            {
                _log.Info($"Window size enforcer started. Monitoring 'Baseball' and 'Kiosk' for size changes.");
                
                // Immediately enforce size on start (in case windows already exist)
                EnforceWindowSize();
            }
            else
            {
                _log.Warn("Failed to set up window monitoring hook.");
                if (_delegateHandle.IsAllocated)
                    _delegateHandle.Free();
            }
        }

        /// <summary>
        /// Stop monitoring
        /// </summary>
        public void Stop()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
                _log.Info("Window size enforcer stopped.");
            }

            if (_delegateHandle.IsAllocated)
                _delegateHandle.Free();
        }

        /// <summary>
        /// Callback when any window location changes
        /// </summary>
        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Only process window-level events (idObject == 0)
            if (idObject != 0 || hwnd == IntPtr.Zero)
                return;

            var cfg = _getConfig();
            if (!cfg.EnforceWindowSize)
                return;

            // Check if this is one of our target windows (Baseball or Kiosk)
            var title = GetWindowTitle(hwnd);
            if (!string.Equals(title, cfg.TargetWindowTitle, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(title, "Kiosk", StringComparison.OrdinalIgnoreCase))
                return;

            // This is a target window - enforce size
            EnforceWindowSizeForHandle(hwnd);
        }

        /// <summary>
        /// Find the target window and enforce size
        /// </summary>
        public void EnforceWindowSize()
        {
            var cfg = _getConfig();
            if (!cfg.EnforceWindowSize)
                return;

            TryEnforceWindowSize(false);
        }

        /// <summary>
        /// Manually trigger window size enforcement (ignores the enable setting)
        /// </summary>
        /// <returns>True if any window was found and adjusted</returns>
        public bool TryEnforceWindowSize(bool logIfNotFound = true)
        {
            var cfg = _getConfig();
            bool anyFound = false;

            // Enforce Baseball window
            IntPtr hwnd = FindWindow(null, cfg.TargetWindowTitle);
            if (hwnd != IntPtr.Zero)
            {
                EnforceWindowSizeForHandle(hwnd, logAlways: logIfNotFound);
                anyFound = true;
            }
            else if (logIfNotFound)
            {
                _log.Warn($"Window '{cfg.TargetWindowTitle}' not found.");
            }

            // Enforce Kiosk window
            IntPtr kioskHwnd = FindWindow(null, "Kiosk");
            if (kioskHwnd != IntPtr.Zero)
            {
                EnforceWindowSizeForHandle(kioskHwnd, logAlways: logIfNotFound);
                anyFound = true;
            }
            else if (logIfNotFound)
            {
                _log.Warn("Window 'Kiosk' not found.");
            }

            return anyFound;
        }

        /// <summary>
        /// Enforce size for a specific window handle
        /// </summary>
        private void EnforceWindowSizeForHandle(IntPtr hwnd, bool logAlways = false)
        {
            // Prevent re-entrancy (our SetWindowPos will trigger another event)
            if (_isEnforcing)
                return;

            var cfg = _getConfig();

            if (!GetWindowRect(hwnd, out RECT rect))
                return;

            // Get the window title for logging
            string windowTitle = GetWindowTitle(hwnd);
            if (string.IsNullOrEmpty(windowTitle))
                windowTitle = "Unknown";

            // Kiosk: only move, don't resize
            bool isKiosk = string.Equals(windowTitle, "Kiosk", StringComparison.OrdinalIgnoreCase);

            // Use absolute screen coordinates
            int targetX = cfg.TargetWindowX;  // Default: 0
            int targetY = cfg.TargetWindowY;  // Default: 0

            int currentX = rect.Left;
            int currentY = rect.Top;
            int currentWidth = rect.Right - rect.Left;
            int currentHeight = rect.Bottom - rect.Top;

            // For Kiosk, keep current size; for Baseball, use target size
            int targetWidth = isKiosk ? currentWidth : cfg.TargetWindowWidth;
            int targetHeight = isKiosk ? currentHeight : cfg.TargetWindowHeight;

            // Log debug info
            if (logAlways)
            {
                if (isKiosk)
                    _log.Info($"[{windowTitle}] Target position: ({targetX},{targetY}) (size unchanged)");
                else
                    _log.Info($"[{windowTitle}] Target: ({targetX},{targetY}) {targetWidth}x{targetHeight}");
                _log.Info($"[{windowTitle}] Current: ({currentX},{currentY}) {currentWidth}x{currentHeight}");
            }

            // Check if already correct
            bool sizeCorrect = isKiosk || (currentWidth == targetWidth && currentHeight == targetHeight);
            bool positionCorrect = currentX == targetX && currentY == targetY;

            // If everything is already correct, nothing to do
            if (sizeCorrect && positionCorrect)
            {
                if (logAlways)
                    _log.Info($"Window '{windowTitle}' already at correct position" + (isKiosk ? "" : "/size"));
                return;
            }

            // Log what will change
            string changes = "";
            if (!positionCorrect)
                changes += $"position ({currentX},{currentY})→({targetX},{targetY})";
            if (!sizeCorrect && !isKiosk)
            {
                if (changes.Length > 0) changes += ", ";
                changes += $"size {currentWidth}x{currentHeight}→{targetWidth}x{targetHeight}";
            }

            // Enforce the target size and position
            _isEnforcing = true;
            try
            {
                bool result = SetWindowPos(hwnd, IntPtr.Zero,
                    targetX, targetY,
                    targetWidth, targetHeight,
                    0);

                if (result)
                {
                    _log.Info($"Window '{windowTitle}' corrected: {changes}");
                }
            }
            finally
            {
                _isEnforcing = false;
            }
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}

