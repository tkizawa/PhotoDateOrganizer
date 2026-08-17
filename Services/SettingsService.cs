using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using PhotoDateOrganizer.Models;

namespace PhotoDateOrganizer.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoDateOrganizer");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string GetSettingsFilePath() => SettingsFilePath;
    public static string GetSettingsDirectory() => SettingsDirectory;

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // If loading fails, return default settings
        }

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore saving errors if unable to write
        }
    }
}
