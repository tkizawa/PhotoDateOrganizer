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
            ExportSettings(SettingsFilePath, settings);
        }
        catch
        {
            // Ignore saving errors if unable to write
        }
    }

    public AppSettings ImportSettings(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("指定された設定ファイルが見つかりません。", filePath);
        }

        var json = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        return settings ?? throw new InvalidOperationException("設定ファイルの読み込みまたは解析に失敗しました。");
    }

    public void ExportSettings(string filePath, AppSettings settings)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
    }
}
