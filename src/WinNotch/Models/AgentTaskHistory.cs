using System;

namespace WinNotch.Models
{
    public class AgentTaskHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AgentName { get; set; } = "agy";
        public string ActionName { get; set; } = "Task completed";
        public string DurationText { get; set; } = "1.2s";
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; } = true;

        public string FormattedTime => CompletedAt.ToString("HH:mm:ss");
        public string StatusSymbol => IsSuccess ? "✅" : "❌";
        public string StatusColor => IsSuccess ? "#10B981" : "#EF4444";
    }
}
