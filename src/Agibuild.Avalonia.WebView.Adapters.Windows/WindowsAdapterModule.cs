using System.Runtime.CompilerServices;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.Windows;

internal static class WindowsAdapterModule
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal))
        {
            Console.WriteLine("[Agibuild.WebView] Windows adapter module initializer running.");
            Console.WriteLine($"[Agibuild.WebView] Registry assembly: {typeof(WebViewAdapterRegistry).Assembly.FullName}");
        }

        WebViewAdapterRegistry.Register(new WebViewAdapterRegistration(
            Platform: WebViewAdapterPlatform.Windows,
            AdapterId: "webview2",
            Factory: static () => new WindowsWebViewAdapter(),
            Priority: 100));
    }
}
