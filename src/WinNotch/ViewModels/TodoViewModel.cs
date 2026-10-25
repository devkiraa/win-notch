using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinNotch.Models;
using WinNotch.Services;

namespace WinNotch.ViewModels
{
    public partial class TodoViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TodoItem> _items = new();

        [ObservableProperty]
        private string _newTodoText = string.Empty;

        [ObservableProperty]
        private int _activeCount;

        [ObservableProperty]
        private int _completedCount;

        public TodoViewModel()
        {
            var saved = TodoStorageService.LoadTodos();
            foreach (var item in saved)
            {
                item.PropertyChanged += OnItemPropertyChanged;
                Items.Add(item);
            }
            UpdateCounts();
            Save();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TodoItem.IsCompleted))
            {
                UpdateCounts();
                Save();
            }
        }

        [RelayCommand]
        private void AddTodo()
        {
            if (string.IsNullOrWhiteSpace(NewTodoText)) return;

            var newItem = new TodoItem
            {
                Text = NewTodoText.Trim(),
                IsCompleted = false
            };
            newItem.PropertyChanged += OnItemPropertyChanged;
            Items.Insert(0, newItem);
            NewTodoText = string.Empty;
            UpdateCounts();
            Save();
        }

        [RelayCommand]
        private void DeleteTodo(TodoItem? item)
        {
            if (item == null) return;
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
            UpdateCounts();
            Save();
        }

        [RelayCommand]
        private void ClearCompleted()
        {
            var toRemove = Items.Where(t => t.IsCompleted).ToList();
            foreach (var item in toRemove)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                Items.Remove(item);
            }
            UpdateCounts();
            Save();
        }

        private void UpdateCounts()
        {
            ActiveCount = Items.Count(t => !t.IsCompleted);
            CompletedCount = Items.Count(t => t.IsCompleted);
        }

        private void Save()
        {
            TodoStorageService.SaveTodos(Items);
        }
    }
}
