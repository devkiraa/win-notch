using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinNotch.Models
{
    public partial class TodoItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private bool _isCompleted;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
