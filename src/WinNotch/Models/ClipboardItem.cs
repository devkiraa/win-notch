using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinNotch.Models
{
    public partial class ClipboardItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Content { get; set; } = string.Empty;
        public DateTime CopiedAt { get; set; } = DateTime.Now;

        [ObservableProperty]
        private bool _isJustCopied;

        public string PreviewText => Content.Replace("\r", " ").Replace("\n", " ").Trim();

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - CopiedAt;
                if (diff.TotalSeconds < 60) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }
    }
}
