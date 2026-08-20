using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PhotoDateOrganizer.Models;

namespace PhotoDateOrganizer.Services;

/// <summary>
/// アプリケーション設定の保存・読み込み・インポート・エクスポートを管理するサービスクラス
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoDateOrganizer");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    // 日本語文字列を Unicode エスケープせず可視テキストとして UTF-8 保存するための JSON シリアライズ設定
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string GetSettingsFilePath() => SettingsFilePath;
    public static string GetSettingsDirectory() => SettingsDirectory;

    /// <summary>
    /// 設定ファイルから設定情報を読み込みます。ファイルが存在しない・破損している場合はデフォルト値を返します。
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // 読み込み失敗時はデフォルト設定を使用
        }

        return new AppSettings();
    }

    /// <summary>
    /// 現在の設定情報を既定のローカル設定ファイルに保存します。
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            ExportSettings(SettingsFilePath, settings);
        }
        catch
        {
            // 保存エラーは無視
        }
    }

    /// <summary>
    /// 指定されたパスの JSON ファイルから設定をインポートします。
    /// </summary>
    public AppSettings ImportSettings(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("指定された設定ファイルが見つかりません。", filePath);
        }

        var json = File.ReadAllText(filePath, Encoding.UTF8);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        return settings ?? throw new InvalidOperationException("設定ファイルの読み込みまたは解析に失敗しました。");
    }

    /// <summary>
    /// 指定されたパスに設定情報を JSON (UTF-8) 形式でエクスポート・保存します。
    /// </summary>
    public void ExportSettings(string filePath, AppSettings settings)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
