using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.Android;

internal static class AndroidAdapterModule
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!OperatingSystem.IsAndroid())
        {
            return;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal))
        {
            Console.WriteLine("[Agibuild.WebView] Android adapter module initializer running.");
            Console.WriteLine($"[Agibuild.WebView] Registry assembly: {typeof(WebViewAdapterRegistry).Assembly.FullName}");
        }

        RegisterAndroid();
    }

    [SupportedOSPlatform("android")]
    private static void RegisterAndroid()
    {
        WebViewAdapterRegistry.Register(new WebViewAdapterRegistration(
            Platform: WebViewAdapterPlatform.Android,
            AdapterId: "android-webview",
            Factory: static () => new AndroidWebViewAdapter(),
            Priority: 100));
    }
}
