using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PhotoDateOrganizer.Models;
using PhotoDateOrganizer.Services;
using PhotoDateOrganizer.ViewModels;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PhotoDateOrganizer;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly SettingsService _settingsService = new();

    // ウィンドウの通常状態（Restored）における最新の位置とサイズをキャッシュ
    private int _savedWindowX = -1;
    private int _savedWindowY = -1;
    private int _savedWindowWidth = 1100;
    private int _savedWindowHeight = 750;

    public MainWindow()
    {
        this.InitializeComponent();

        // Mica バックドロップを適用（サポート環境のみ）
        try
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // 未サポート環境ではデフォルト背景を使用
        }

        // ViewModel の初期化とイベント購読
        ViewModel = new MainViewModel();
        ViewModel.RequestFolderPickerAsync += PickFolderAsync;
        ViewModel.RequestImportFilePickerAsync += PickImportFileAsync;
        ViewModel.RequestExportFilePickerAsync += PickExportFileAsync;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // アプリバージョンを含むウィンドウタイトルを設定
        this.Title = $"PhotoDateOrganizer {ViewModel.AppVersionDisplay} - 写真・動画撮影日時自動整理";

        // ウィンドウアイコンの設定
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app_icon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                this.AppWindow.SetIcon(iconPath);
            }
        }
        catch
        {
            // アイコン設定失敗は無視
        }

        // 設定の復元（ウィンドウ位置・サイズ・設定値）
        RestoreSettings();

        // ウィンドウの移動・リサイズ変更をリアルタイムに監視
        this.AppWindow.Changed += OnAppWindowChanged;

        // アプリ終了時に設定を確実に保存
        this.Closed += OnWindowClosed;
        this.AppWindow.Closing += OnAppWindowClosing;
    }

    /// <summary>
    /// 設定ファイルからウィンドウサイズ・位置および各種オプションを復元します。
    /// </summary>
    private void RestoreSettings()
    {
        var settings = _settingsService.LoadSettings();

        // ウィンドウサイズ復元（最小サイズ 800x600 を担保）
        int width = Math.Max(settings.WindowWidth, 800);
        int height = Math.Max(settings.WindowHeight, 600);
        _savedWindowWidth = width;
        _savedWindowHeight = height;
        this.AppWindow.Resize(new SizeInt32(width, height));

        // ウィンドウ位置復元（ディスプレイ作業領域内に収まるようクランプ補正）
        if (settings.WindowX > -10000 && settings.WindowY > -10000 && (settings.WindowX != -1 || settings.WindowY != -1))
        {
            _savedWindowX = settings.WindowX;
            _savedWindowY = settings.WindowY;

            // ディスプレイ領域を取得して画面外にはみ出さないよう補正
            var displayArea = DisplayArea.GetFromPoint(new PointInt32(settings.WindowX, settings.WindowY), DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var workArea = displayArea.WorkArea;
                int clampedX = Math.Max(workArea.X, Math.Min(settings.WindowX, workArea.X + Math.Max(0, workArea.Width - 100)));
                int clampedY = Math.Max(workArea.Y, Math.Min(settings.WindowY, workArea.Y + Math.Max(0, workArea.Height - 100)));
                this.AppWindow.Move(new PointInt32(clampedX, clampedY));
            }
            else
            {
                this.AppWindow.Move(new PointInt32(settings.WindowX, settings.WindowY));
            }
        }

        // フォルダパスおよび設定オプションの復元
        if (!string.IsNullOrEmpty(settings.SourceDirectory))
        {
            ViewModel.SourceDirectory = settings.SourceDirectory;
        }

        if (!string.IsNullOrEmpty(settings.DestinationDirectory))
        {
            ViewModel.DestinationDirectory = settings.DestinationDirectory;
        }

        ViewModel.CloudFileMode = settings.CloudFileMode;
    }

    /// <summary>
    /// ウィンドウの位置やサイズ変更時に通常表示（Restored）状態の値のみを記憶します。
    /// </summary>
    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange || args.DidPresenterChange)
        {
            if (this.AppWindow.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Restored)
            {
                var pos = this.AppWindow.Position;
                var size = this.AppWindow.Size;

                // 最小化時の特殊座標（-32000等）を除外し、有効な値のみを更新
                if (pos.X > -10000 && pos.Y > -10000 && size.Width >= 400 && size.Height >= 300)
                {
                    _savedWindowX = pos.X;
                    _savedWindowY = pos.Y;
                    _savedWindowWidth = size.Width;
                    _savedWindowHeight = size.Height;
                }
            }
        }
    }

    /// <summary>
    /// 現在の有効なウィンドウ位置・サイズおよび設定値を設定ファイルに保存します。
    /// </summary>
    private void SaveCurrentSettings()
    {
        try
        {
            if (this.AppWindow?.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Restored)
            {
                var pos = this.AppWindow.Position;
                var size = this.AppWindow.Size;
                if (pos.X > -10000 && pos.Y > -10000 && size.Width >= 400 && size.Height >= 300)
                {
                    _savedWindowX = pos.X;
                    _savedWindowY = pos.Y;
                    _savedWindowWidth = size.Width;
                    _savedWindowHeight = size.Height;
                }
            }

            var settings = new AppSettings
            {
                WindowX = _savedWindowX,
                WindowY = _savedWindowY,
                WindowWidth = _savedWindowWidth,
                WindowHeight = _savedWindowHeight,
                SourceDirectory = ViewModel.SourceDirectory ?? string.Empty,
                DestinationDirectory = ViewModel.DestinationDirectory ?? string.Empty,
                CloudFileMode = ViewModel.CloudFileMode
            };

            _settingsService.SaveSettings(settings);
        }
        catch
        {
            // 終了時の設定保存エラーは無視
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SourceDirectory) 
            or nameof(ViewModel.DestinationDirectory) 
            or nameof(ViewModel.CloudFileMode)
            or nameof(ViewModel.SkipCloudOnlyFiles))
        {
            SaveCurrentSettings();
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        SaveCurrentSettings();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        SaveCurrentSettings();
    }

    private async void OnBrowseSourceClick(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            ViewModel.SourceDirectory = folder;
        }
    }

    private async void OnBrowseDestinationClick(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            ViewModel.DestinationDirectory = folder;
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        var folderPicker = new FolderPicker();

        // WinUI 3 Desktop では FolderPicker の HWND 初期化が必須
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickImportFileAsync()
    {
        var openPicker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(openPicker, hwnd);

        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".json");

        var file = await openPicker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task<string?> PickExportFileAsync()
    {
        var savePicker = new FileSavePicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(savePicker, hwnd);

        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        savePicker.FileTypeChoices.Add("JSON ファイル (*.json)", new[] { ".json" });
        savePicker.SuggestedFileName = "PhotoDateOrganizer_settings.json";

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
