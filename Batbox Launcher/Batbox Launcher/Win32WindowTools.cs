using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BatboxLauncher
{
    public enum MoveResult
    {
        NoWindowFound,
        AlreadyOnPrimary,
        MovedToPrimary,
        FailedToMove
    }

    public static class Win32WindowTools
    {
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern IntPtr SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // For changing primary monitor
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const uint CDS_UPDATEREGISTRY = 0x01;
        private const uint CDS_SET_PRIMARY = 0x10;
        private const uint CDS_NORESET = 0x10000000;
        private const int DISP_CHANGE_SUCCESSFUL = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        private const uint DM_POSITION = 0x20;

        private const int SW_RESTORE = 9;
        private const int SW_SHOWNOACTIVATE = 4;
        
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        /// <summary>
        /// Lists all visible windows with their titles
        /// </summary>
        public static List<string> GetAllVisibleWindows()
        {
            var windows = new List<string>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    var title = GetWindowTitle(hWnd);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        GetWindowThreadProcessId(hWnd, out int pid);
                        GetWindowRect(hWnd, out var rect);
                        var screen = Screen.FromHandle(hWnd);
                        windows.Add($"'{title}' (PID={pid}) at ({rect.Left},{rect.Top}) on {screen.DeviceName}");
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// Moves window to primary monitor. Uses SetWindowPos directly.
        /// </summary>
        public static MoveResult TryMoveMainWindowToPrimary(Process proc, out string debugInfo)
        {
            debugInfo = "";
            
            var hWnd = GetMainWindow(proc, out string windowInfo);
            debugInfo = windowInfo;
            
            if (hWnd == IntPtr.Zero)
                return MoveResult.NoWindowFound;

            // Check which monitor the window is currently on
            if (!GetWindowRect(hWnd, out var rect))
            {
                debugInfo += " | Failed to get window rect";
                return MoveResult.NoWindowFound;
            }
            
            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;
            
            // Use LEFT EDGE of window to determine which monitor it's on
            // (important for wide windows that span multiple monitors)
            var leftEdgePoint = new System.Drawing.Point(rect.Left + 10, rect.Top + 10);
            var currentScreen = Screen.FromPoint(leftEdgePoint);
            var primaryScreen = Screen.PrimaryScreen;
            
            if (primaryScreen == null)
            {
                debugInfo += " | No primary screen found";
                return MoveResult.FailedToMove;
            }

            debugInfo += $" | Window size: {windowWidth}x{windowHeight} at ({rect.Left},{rect.Top}) on {currentScreen.DeviceName}";

            // Already on primary (check if left edge is on primary monitor)
            if (currentScreen.DeviceName == primaryScreen.DeviceName)
                return MoveResult.AlreadyOnPrimary;
            
            // Also check if position matches primary's origin
            if (rect.Left == primaryScreen.Bounds.Left && rect.Top == primaryScreen.Bounds.Top)
                return MoveResult.AlreadyOnPrimary;

            // Restore window first in case it's maximized
            ShowWindow(hWnd, SW_RESTORE);
            Thread.Sleep(200);

            // Position at top-left of primary monitor (using Bounds, not WorkingArea, to include taskbar area)
            int newX = primaryScreen.Bounds.Left;
            int newY = primaryScreen.Bounds.Top;

            // Use SetWindowPos to directly move the window
            bool moved = SetWindowPos(hWnd, IntPtr.Zero, newX, newY, 0, 0, 
                SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

            debugInfo += $" | SetWindowPos to ({newX},{newY}) result={moved}";

            if (!moved)
            {
                debugInfo += " | SetWindowPos failed";
                return MoveResult.FailedToMove;
            }

            // Verify it moved - check if left edge is on primary monitor
            Thread.Sleep(100);
            if (!GetWindowRect(hWnd, out rect))
                return MoveResult.FailedToMove;

            // Check left edge of window, not center (for wide windows that span monitors)
            var leftEdge = new System.Drawing.Point(rect.Left + 10, rect.Top + 10);
            currentScreen = Screen.FromPoint(leftEdge);

            debugInfo += $" | After move: ({rect.Left},{rect.Top}) on {currentScreen.DeviceName}";

            // Success if the left edge is now on primary
            if (currentScreen.DeviceName == primaryScreen.DeviceName)
                return MoveResult.MovedToPrimary;

            // Also consider it success if window is now at position (0,0) or primary's bounds
            if (rect.Left == primaryScreen.Bounds.Left && rect.Top == primaryScreen.Bounds.Top)
                return MoveResult.MovedToPrimary;

            return MoveResult.FailedToMove;
        }

        // Keep old signature for compatibility
        public static bool TryMoveMainWindowToPrimary(Process proc)
        {
            var result = TryMoveMainWindowToPrimary(proc, out _);
            return result == MoveResult.AlreadyOnPrimary || result == MoveResult.MovedToPrimary;
        }

        private static IntPtr GetMainWindow(Process proc, out string debugInfo)
        {
            debugInfo = "";
            
            try
            {
                debugInfo = $"PID={proc.Id}";
                
                if (proc == null || proc.HasExited)
                {
                    debugInfo += " | Process null or exited";
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                debugInfo = $"Error accessing process: {ex.Message}";
                return IntPtr.Zero;
            }

            // Try MainWindowHandle first
            var h = proc.MainWindowHandle;
            if (h != IntPtr.Zero && IsWindowVisible(h))
            {
                var title = GetWindowTitle(h);
                debugInfo += $" | MainWindowHandle: '{title}'";
                return h;
            }
            
            debugInfo += " | No MainWindowHandle, enumerating...";

            // Enumerate all windows to find one owned by this process
            IntPtr found = IntPtr.Zero;
            var windowTitles = new List<string>();
            
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out int pid);
                if (pid == proc.Id)
                {
                    var title = GetWindowTitle(hWnd);
                    bool visible = IsWindowVisible(hWnd);
                    windowTitles.Add($"'{title}' visible={visible}");
                    
                    if (visible && found == IntPtr.Zero)
                    {
                        found = hWnd;
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (windowTitles.Count > 0)
                debugInfo += $" | Windows: {string.Join(", ", windowTitles)}";
            else
                debugInfo += " | No windows found";

            return found;
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Sets the specified screen as the primary monitor
        /// </summary>
        public static bool SetPrimaryMonitor(Screen targetScreen)
        {
            if (targetScreen.Primary)
                return true; // Already primary

            string targetDeviceName = targetScreen.DeviceName;
            int offsetX = targetScreen.Bounds.X;
            int offsetY = targetScreen.Bounds.Y;

            // We need to shift all monitors so the target becomes at (0,0)
            var screens = Screen.AllScreens;
            
            // First pass: update all monitors with NORESET flag
            foreach (var screen in screens)
            {
                var dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

                if (!EnumDisplaySettingsEx(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref dm, 0))
                    continue;

                // Shift position so target monitor ends up at (0,0)
                dm.dmPositionX = screen.Bounds.X - offsetX;
                dm.dmPositionY = screen.Bounds.Y - offsetY;
                dm.dmFields = DM_POSITION;

                uint flags = CDS_UPDATEREGISTRY | CDS_NORESET;
                if (screen.DeviceName == targetDeviceName)
                    flags |= CDS_SET_PRIMARY;

                ChangeDisplaySettingsEx(screen.DeviceName, ref dm, IntPtr.Zero, flags, IntPtr.Zero);
            }

            // Apply all changes at once
            int result = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
            return result == DISP_CHANGE_SUCCESSFUL;
        }
    }
}
