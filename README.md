<div align="center">

# 🏝️ WinNotch

### macOS Dynamic Island & AI Coding Agent HUD for Windows 10 & 11

[![.NET 8](https://img.shields.io/badge/.NET-8.0_WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](LICENSE)
[![Architecture](https://img.shields.io/badge/Architecture-x64-black?style=for-the-badge)]()
[![Status](https://img.shields.io/badge/Status-Active%20v2.0-10B981?style=for-the-badge)]()

<p align="center">
  <b>A lightweight, ultra-fluid desktop Dynamic Island that floats flush at the top edge of your screen.</b><br/>
  Track live AI coding agents (<code>agy</code>, Antigravity, Claude Code), control music with a realistic spinning vinyl record, run Pomodoro focus sprints, manage clipboard history, and monitor system activities — all from a native, hardware-accelerated OLED black capsule.
</p>

</div>

---

## ✨ Features at a Glance

### 🤖 1. AI Coding Agent Live HUD
Built specifically for developers running agentic AI workflows:
- **Terminal & IDE Tracking**: Monitors background terminal agents (`agy`, Claude Code, Aider) and Antigravity IDE brain sessions.
- **Dynamic Activity Pill**: When any agent is executing commands or modifying code, the collapsed capsule morphs into a live activity pill (`🤖 agy: Compiling build · 1 run`).
- **Instant Micro-Alerts**: Automatic 2.5s notification pill whenever a tool or step finishes (`✅ Antigravity: Task Succeeded`).
- **Agents Dashboard**: View active agent PIDs, live elapsed runtime, current tool actions, recent step history, and kill runaway processes with 1 click.

### 🎵 2. Music Player & Realistic Spinning Vinyl Record
- **Universal Media Sync**: Connects via native Windows `GlobalSystemMediaTransportControlsSessionManager` (GSMTC) with Spotify, Apple Music, YouTube in Chrome/Edge, Tidal, and VLC.
- **Spinning Vinyl Disc (76px)**: Photorealistic disc with concentric microgrooves, metallic spindle hole, light reflection sheen, and hardware-clipped circular album artwork.
- **Continuous Playback Sync**: Smoothly pauses at the exact angular rotation when paused, and resumes without angle jumps.
- **Interactive Scrubber**: Click anywhere on the progress bar to seek track position.
- **Collapsed Mini Disc**: 18px rotating mini disc in the collapsed pill with animated 3-bar equalizer waveform.

### ⏱️ 3. Focus & Pomodoro Countdown
- **Quick Focus Presets**: One-tap `5m`, `15m`, `25m`, and `45m` Pomodoro presets.
- **Digital Display**: 40px bold countdown with integrated Apple-style progress bar.
- **Collapsed Pill Counter**: Live countdown displays directly in the collapsed notch (`⏱ 24:59`) while working.
- **Sound Alert**: System chime notifications when timer completes.

### 📋 4. Clipboard History Drawer
- **Win32 OS Listener**: Captures text copied across Windows via `AddClipboardFormatListener`.
- **Clips Manager**: Stores recent clips with relative time stamps (`Just now`, `5m ago`).
- **1-Click Restore**: Click "Copy" to put any clip back onto your active clipboard with green `Copied!` feedback.

### ✅ 5. Task & Todo Checklist
- **Persistent Storage**: Saved atomically to `%APPDATA%\WinNotch\todos.json`.
- **Fast Entry**: Type task, press <kbd>Enter</kbd> to add.
- **Task Management**: Checkbox with dynamic strike-through animation, active task badge in tab header, and one-click "Clear completed".

### ⚡ 6. System Live Activities & Smart Controls
- **Caps Lock Micro-Pill**: Immediate alert on key toggle (`⇪ CAPS LOCK ON / OFF`).
- **Battery & Charger Pill**: Real-time notification when power adapter is plugged or unplugged (`⚡ CHARGING · 85%`).
- **Scroll Wheel Volume Control**: Hover anywhere over the notch (collapsed or expanded) and scroll mouse wheel up/down to adjust system volume.
- **Fullscreen Auto-Hide**: Automatically conceals the notch when running fullscreen games or watching movies (YouTube, VLC, Netflix).
- **Repositioning & Reset**:
  - Hold <kbd>Ctrl</kbd> + drag notch to move anywhere across multiple displays.
  - 1-click **Reset** button in Settings with Quartic-eased gliding animation back to primary screen top-center.

---

## 📐 Architecture & Technology Stack

| Layer | Technology | Details |
|---|---|---|
| **Platform** | C# .NET 8 (WPF) | Target: `net8.0-windows10.0.19041.0` |
| **Window Subsystem** | Win32 Interop | `WS_EX_TOPMOST`, `WS_EX_TOOLWINDOW`, `AllowsTransparency` |
| **Media Engine** | WinRT GSMTC | `GlobalSystemMediaTransportControlsSessionManager` |
| **System Events** | Win32 User32/Kernel32 | `GetSystemPowerStatus`, `GetForegroundWindow`, `keybd_event`, `AddClipboardFormatListener` |
| **State Machine** | CommunityToolkit.Mvvm | Observable properties, Relay commands, event-driven architecture |
| **Design System** | Apple Dynamic Island | Solid `#000000`, 1px `#1C1C1E` rim, `#3478F6` blue accent, `#8B5CF6` agent purple |
| **Footprint** | Ultra-Low Resources | ~60 MB RAM, <0.2% idle CPU, zero-delay hardware rendering |

---

## 🚀 Getting Started

### Prerequisites
- **Operating System**: Windows 10 (19041+) or Windows 11
- **Runtime**: [.NET 8 Desktop Runtime or SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Quick Launch
1. Clone or download this repository:
   ```bash
   git clone https://github.com/devkiraa/win-notch.git
   cd win-notch
   ```
2. Run the interactive launcher:
   ```cmd
   run.bat
   ```
   Or double-click `run.vbs` to launch silently in the background without a command prompt window.

3. To stop WinNotch:
   ```cmd
   stop.bat
   ```

---

## 🛠️ Building from Source

```powershell
# Restore dependencies and build optimized Release binary
dotnet build src/WinNotch/WinNotch.csproj -c Release
```

The output executable and dependencies will be compiled to:
```
src/WinNotch/bin/Release/net8.0-windows10.0.19041.0/
```

### Windows 11 Smart App Control Note
If Windows 11 Smart App Control (SAC) is enabled on your device:
1. Enable **Developer Mode** in Windows Settings:
   - *Settings* → *System* → *For developers* → Toggle **Developer Mode: On**.
2. WinNotch runs cleanly with SAC remaining **On** via developer mode boot-time exemptions.

---

## ⌨️ Keyboard & Mouse Shortcuts

| Action | Shortcut / Gesture |
|---|---|
| **Expand Notch** | Left-click on collapsed pill |
| **Collapse Notch** | Left-click outside notch or press <kbd>Esc</kbd> |
| **Reposition Notch** | Hold <kbd>Ctrl</kbd> + Click & Drag anywhere |
| **Adjust System Volume** | Mouse wheel scroll up / down over notch |
| **Seek Track** | Click anywhere along music progress bar |
| **Add New Todo** | Type in box and press <kbd>Enter</kbd> |

---

## 📁 Repository Structure

```
win-notch/
├── src/
│   └── WinNotch/
│       ├── WinNotch.csproj           # .NET 8 WPF Project
│       ├── App.xaml / App.xaml.cs    # Entry point & dependency injection
│       ├── MainWindow.xaml           # Dynamic Island vector layout & XAML animations
│       ├── MainWindow.xaml.cs        # Drag, resize, volume scroll, fullscreen watcher
│       ├── Converters.cs             # WPF value converters
│       ├── ViewModels/
│       │   ├── MainViewModel.cs      # Master state machine
│       │   ├── AgentViewModel.cs     # AI coding agent monitor
│       │   ├── MediaViewModel.cs     # WinRT media playback & seeking
│       │   ├── TimerViewModel.cs     # Focus & Pomodoro countdown
│       │   ├── ClipboardViewModel.cs # Clipboard history manager
│       │   └── TodoViewModel.cs      # Persistent todo list
│       ├── Services/
│       │   ├── CodingAgentService.cs # agy & Antigravity transcript watcher
│       │   ├── MediaService.cs       # WinRT GSMTC media session listener
│       │   ├── SystemStatusService.cs# Battery, Caps Lock, Fullscreen & Volume
│       │   ├── ClipboardService.cs   # Win32 clipboard format listener
│       │   ├── StartupService.cs     # Windows Run registry key toggle
│       │   └── TodoStorageService.cs # AppData JSON persistence
│       └── Models/
│           ├── AgentInfo.cs
│           ├── AgentTaskHistory.cs
│           ├── ClipboardItem.cs
│           ├── TodoItem.cs
│           └── NotchState.cs
├── run.bat                           # Interactive launcher
├── run.vbs                           # Silent background runner
├── stop.bat                          # Process termination script
├── .gitignore
└── README.md
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

<div align="center">
  <sub>Crafted with ❤️ for developers by devkiraa</sub>
</div>
