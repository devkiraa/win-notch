using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinNotch.Models;

namespace WinNotch.Services
{
    public static class TodoStorageService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinNotch"
        );
        private static readonly string FilePath = Path.Combine(AppDataFolder, "todos.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static List<TodoItem> LoadTodos()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return GetDefaultTodos();
                }

                var json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<TodoItem>>(json, JsonOptions);
                if (items == null || items.Count == 0)
                {
                    return GetDefaultTodos();
                }
                return items;
            }
            catch
            {
                return GetDefaultTodos();
            }
        }

        public static void SaveTodos(IEnumerable<TodoItem> todos)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var json = JsonSerializer.Serialize(todos, JsonOptions);
                var tempFile = FilePath + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, FilePath, overwrite: true);
            }
            catch
            {
                // Ignore transient write errors
            }
        }

        private static List<TodoItem> GetDefaultTodos()
        {
            return new List<TodoItem>
            {
                new TodoItem { Text = "Welcome to WinNotch Dynamic Island", IsCompleted = false },
                new TodoItem { Text = "Click music widget to expand player", IsCompleted = false },
                new TodoItem { Text = "Toggle startup in settings", IsCompleted = true }
            };
        }
    }
}
