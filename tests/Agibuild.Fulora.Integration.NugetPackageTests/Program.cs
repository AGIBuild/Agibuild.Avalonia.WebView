using System;
using Agibuild.Fulora;
using Avalonia;

namespace Agibuild.Fulora.Integration.NugetPackageTests;

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
            .UseFulora(); // Initialize WebView environment via DI
}
