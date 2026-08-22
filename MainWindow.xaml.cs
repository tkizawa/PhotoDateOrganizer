using System;
using System.ComponentModel;
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
    public MainViewModel ViewModel { get; }
    private readonly SettingsService _settingsService = new();

    // ウィンドウの通常状態（Restored）における最新の位置とサイズをキャッシュ
    private int _savedWindowX = -1;
    private int _savedWindowY = -1;
    private int _savedWindowWidth = 1100;
    private int _savedWindowHeight = 750;
    private bool _isDisclaimerAccepted = false;

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
        this.Title = string.Format(LocalizationService.Strings.WindowTitleFormat, ViewModel.AppVersionDisplay);

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

    /// <summary>
    /// 設定ファイルからウィンドウサイズ・位置および各種オプションを復元します。
    /// </summary>
    private void RestoreSettings()
    {
        var settings = _settingsService.LoadSettings();

        // 免責事項の同意状態を復元
        _isDisclaimerAccepted = settings.IsDisclaimerAccepted;

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
    /// 初回起動時に免責事項ダイアログを表示し、ユーザーの同意を確認します。
    /// 同意されない場合はアプリケーションを安全に終了します。
    /// </summary>
    private async Task CheckAndShowDisclaimerAsync()
    {
        if (_isDisclaimerAccepted)
        {
            return;
        }

        var s = LocalizationService.Strings;

        var dialog = new ContentDialog
        {
            Title = s.DisclaimerDialogTitle,
            PrimaryButtonText = s.DisclaimerAcceptButton,
            CloseButtonText = s.DisclaimerDeclineButton,
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
            Text = s.DisclaimerCautionBanner,
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

        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem1Title, s.DisclaimerItem1Body));
        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem2Title, s.DisclaimerItem2Body));
        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem3Title, s.DisclaimerItem3Body));
        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem4Title, s.DisclaimerItem4Body));
        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem5Title, s.DisclaimerItem5Body));
        itemsStack.Children.Add(CreateDisclaimerItem(s.DisclaimerItem6Title, s.DisclaimerItem6Body));

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
                CloudFileMode = ViewModel.CloudFileMode,
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
        savePicker.FileTypeChoices.Add(LocalizationService.Strings.JsonFileFilterName, new[] { ".json" });
        savePicker.SuggestedFileName = "PhotoDateOrganizer_settings.json";

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
