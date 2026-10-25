using CommunityToolkit.Mvvm.ComponentModel;

namespace WinNotch.Models
{
    public partial class InstalledAgent : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = "Terminal Agent";
        public string ExecutablePath { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isInstalled;

        [ObservableProperty]
        private bool _isRunning;

        public string AccentColor { get; set; } = "#8B5CF6";
        public string BgColor { get; set; } = "#140E28";
        public string LogoKey { get; set; } = "Antigravity";

        public string StatusText => IsRunning ? "● ACTIVE" : (IsInstalled ? "STANDBY" : "NOT FOUND");
        public string StatusColor => IsRunning ? "#10B981" : (IsInstalled ? "#A1A1AA" : "#3F3F46");
        public string StatusBg => IsRunning ? "#0A2016" : "#141418";
    }
}
