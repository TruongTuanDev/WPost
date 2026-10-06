using System;
using System.IO;
using System.Text.Json;

namespace WbTnvedManager.Models
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ContentBaseUrl { get; set; } = "https://content-api.wildberries.ru";
        public int RateLimitDelayMs { get; set; } = 350;
        public int BatchSize { get; set; } = 50;
        public bool AutoCheckErrors { get; set; } = true;
        public int ErrorPollIntervalMinutes { get; set; } = 5;

        private static string? _resolvedSettingsPath;
        private static string SettingsFilePath
        {
            get
            {
                if (_resolvedSettingsPath != null) return _resolvedSettingsPath;
                try
                {
                    var basePath = AppDomain.CurrentDomain.BaseDirectory;
                    var testFile = Path.Combine(basePath, "write_test_settings.tmp");
                    File.WriteAllText(testFile, "ok");
                    File.Delete(testFile);
                    _resolvedSettingsPath = Path.Combine(basePath, "app_settings.json");
                }
                catch
                {
                    var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WPost");
                    Directory.CreateDirectory(appDataDir);
                    _resolvedSettingsPath = Path.Combine(appDataDir, "app_settings.json");
                }
                return _resolvedSettingsPath;
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch
            {
                // Fallback to default
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore failure on save
            }
        }
    }
}
