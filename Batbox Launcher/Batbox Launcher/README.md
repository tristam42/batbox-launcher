# Batbox Launcher

A Windows application that manages the launch and monitoring of the Batbox baseball simulation system.

## Features

### 🎮 Game Launch Management
- **One-click launch** - Click "Launch Game" to start the Kiosk application
- **Automatic detection** - Detects if Kiosk.exe and Baseball.exe are already running
- **Launch path fallback** - If the configured shortcut isn't found, falls back to `%LOCALAPPDATA%\Takoha.Kiosk\current\Kiosk.exe`
- **Window positioning** - Automatically moves game windows to the primary monitor after launch

### 📡 Device Connectivity Monitoring
- **Real-time status indicators** - Visual status lights for each configured device
- **Automatic ping checks** - Pings devices every 60 seconds
- **Configurable devices** - Add/remove devices with name and IP address
- **Skip on launch** - Option to skip specific devices during launch checks (still shows status)
- **Default devices:**
  - Pitching Machine (192.168.0.150)
  - LeftCam (10.10.1.1)
  - RightCam (10.11.1.1)

### 🖥️ Monitor Detection
- **Multi-monitor support** - Detects all connected monitors
- **Minimum monitor requirement** - Configurable minimum (default: 2 monitors)
- **Primary monitor check** - Warns if primary monitor is not on the left
- **Visual monitor layout** - Shows a diagram of your monitor arrangement
- **Quick access** - "Display Settings" button opens Windows display settings
- **Skip option** - Can skip monitor check for testing

### 🌐 Server Connectivity
- **Batbox server check** - Pings api.batbox.com to verify connectivity
- **Internet fallback check** - If server is offline, checks if internet is working
- **Offline mode support** - Game can still launch in offline mode
- **Auto-retry** - Checks every 3 minutes when online, every 10 seconds when offline

### 📐 Window Size Enforcement
- **Automatic correction** - Detects when Baseball or Kiosk windows are moved or resized
- **Event-driven** - Uses Windows hooks (no polling) for instant detection
- **Monitors both windows:**
  - **Baseball**: Move to (0,0) AND resize to 3840x1080 (spans dual monitors)
  - **Kiosk**: Move to (0,0) only (keeps its current size)
- **Manual trigger** - "Adjust Window" button to manually apply corrections to both windows
- **Enable/disable toggle** - Can be turned on/off in settings

### 🔽 System Tray Integration
- **Minimize to tray** - After successful launch, hides to system tray
- **Tray icon** - Shows Batbox icon with context menu
- **Balloon notification** - Shows status when minimizing
- **Quick restore** - Double-click tray icon or use "Show" menu
- **Clean exit** - "Exit" option in tray menu

### ⚙️ Settings Panel
Toggle settings panel with the "⚙ Settings" button:

| Setting | Description | Default |
|---------|-------------|---------|
| Kiosk Shortcut Path | Path to the .lnk shortcut file | `C:\Users\Batbox\Desktop\Kiosk.lnk` |
| Min Monitors | Minimum required monitors | 2 |
| Retry Interval | Seconds between retry attempts | 10 |
| Skip monitor check | Bypass monitor requirement | Off |
| Enforce window size | Auto-correct Baseball window | Off |
| Device list | IP addresses to ping before launch | 3 devices |

### 📋 Logging
- **Timestamped logs** - All events logged with timestamps
- **Color-coded** - INFO (gray), WARN (orange), ERROR (red)
- **Detailed diagnostics** - Monitor info, ping results, window positions
- **Status suppression** - Duplicate status messages are suppressed

## Configuration

Settings are saved to: `%APPDATA%\BatboxLauncher\config.json`

### Config File Structure
```json
{
  "LnkPath": "C:\\Users\\Batbox\\Desktop\\Kiosk.lnk",
  "KioskExeName": "Kiosk.exe",
  "BaseballExeName": "Baseball.exe",
  "MinMonitors": 2,
  "IntervalSeconds": 10,
  "SkipMonitorCheck": false,
  "EnforceWindowSize": false,
  "TargetWindowTitle": "Baseball",
  "TargetWindowX": 0,
  "TargetWindowY": 0,
  "TargetWindowWidth": 3840,
  "TargetWindowHeight": 1080,
  "Devices": [
    { "Name": "Pitching Machine", "Ip": "192.168.0.150", "Skip": false },
    { "Name": "LeftCam", "Ip": "10.10.1.1", "Skip": false },
    { "Name": "RightCam", "Ip": "10.11.1.1", "Skip": false }
  ]
}
```

> **Note:** `TargetWindowWidth` and `TargetWindowHeight` only apply to the Baseball window. The Kiosk window is only repositioned, not resized.

## UI Overview

```
┌─────────────────────────────────────────────────────────────┐
│  [▶ Launch Game] [✕ Abort]  │ Status Lights │  [⚙ Settings] │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Monitor Visualization          [Adjust Window]     │    │
│  │  ┌──────────┐ ┌──────────┐                          │    │
│  │  │ ★ Primary│ │Secondary │     [Display Settings]  │    │
│  │  │ 1920x1080│ │ 1920x1080│                          │    │
│  │  └──────────┘ └──────────┘                          │    │
│  └─────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│  Log:                                                       │
│  2024-01-05 20:00:00 [INFO] === Batbox Launcher Starting ===│
│  2024-01-05 20:00:01 [INFO] Pinging Servers... Online       │
│  ...                                                        │
└─────────────────────────────────────────────────────────────┘
```

## Requirements

- Windows 10/11
- .NET 9.0 Runtime
- Dual monitor setup (recommended)

## Version

tristam 2.0

