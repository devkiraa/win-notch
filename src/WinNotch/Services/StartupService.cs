using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace WinNotch.Services
{
    public static class StartupService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WinNotch";

        public static bool IsRunOnStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetRunOnStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return false;

                if (enable)
                {
                    var mainModule = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(mainModule))
                    {
                        if (mainModule.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            var dllPath = typeof(StartupService).Assembly.Location;
                            key.SetValue(AppName, $"\"{mainModule}\" exec \"{dllPath}\"");
                        }
                        else
                        {
                            key.SetValue(AppName, $"\"{mainModule}\"");
                        }
                        return true;
                    }
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartupService error: {ex.Message}");
            }

            return false;
        }
    }
}
