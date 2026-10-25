using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinNotch.Models;
using WinNotch.Services;

namespace WinNotch.ViewModels
{
    public partial class ClipboardViewModel : ObservableObject
    {
        public ObservableCollection<ClipboardItem> Items { get; } = new();

        [ObservableProperty]
        private int _count;

        public void AddClip(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var existing = Items.FirstOrDefault(i => i.Content == text);
                if (existing != null)
                {
                    Items.Remove(existing);
                }

                Items.Insert(0, new ClipboardItem
                {
                    Content = text,
                    CopiedAt = DateTime.Now
                });

                while (Items.Count > 25)
                {
                    Items.RemoveAt(Items.Count - 1);
                }

                Count = Items.Count;
            });
        }

        [RelayCommand]
        public async Task CopyClipAsync(ClipboardItem? item)
        {
            if (item == null) return;
            ClipboardService.SetText(item.Content);
            item.IsJustCopied = true;
            await Task.Delay(1500);
            item.IsJustCopied = false;
        }

        [RelayCommand]
        public void DeleteClip(ClipboardItem? item)
        {
            if (item == null) return;
            Items.Remove(item);
            Count = Items.Count;
        }

        [RelayCommand]
        public void ClearAll()
        {
            Items.Clear();
            Count = 0;
        }
    }
}
