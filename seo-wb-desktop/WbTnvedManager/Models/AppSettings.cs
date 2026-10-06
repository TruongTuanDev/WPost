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

        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "app_settings.json"
        );

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
