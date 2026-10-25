using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinNotch.Models
{
    public partial class AgentInfo : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public int ProcessId { get; set; }

        [ObservableProperty]
        private string _agentName = "agy";

        [ObservableProperty]
        private string _currentAction = "Executing task...";

        [ObservableProperty]
        private string _targetDetails = string.Empty;

        [ObservableProperty]
        private bool _isRunning = true;

        public DateTime StartedAt { get; set; } = DateTime.Now;

        [ObservableProperty]
        private string _elapsedText = "0s";

        [ObservableProperty]
        private string _statusColor = "#3478F6";

        public void UpdateElapsed()
        {
            var diff = DateTime.Now - StartedAt;
            if (diff.TotalSeconds < 60)
            {
                ElapsedText = $"{(int)diff.TotalSeconds}s";
            }
            else if (diff.TotalMinutes < 60)
            {
                ElapsedText = $"{(int)diff.TotalMinutes}m {diff.Seconds}s";
            }
            else
            {
                ElapsedText = $"{(int)diff.TotalHours}h {diff.Minutes}m";
            }
        }
    }
}
