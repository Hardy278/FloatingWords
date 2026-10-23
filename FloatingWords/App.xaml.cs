using System.Windows;
using System.Windows.Threading;

namespace FloatingWords;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public AppHost? Host { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            Host = new AppHost();

            var window = new MainWindow(Host!);
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            Host?.Logger.LogError("FloatingWords 启动失败。", exception);

            MessageBox.Show("FloatingWords 启动失败：\n" + exception.Message, "词浮",
                MessageBoxButton.OK, MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        => Host?.Logger.LogError("未处理的 UI 异常。", e.Exception);

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Host?.Logger.LogError("未处理的异常。", exception);
        }
    }
}
