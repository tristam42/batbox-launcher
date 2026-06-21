# Batbox Launcher

A Windows application that manages the launch and monitoring of the Batbox baseball simulation system.

## Features

### 🎮 Game Launch Management
- **One-click launch** - Click "Launch Game" to start the Kiosk application
- **Automatic detection** - Detects if Kiosk.exe and Baseball.exe are already running
- **Launch path fallback** - If the configured shortcut isn't found, falls back to `%LOCALAPPDATA%\Takoha.Kiosk\current\Kiosk.exe`
- **Window positioning** - Automatically moves game windows to the primary monitor after launch
- **Running-state tracking** - A background check (every 30 seconds) verifies that Kiosk.exe and Baseball.exe are still running
- **Auto-return to ready** - When the game closes, the launcher restores its window, exits tray mode, stops window enforcement, and resets to the ready state

### 📡 Device Connectivity Monitoring
- **Real-time status indicators** - Visual status lights for each configured device
- **Real ICMP ping checks** - Actually pings each device IP (not cached state)
- **Adaptive interval** - Checks every 60 seconds when healthy; automatically speeds up to every 5 seconds while anything is offline, then returns to 60 seconds once recovered
- **Configurable devices** - Add/remove devices with name and IP address
- **Skip on launch** - Option to skip specific devices during launch checks (skipped devices are still pinged for status, they just don't block launch readiness)
- **Default devices:**
  - Pitching Machine (192.168.0.150)
  - LeftCam (10.10.1.1)
  - RightCam (10.11.1.1)

> **Note:** Connectivity is verified with ICMP ping. A device that is powered on but blocks ping will still report as OFFLINE.

### 🔌 Offline Socket Diagnostics (Optional Auto-Kill)
- **Diagnostics on disconnect** - When a device transitions to OFFLINE, the launcher runs `netstat -ano`, finds sockets bound to that device IP, and logs the protocol, PID, process name, local port, and endpoints
- **Auto-kill toggle** - Disabled by default. When enabled, it logs exactly which PIDs/ports it will terminate, then kills those processes to release the socket
- **Safety guards** - Never targets system PIDs (≤ 4) or the launcher's own process
- **Diagnostics-only mode** - When the toggle is off, it logs the bindings and `Auto-kill disabled ... diagnostics only.` without killing anything

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
- **Live enable/disable** - Toggling "Enforce window size" in settings starts/stops monitoring immediately, even while the game is already running (no need to enable it before launch)

### 🔔 On-Screen Status Notifications
- **Always-on-top alerts** - A top-center notification window forces itself above other apps (re-asserts topmost while visible)
- **Persistent while offline** - When devices/monitor are offline, the alert stays on screen and updates its contents until everything is healthy again, then it auto-closes
- **Enumerated detail** - Shows a `Status: OFFLINE` header followed by a bullet list of each offline item (device name + IP, or monitor check)

### 🔽 System Tray Integration
- **Minimize to tray** - After successful launch, hides to system tray
- **Reliable minimize** - Minimizing the restored/maximized window after launch sends it back to the tray
- **Tray icon** - Shows Batbox icon with context menu
- **Quick restore** - Double-click tray icon or use "Show" menu
- **Auto-restore on game close** - If the game closes while in tray, the launcher window reappears automatically
- **Clean exit** - "Exit" option in tray menu

### ⚙️ Settings Panel
Toggle settings panel with the "⚙ Settings" button:

| Setting | Description | Default |
|---------|-------------|---------|
| Kiosk Shortcut Path | Path to the .lnk shortcut file | `C:\Users\Batbox\Desktop\Kiosk.lnk` |
| Min Monitors | Minimum required monitors | 2 |
| Retry Interval | Seconds between launch retry attempts | 10 |
| Skip monitor check on launch | Bypass monitor requirement | Off |
| Enforce window size (3840x1080) | Auto-correct Baseball/Kiosk windows (live toggle) | Off |
| Auto-kill offline device socket bindings | On disconnect, kill processes holding sockets to that device IP | Off |
| Device list | Devices to monitor (name, IP, skip-on-launch) | 3 devices |

### 📋 Logging
- **Timestamped logs** - All events logged with timestamps
- **Color-coded** - INFO (gray), WARN (orange), ERROR (red)
- **Detailed diagnostics** - Monitor info, ping results, window positions, socket bindings on disconnect
- **Change-based logging** - Status is logged on change rather than every cycle, to avoid noise
- **File logs** - Written to `%APPDATA%\BatboxLauncher\Logs\launcher_YYYYMMDD.log` with rotation (3 MB cap, last few files kept)

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
  "AutoKillCameraSocketBindings": false,
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
>
> **Note:** `AutoKillCameraSocketBindings` applies to **all** configured devices (not just cameras), despite the legacy key name. Leave it `false` for diagnostics-only behavior.

## Monitoring Intervals

| Check | Healthy | Degraded / Offline |
|-------|---------|--------------------|
| Device pings | Every 60s | Every 5s until all recover |
| Batbox server | Every 3 min | Every 10s while offline |
| Game process (Kiosk/Baseball) | Every 30s | — |
| Window size/position | Instant (event hook, when enabled) | — |

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

