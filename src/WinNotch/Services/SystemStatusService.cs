using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace WinNotch.Services
{
    public class SystemStatusService
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;        // 0: Offline, 1: Online, 255: Unknown
            public byte BatteryFlag;         // 1: High, 2: Low, 4: Critical, 8: Charging, 128: No battery
            public byte BatteryLifePercent;  // 0-100, 255: Unknown
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_VOLUME_DOWN = 0xAE;
        private const byte VK_VOLUME_UP = 0xAF;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private readonly DispatcherTimer _pollTimer;
        private byte _lastAcStatus = 255;
        private bool _lastCapsLock;
        private bool _isFullscreen;
        private IntPtr _ownHwnd = IntPtr.Zero;

        public event Action<bool, int>? BatteryChargingChanged; // isCharging, percent
        public event Action<bool>? CapsLockChanged;              // isCapsLockOn
        public event Action<bool>? FullscreenChanged;            // isFullscreen
        public event Action<string, string>? AlertTriggered;      // message, symbol

        public bool AutoHideOnFullscreen { get; set; } = true;
        public int CurrentBatteryPercent { get; private set; } = 100;
        public bool IsCharging { get; private set; }

        public SystemStatusService()
        {
            try
            {
                _lastCapsLock = Console.CapsLock;
            }
            catch
            {
                _lastCapsLock = false;
            }

            // Check initial power status
            if (GetSystemPowerStatus(out var status))
            {
                _lastAcStatus = status.ACLineStatus;
                IsCharging = status.ACLineStatus == 1;
                CurrentBatteryPercent = status.BatteryLifePercent <= 100 ? status.BatteryLifePercent : 100;
            }

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _pollTimer.Tick += OnPollTick;
        }

        public void Start(IntPtr ownHwnd)
        {
            _ownHwnd = ownHwnd;
            _pollTimer.Start();
        }

        public void Stop()
        {
            _pollTimer.Stop();
        }

        private int _batteryCheckCounter = 0;

        private void OnPollTick(object? sender, EventArgs e)
        {
            // 1. Check Caps Lock
            try
            {
                bool currentCaps = Console.CapsLock;
                if (currentCaps != _lastCapsLock)
                {
                    _lastCapsLock = currentCaps;
                    CapsLockChanged?.Invoke(currentCaps);
                    AlertTriggered?.Invoke(currentCaps ? "CAPS LOCK ON" : "CAPS LOCK OFF", currentCaps ? "⇪" : "⇩");
                }
            }
            catch { }

            // 2. Check Power Status every 2 seconds (10 ticks)
            _batteryCheckCounter++;
            if (_batteryCheckCounter >= 10)
            {
                _batteryCheckCounter = 0;
                if (GetSystemPowerStatus(out var status))
                {
                    int pct = status.BatteryLifePercent <= 100 ? status.BatteryLifePercent : 100;
                    CurrentBatteryPercent = pct;
                    bool charging = status.ACLineStatus == 1;
                    IsCharging = charging;

                    if (_lastAcStatus != 255 && status.ACLineStatus != _lastAcStatus)
                    {
                        BatteryChargingChanged?.Invoke(charging, pct);
                        AlertTriggered?.Invoke(charging ? $"CHARGING · {pct}%" : $"ON BATTERY · {pct}%", charging ? "⚡" : "🔋");
                    }
                    _lastAcStatus = status.ACLineStatus;
                }
            }

            // 3. Check Fullscreen Foreground Window
            if (AutoHideOnFullscreen)
            {
                CheckFullscreen();
            }
        }

        private void CheckFullscreen()
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero || fg == _ownHwnd || fg == GetDesktopWindow() || fg == GetShellWindow())
            {
                if (_isFullscreen)
                {
                    _isFullscreen = false;
                    FullscreenChanged?.Invoke(false);
                }
                return;
            }

            // Exclude taskbar and desktop classes
            var sb = new StringBuilder(256);
            GetClassName(fg, sb, 256);
            var className = sb.ToString();
            if (className == "Shell_TrayWnd" || className == "Progman" || className == "WorkerW")
            {
                if (_isFullscreen)
                {
                    _isFullscreen = false;
                    FullscreenChanged?.Invoke(false);
                }
                return;
            }

            if (GetWindowRect(fg, out var rect))
            {
                int screenW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                int screenH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                bool isFs = rect.Left <= 0 && rect.Top <= 0 && rect.Right >= screenW && rect.Bottom >= screenH;
                if (isFs != _isFullscreen)
                {
                    _isFullscreen = isFs;
                    FullscreenChanged?.Invoke(_isFullscreen);
                }
            }
        }

        public static void VolumeUp()
        {
            keybd_event(VK_VOLUME_UP, 0, 0, UIntPtr.Zero);
            keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void VolumeDown()
        {
            keybd_event(VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
