using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.MacOS;

internal static class MacOSAdapterModule
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal))
        {
            Console.WriteLine("[Agibuild.WebView] macOS adapter module initializer running.");
            Console.WriteLine($"[Agibuild.WebView] Registry assembly: {typeof(WebViewAdapterRegistry).Assembly.FullName}");
        }

        RegisterMacOS();
    }

    [SupportedOSPlatform("macos")]
    private static void RegisterMacOS()
    {
        WebViewAdapterRegistry.Register(new WebViewAdapterRegistration(
            Platform: WebViewAdapterPlatform.MacOS,
            AdapterId: "wkwebview",
            Factory: static () => new MacOSWebViewAdapter(),
            Priority: 100));
    }
}

