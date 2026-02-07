using Avalonia;
using Agibuild.Avalonia.WebView;
using System;

namespace Agibuild.Avalonia.WebView.Integration.NugetPackageTests;

internal class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseAgibuildWebView(); // Initialize WebView environment via DI
}
