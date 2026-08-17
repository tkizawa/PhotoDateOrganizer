using System;

namespace PhotoDateOrganizer.Models;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; } = DateTime.Now;
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }

    public string TimestampString => Timestamp.ToString("HH:mm:ss");

    public string LevelBadge => Level switch
    {
        LogLevel.Success => "✓ 成功",
        LogLevel.Warning => "⚠ 警告",
        LogLevel.Error => "✕ エラー",
        _ => "ℹ 情報"
    };

#if HAS_WINUI
    public Microsoft.UI.Xaml.Media.SolidColorBrush LevelBrush => Level switch
    {
        LogLevel.Success => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65)),   // Green #107C41
        LogLevel.Warning => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 216, 59, 1)),    // Amber #D83B01
        LogLevel.Error => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 17, 35)),    // Red #E81123
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212))                  // Accent Blue #0078D4
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush LevelBackgroundBrush => Level switch
    {
        LogLevel.Success => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 16, 124, 65)),
        LogLevel.Warning => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 216, 59, 1)),
        LogLevel.Error => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 232, 17, 35)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 0, 120, 212))
    };
#endif
}
