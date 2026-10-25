using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WinNotch.Services
{
    public class ClipboardService
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private IntPtr _hwnd = IntPtr.Zero;
        private HwndSource? _hwndSource;
        private string _lastCopiedText = string.Empty;

        public event Action<string>? ClipboardTextChanged;

        public void Start(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);
            AddClipboardFormatListener(_hwnd);
        }

        public void Stop()
        {
            if (_hwnd != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_hwnd);
                _hwndSource?.RemoveHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(text) && text != _lastCopiedText)
                        {
                            _lastCopiedText = text;
                            ClipboardTextChanged?.Invoke(text);
                        }
                    }
                }
                catch
                {
                    // Clipboard may be locked by another application
                }
            }
            return IntPtr.Zero;
        }

        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetDataObject(text, true);
            }
            catch { }
        }
    }
}
