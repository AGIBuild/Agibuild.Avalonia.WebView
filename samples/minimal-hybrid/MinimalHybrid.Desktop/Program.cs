using Agibuild.Fulora;
using Avalonia;

namespace MinimalHybrid.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseAgibuildWebView()
            .LogToTrace();
}
