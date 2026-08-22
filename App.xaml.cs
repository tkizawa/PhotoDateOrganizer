using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PhotoDateOrganizer;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public App()
    {
        this.InitializeComponent();

        // 捕捉されなかった例外のイベントハンドラを登録
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
        _mainWindow.RestoreWindowPlacement();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Application.UnhandledException");
        // e.Handled = true; を設定するとクラッシュを回避できる場合がありますが、
        // 状態が不安定になるため、ログだけ記録してクラッシュさせるのが一般的です。
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex, "AppDomain.UnhandledException");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private void LogException(Exception ex, string source)
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, "PhotoDateOrganizer");
            Directory.CreateDirectory(appFolder);
            
            string logPath = Path.Combine(appFolder, "crash.log");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] [{source}]\r\n{ex.GetType().Name}: {ex.Message}\r\n{ex.StackTrace}\r\n\r\n";
            
            File.AppendAllText(logPath, logMessage);
        }
        catch
        {
            // ログ記録中のエラーは無視する
        }
    }
}
