using System;
using System.IO;
using System.Text.Json;

namespace WinNotch.Services
{
    public class UserSettings
    {
        public double CollapsedOpacity { get; set; } = 0.45;
        public bool RequireCtrlToClick { get; set; } = true;
    }

    public static class SettingsStorageService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinNotch"
        );
        private static readonly string FilePath = Path.Combine(AppDataFolder, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static UserSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new UserSettings();
                }

                var json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                return settings ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public static void SaveSettings(UserSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                var tempFile = FilePath + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, FilePath, overwrite: true);
            }
            catch
            {
            }
        }
    }
}
