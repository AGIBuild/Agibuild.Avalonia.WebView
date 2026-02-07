using System.Runtime.CompilerServices;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.Gtk;

internal static class GtkAdapterModule
{
    [ModuleInitializer]
    internal static void Register()
    {
        // GTK adapter is the fallback for non-Windows/macOS/Android/iOS platforms.
        // No platform check needed — the registry's GetCurrentPlatform() returns Gtk
        // for all unrecognized platforms (typically Linux).

        if (string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal))
        {
            Console.WriteLine("[Agibuild.WebView] GTK adapter module initializer running.");
            Console.WriteLine($"[Agibuild.WebView] Registry assembly: {typeof(WebViewAdapterRegistry).Assembly.FullName}");
        }

        WebViewAdapterRegistry.Register(new WebViewAdapterRegistration(
            Platform: WebViewAdapterPlatform.Gtk,
            AdapterId: "webkitgtk",
            Factory: static () => new GtkWebViewAdapter(),
            Priority: 100));
    }
}
