using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.iOS;

internal static class iOSAdapterModule
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!OperatingSystem.IsIOS())
        {
            return;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal))
        {
            Console.WriteLine("[Agibuild.WebView] iOS adapter module initializer running.");
            Console.WriteLine($"[Agibuild.WebView] Registry assembly: {typeof(WebViewAdapterRegistry).Assembly.FullName}");
        }

        RegisteriOS();
    }

    [SupportedOSPlatform("ios")]
    private static void RegisteriOS()
    {
        WebViewAdapterRegistry.Register(new WebViewAdapterRegistration(
            Platform: WebViewAdapterPlatform.iOS,
            AdapterId: "wkwebview-ios",
            Factory: static () => new iOSWebViewAdapter(),
            Priority: 100));
    }
}
