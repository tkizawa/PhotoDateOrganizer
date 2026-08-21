using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    public MainViewModel ViewModel { get; }
    private readonly SettingsService _settingsService = new();

    // ウィンドウの通常状態（Restored）における最新の位置とサイズをキャッシュ
    private int _savedWindowX = -1;
    private int _savedWindowY = -1;
    private int _savedWindowWidth = 1100;
    private int _savedWindowHeight = 750;
    private bool _isDisclaimerAccepted = false;
    private bool _isInitializing = true;
    private bool _hasAppliedInitialPosition = false;
    private AppSettings? _initialSettings;

    public MainWindow()
    {
        _isInitializing = true;
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

        // ViewModel の初期化
        ViewModel = new MainViewModel();
        ViewModel.RequestFolderPickerAsync += PickFolderAsync;
        ViewModel.RequestImportFilePickerAsync += PickImportFileAsync;
        ViewModel.RequestExportFilePickerAsync += PickExportFileAsync;

        // アプリバージョンを含むウィンドウタイトルを設定
        UpdateTitle();
        LocalizationService.Current.PropertyChanged += (_, _) => UpdateTitle();

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

        // ウィンドウアクティブ化時の位置復元フック（WinUI 3の初期Activateによるプライマリモニタへのリセットを防止）
        this.Activated += OnWindowActivated;

        // 設定復元完了後に ViewModel のプロパティ変更を監視開始
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 初回起動時の免責事項同意確認
        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.Loaded += async (s, e) =>
            {
                await CheckAndShowDisclaimerAsync();
            };
        }

        // ウィンドウの移動・リサイズ変更をリアルタイムに監視
        this.AppWindow.Changed += OnAppWindowChanged;

        // アプリ終了時に設定を確実に保存
        this.Closed += OnWindowClosed;
        this.AppWindow.Closing += OnAppWindowClosing;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (!_hasAppliedInitialPosition && _initialSettings != null)
        {
            _hasAppliedInitialPosition = true;
            RestoreWindowPositionAndSize(_initialSettings);

            // 表示・アクティブ化に伴う初期レイアウトイベントが完了した後に初期化フラグを解除
            this.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                _isInitializing = false;
            });
        }
    }

    /// <summary>
    /// ウィンドウのアクティブ化完了後に外部（App.xaml.cs）から呼び出され、確実にマルチモニタ位置へ復元します。
    /// </summary>
    public void RestoreWindowPlacement()
    {
        var settings = _settingsService.LoadSettings();
        RestoreWindowPositionAndSize(settings);

        this.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            _isInitializing = false;
        });
    }

    private void UpdateTitle()
    {
        this.Title = $"PhotoDateOrganizer {ViewModel.AppVersionDisplay} - {(LocalizationService.Current.IsJapanese ? "写真・動画撮影日時自動整理" : "Auto Organize Photos & Videos by Date")}";
    }

    /// <summary>
    /// 設定ファイルからウィンドウサイズ・位置および各種オプションを復元します。
    /// </summary>
    private void RestoreSettings()
    {
        var settings = _settingsService.LoadSettings();
        _initialSettings = settings;

        // 言語設定の復元
        if (!string.IsNullOrEmpty(settings.Language) && Enum.TryParse<AppLanguage>(settings.Language, true, out var lang))
        {
            LocalizationService.Current.Language = lang;
        }

        // 免責事項の同意状態を復元
        _isDisclaimerAccepted = settings.IsDisclaimerAccepted;

        // マルチモニタ対応のウィンドウ位置・サイズ復元
        RestoreWindowPositionAndSize(settings);

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
    /// マルチモニタ環境を考慮して保存されたウィンドウ位置とサイズを復元します。
    /// </summary>
    private void RestoreWindowPositionAndSize(AppSettings settings)
    {
        int width = Math.Max(settings.WindowWidth, 800);
        int height = Math.Max(settings.WindowHeight, 600);
        _savedWindowWidth = width;
        _savedWindowHeight = height;

        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero) return;

        if (settings.WindowX <= -10000 || settings.WindowY <= -10000 || (settings.WindowX == -1 && settings.WindowY == -1))
        {
            this.AppWindow.Resize(new SizeInt32(width, height));
            return;
        }

        int savedX = settings.WindowX;
        int savedY = settings.WindowY;

        // Win32 MonitorFromRect を用いてターゲットモニタを判定（マルチモニタ対応）
        var targetRect = new RECT
        {
            Left = savedX,
            Top = savedY,
            Right = savedX + width,
            Bottom = savedY + height
        };

        var hMonitor = MonitorFromRect(ref targetRect, MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero)
        {
            var mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf<MONITORINFO>();

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                int workWidth = mi.rcWork.Right - mi.rcWork.Left;
                int workHeight = mi.rcWork.Bottom - mi.rcWork.Top;

                int targetWidth = Math.Min(width, workWidth);
                int targetHeight = Math.Min(height, workHeight);

                // 操作バーが消えないよう作業領域内に確実にクランプ
                int clampedX = Math.Max(mi.rcWork.Left, Math.Min(savedX, mi.rcWork.Right - 100));
                int clampedY = Math.Max(mi.rcWork.Top, Math.Min(savedY, mi.rcWork.Bottom - 100));

                _savedWindowX = clampedX;
                _savedWindowY = clampedY;
                _savedWindowWidth = targetWidth;
                _savedWindowHeight = targetHeight;

                SetWindowPos(hwnd, IntPtr.Zero, clampedX, clampedY, targetWidth, targetHeight, SWP_NOZORDER | SWP_SHOWWINDOW);
                this.AppWindow.MoveAndResize(new RectInt32(clampedX, clampedY, targetWidth, targetHeight));

                if (settings.IsMaximized && this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
                return;
            }
        }

        // フォールバック: 直接指定
        SetWindowPos(hwnd, IntPtr.Zero, savedX, savedY, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
        this.AppWindow.MoveAndResize(new RectInt32(savedX, savedY, width, height));

        if (settings.IsMaximized && this.AppWindow.Presenter is OverlappedPresenter fallbackPresenter)
        {
            fallbackPresenter.Maximize();
        }
    }

    /// <summary>
    /// 初回起動時に免責事項ダイアログを表示し、ユーザーの同意を確認します。
    /// 同意されない場合はアプリケーションを安全に終了します。
    /// </summary>
    private async Task CheckAndShowDisclaimerAsync()
    {
        if (_isDisclaimerAccepted)
        {
            return;
        }

        var strings = LocalizationService.Current.Strings;

        var dialog = new ContentDialog
        {
            Title = strings.DisclaimerDialogTitle,
            PrimaryButtonText = strings.DisclaimerAgreeButton,
            CloseButtonText = strings.DisclaimerDisagreeButton,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var mainStack = new StackPanel
        {
            Spacing = 14,
            MaxWidth = 620
        };

        // 注意喚起バナー (CAUTION) - 横幅を確実にフィットさせ折り返しを有効化するため Grid を使用
        var cautionBorder = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(24, 239, 68, 68)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 239, 68, 68)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var cautionGrid = new Grid
        {
            ColumnSpacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        cautionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cautionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = "\uE7BA",
            FontSize = 18,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetColumn(icon, 0);
        cautionGrid.Children.Add(icon);

        var cautionText = new TextBlock
        {
            Text = strings.DisclaimerCautionBanner,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(cautionText, 1);
        cautionGrid.Children.Add(cautionText);

        cautionBorder.Child = cautionGrid;
        mainStack.Children.Add(cautionBorder);

        // 条項リスト
        var itemsStack = new StackPanel
        {
            Spacing = 12
        };

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem1Title,
            strings.DisclaimerItem1Desc));

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem2Title,
            strings.DisclaimerItem2Desc));

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem3Title,
            strings.DisclaimerItem3Desc));

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem4Title,
            strings.DisclaimerItem4Desc));

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem5Title,
            strings.DisclaimerItem5Desc));

        itemsStack.Children.Add(CreateDisclaimerItem(
            strings.DisclaimerItem6Title,
            strings.DisclaimerItem6Desc));

        var scrollViewer = new ScrollViewer
        {
            Content = itemsStack,
            MaxHeight = 360,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        mainStack.Children.Add(scrollViewer);
        dialog.Content = mainStack;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _isDisclaimerAccepted = true;
            SaveCurrentSettings();
        }
        else
        {
            // ユーザーが同意しなかった場合はアプリを終了
            this.Close();
        }
    }

    /// <summary>
    /// 免責事項の各条項UI要素を生成します。
    /// </summary>
    private static UIElement CreateDisclaimerItem(string title, string description)
    {
        var panel = new StackPanel { Spacing = 4 };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 13
        });

        panel.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Foreground = Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var brush) && brush is Brush b
                ? b
                : new SolidColorBrush(Windows.UI.Color.FromArgb(200, 150, 150, 150)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(6, 0, 0, 0)
        });

        return panel;
    }

    /// <summary>
    /// ウィンドウの位置やサイズ変更時に通常表示（Restored）状態の値のみを記憶します。
    /// </summary>
    private void UpdateSavedWindowBounds()
    {
        if (_isInitializing) return;

        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
        {
            if (this.AppWindow?.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Restored)
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // 最小化時の特殊座標（-32000等）を除外し、有効な実座標のみを更新
                if (rect.Left > -10000 && rect.Top > -10000 && width >= 400 && height >= 300)
                {
                    _savedWindowX = rect.Left;
                    _savedWindowY = rect.Top;
                    _savedWindowWidth = width;
                    _savedWindowHeight = height;
                }
            }
        }
    }

    /// <summary>
    /// ウィンドウの位置やサイズ変更時に通常表示（Restored）状態の値のみを記憶します。
    /// </summary>
    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isInitializing) return;

        if (args.DidPositionChange || args.DidSizeChange || args.DidPresenterChange)
        {
            try
            {
                UpdateSavedWindowBounds();
            }
            catch
            {
                // ウィンドウ破棄中などの例外は無視
            }
        }
    }

    /// <summary>
    /// 現在の有効なウィンドウ位置・サイズおよび設定値を設定ファイルに保存します。
    /// </summary>
    private void SaveCurrentSettings()
    {
        if (_isInitializing) return;

        bool isMaximized = false;
        try
        {
            if (this.AppWindow?.Presenter is OverlappedPresenter presenter)
            {
                isMaximized = presenter.State == OverlappedPresenterState.Maximized;
            }
            UpdateSavedWindowBounds();
        }
        catch
        {
            // 終了時などのCOMアクセス例外は無視し、キャッシュ値を使用
        }

        try
        {
            var settings = new AppSettings
            {
                WindowX = _savedWindowX,
                WindowY = _savedWindowY,
                WindowWidth = _savedWindowWidth,
                WindowHeight = _savedWindowHeight,
                IsMaximized = isMaximized,
                SourceDirectory = ViewModel?.SourceDirectory ?? string.Empty,
                DestinationDirectory = ViewModel?.DestinationDirectory ?? string.Empty,
                CloudFileMode = ViewModel?.CloudFileMode ?? CloudFileHandlingMode.Download,
                Language = LocalizationService.Current.Language.ToString().ToLowerInvariant(),
                IsDisclaimerAccepted = _isDisclaimerAccepted
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
        savePicker.FileTypeChoices.Add(LocalizationService.Current.Strings.JsonFilePickerFilterName, new[] { ".json" });
        savePicker.SuggestedFileName = "PhotoDateOrganizer_settings.json";

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
