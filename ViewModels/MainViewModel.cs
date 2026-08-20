using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoDateOrganizer.Models;
using PhotoDateOrganizer.Services;

namespace PhotoDateOrganizer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPhotoOrganizerService _organizerService;
    private CancellationTokenSource? _cancellationTokenSource;

    public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
    public string AppVersionDisplay => $"v{AppVersion}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartOrganizingCommand))]
    private string _sourceDirectory = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartOrganizingCommand))]
    private string _destinationDirectory = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(StartOrganizingCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelOrganizingCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCloudModeHydrateAndDehydrate))]
    [NotifyPropertyChangedFor(nameof(IsCloudModeSkip))]
    [NotifyPropertyChangedFor(nameof(IsCloudModeKeepLocal))]
    [NotifyPropertyChangedFor(nameof(SkipCloudOnlyFiles))]
    private CloudFileHandlingMode _cloudFileMode = CloudFileHandlingMode.HydrateAndDehydrate;

    /// <summary>
    /// 一時ダウンロードして整理し、完了後にクラウド専用に戻す
    /// </summary>
    public bool IsCloudModeHydrateAndDehydrate
    {
        get => CloudFileMode == CloudFileHandlingMode.HydrateAndDehydrate;
        set
        {
            if (value)
            {
                CloudFileMode = CloudFileHandlingMode.HydrateAndDehydrate;
            }
        }
    }

    /// <summary>
    /// クラウド専用ファイルはスキップする
    /// </summary>
    public bool IsCloudModeSkip
    {
        get => CloudFileMode == CloudFileHandlingMode.Skip;
        set
        {
            if (value)
            {
                CloudFileMode = CloudFileHandlingMode.Skip;
            }
        }
    }

    /// <summary>
    /// クラウドからダウンロードしてローカルにも実体を保持する
    /// </summary>
    public bool IsCloudModeKeepLocal
    {
        get => CloudFileMode == CloudFileHandlingMode.DownloadAndKeep;
        set
        {
            if (value)
            {
                CloudFileMode = CloudFileHandlingMode.DownloadAndKeep;
            }
        }
    }

    /// <summary>
    /// 以前のプロパティとの下位互換性
    /// </summary>
    public bool SkipCloudOnlyFiles
    {
        get => CloudFileMode == CloudFileHandlingMode.Skip;
        set => CloudFileMode = value ? CloudFileHandlingMode.Skip : CloudFileHandlingMode.HydrateAndDehydrate;
    }


    public bool IsIdle => !IsProcessing;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMax = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    private double _progressPercentage;

    public string ProgressPercentageText => $"{ProgressPercentage:0.0}%";

    [ObservableProperty]
    private string _currentFile = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "準備完了: ソースフォルダと出力先フォルダを選択してください。";

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _processedFiles;

    [ObservableProperty]
    private int _copiedFiles;

    [ObservableProperty]
    private int _skippedFiles;

    [ObservableProperty]
    private int _errorFiles;

    [ObservableProperty]
    private int _fallbackFiles;

    [ObservableProperty]
    private bool _hasFallbackFiles;

    [ObservableProperty]
    private string _fallbackNoticeMessage = string.Empty;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public event Func<Task<string?>>? RequestFolderPickerAsync;
    public event Func<Task<string?>>? RequestImportFilePickerAsync;
    public event Func<Task<string?>>? RequestExportFilePickerAsync;

    public MainViewModel(IPhotoOrganizerService organizerService)
    {
        _organizerService = organizerService;
    }

    public MainViewModel() : this(new PhotoOrganizerService())
    {
    }

    private bool CanStartOrganizing() =>
        !IsProcessing &&
        !string.IsNullOrWhiteSpace(SourceDirectory) &&
        !string.IsNullOrWhiteSpace(DestinationDirectory) &&
        Directory.Exists(SourceDirectory);

    private bool CanCancelOrganizing() => IsProcessing;

    [RelayCommand]
    private async Task SelectSourceFolderAsync()
    {
        if (RequestFolderPickerAsync != null)
        {
            var folder = await RequestFolderPickerAsync();
            if (!string.IsNullOrEmpty(folder))
            {
                SourceDirectory = folder;
                AddLog(LogLevel.Info, $"ソースフォルダを設定: {folder}");
            }
        }
    }

    [RelayCommand]
    private async Task SelectDestinationFolderAsync()
    {
        if (RequestFolderPickerAsync != null)
        {
            var folder = await RequestFolderPickerAsync();
            if (!string.IsNullOrEmpty(folder))
            {
                DestinationDirectory = folder;
                AddLog(LogLevel.Info, $"出力先フォルダを設定: {folder}");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartOrganizing))]
    private async Task StartOrganizingAsync()
    {
        if (!Directory.Exists(SourceDirectory))
        {
            AddLog(LogLevel.Error, "エラー: ソースフォルダが存在しません。");
            return;
        }

        IsProcessing = true;
        IsIndeterminate = true;
        ProgressValue = 0;
        ProgressMax = 100;
        ProgressPercentage = 0;
        TotalFiles = 0;
        ProcessedFiles = 0;
        CopiedFiles = 0;
        SkippedFiles = 0;
        ErrorFiles = 0;
        FallbackFiles = 0;
        HasFallbackFiles = false;
        FallbackNoticeMessage = string.Empty;
        CurrentFile = string.Empty;

        _cancellationTokenSource = new CancellationTokenSource();

        var progressHandler = new Progress<OrganizeProgress>(OnProgressReported);

        try
        {
            AddLog(LogLevel.Info, $"整理処理を開始します...");
            var result = await _organizerService.OrganizeAsync(
                SourceDirectory,
                DestinationDirectory,
                progressHandler,
                _cancellationTokenSource.Token,
                CloudFileMode);


            if (result.IsCancelled)
            {
                StatusMessage = "処理がキャンセルされました。";
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                StatusMessage = $"エラー: {result.ErrorMessage}";
            }
            else
            {
                if (result.FallbackCount > 0)
                {
                    HasFallbackFiles = true;
                    FallbackNoticeMessage = $"💡 注意: {result.FallbackCount} 件のファイルはExifメタデータが無いため、ファイル名またはタイムスタンプから判定しました。";
                    StatusMessage = $"整理完了: {result.CopiedCount} 件コピー (うち {result.FallbackCount} 件はExif欠損), {result.SkippedCount} 件スキップ ({result.Duration:mm\\:ss})";
                }
                else
                {
                    StatusMessage = $"整理完了: {result.CopiedCount} 件コピー, {result.SkippedCount} 件スキップ, {result.ErrorCount} 件エラー ({result.Duration:mm\\:ss})";
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "処理がキャンセルされました。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラーが発生しました: {ex.Message}";
            AddLog(LogLevel.Error, $"例外エラー: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            IsIndeterminate = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelOrganizing))]
    private void CancelOrganizing()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            StatusMessage = "キャンセルを要求中...";
            AddLog(LogLevel.Warning, "ユーザーによるキャンセルが要求されました。");
            _cancellationTokenSource.Cancel();
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }

    [RelayCommand]
    private void OpenDestinationFolder()
    {
        if (Directory.Exists(DestinationDirectory))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DestinationDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddLog(LogLevel.Error, $"フォルダを開けませんでした: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        if (RequestImportFilePickerAsync == null) return;

        try
        {
            var filePath = await RequestImportFilePickerAsync();
            if (string.IsNullOrEmpty(filePath)) return;

            var settingsService = new SettingsService();
            var settings = settingsService.ImportSettings(filePath);

            if (!string.IsNullOrEmpty(settings.SourceDirectory))
            {
                SourceDirectory = settings.SourceDirectory;
            }
            if (!string.IsNullOrEmpty(settings.DestinationDirectory))
            {
                DestinationDirectory = settings.DestinationDirectory;
            }
            SkipCloudOnlyFiles = settings.SkipCloudOnlyFiles;

            AddLog(LogLevel.Info, $"設定をインポートしました: {filePath}");
            StatusMessage = $"設定をインポートしました ({Path.GetFileName(filePath)})";
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"設定のインポートに失敗しました: {ex.Message}");
            StatusMessage = $"設定インポートエラー: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        if (RequestExportFilePickerAsync == null) return;

        try
        {
            var filePath = await RequestExportFilePickerAsync();
            if (string.IsNullOrEmpty(filePath)) return;

            var settingsService = new SettingsService();
            var currentSettings = settingsService.LoadSettings();
            currentSettings.SourceDirectory = SourceDirectory ?? string.Empty;
            currentSettings.DestinationDirectory = DestinationDirectory ?? string.Empty;
            currentSettings.SkipCloudOnlyFiles = SkipCloudOnlyFiles;

            settingsService.ExportSettings(filePath, currentSettings);

            AddLog(LogLevel.Info, $"設定をエクスポートしました: {filePath}");
            StatusMessage = $"設定をエクスポートしました ({Path.GetFileName(filePath)})";
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"設定のエクスポートに失敗しました: {ex.Message}");
            StatusMessage = $"設定エクスポートエラー: {ex.Message}";
        }
    }


    private void OnProgressReported(OrganizeProgress p)
    {
        if (p.Phase == OrganizePhase.Scanning)
        {
            IsIndeterminate = true;
        }
        else
        {
            IsIndeterminate = false;
        }

        if (p.TotalCount > 0)
        {
            TotalFiles = p.TotalCount;
            ProgressMax = p.TotalCount;
        }

        ProcessedFiles = p.ProcessedCount;
        ProgressValue = p.ProcessedCount;
        ProgressPercentage = Math.Round(p.Percentage, 1);
        CopiedFiles = p.CopiedCount;
        SkippedFiles = p.SkippedCount;
        ErrorFiles = p.ErrorCount;
        FallbackFiles = p.FallbackCount;

        if (p.FallbackCount > 0)
        {
            HasFallbackFiles = true;
            FallbackNoticeMessage = $"💡 注意: {p.FallbackCount} 件のファイルはExif欠損のため、ファイル名またはタイムスタンプから判定中です。";
        }

        if (!string.IsNullOrEmpty(p.CurrentFilePath))
        {
            CurrentFile = p.CurrentFilePath;
        }

        if (!string.IsNullOrEmpty(p.StatusMessage))
        {
            StatusMessage = p.StatusMessage;
        }

        if (p.NewLogEntry != null)
        {
            Logs.Insert(0, p.NewLogEntry);
            while (Logs.Count > 500)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        }
    }

    private void AddLog(LogLevel level, string message, string? filePath = null)
    {
        Logs.Insert(0, new LogEntry
        {
            Level = level,
            Message = message,
            FilePath = filePath
        });

        while (Logs.Count > 500)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }
}
