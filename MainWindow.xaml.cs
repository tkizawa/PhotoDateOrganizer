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

    public MainWindow()
    {
        this.InitializeComponent();

        // Enable Mica backdrop if supported
        try
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // Fallback gracefully to default background
        }

        // Initialize ViewModel
        ViewModel = new MainViewModel();
        ViewModel.RequestFolderPickerAsync += PickFolderAsync;
        ViewModel.RequestImportFilePickerAsync += PickImportFileAsync;
        ViewModel.RequestExportFilePickerAsync += PickExportFileAsync;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Set window title with version
        this.Title = $"PhotoDateOrganizer {ViewModel.AppVersionDisplay} - 写真・動画撮影日時自動整理";

        // Set window icon
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
            // Ignore icon setting errors
        }

        // Restore settings (Window position, size, and folders)
        RestoreSettings();

        // Save settings on window closing
        this.Closed += OnWindowClosed;
        this.AppWindow.Closing += OnAppWindowClosing;
    }

    private void RestoreSettings()
    {
        var settings = _settingsService.LoadSettings();

        // Restore Window Size & Position
        int width = Math.Max(settings.WindowWidth, 800);
        int height = Math.Max(settings.WindowHeight, 600);
        this.AppWindow.Resize(new SizeInt32(width, height));

        if (settings.WindowX >= 0 && settings.WindowY >= 0)
        {
            this.AppWindow.Move(new PointInt32(settings.WindowX, settings.WindowY));
        }

        // Restore Folders & Options
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

    private void SaveCurrentSettings()
    {
        try
        {
            var pos = this.AppWindow.Position;
            var size = this.AppWindow.Size;

            var settings = new AppSettings
            {
                WindowX = pos.X,
                WindowY = pos.Y,
                WindowWidth = size.Width,
                WindowHeight = size.Height,
                SourceDirectory = ViewModel.SourceDirectory ?? string.Empty,
                DestinationDirectory = ViewModel.DestinationDirectory ?? string.Empty,
                CloudFileMode = ViewModel.CloudFileMode
            };

            _settingsService.SaveSettings(settings);
        }
        catch
        {
            // Ignore settings save errors on exit
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

        // WinUI 3 Desktop requires HWND initialization for FolderPicker
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
