using System.Diagnostics;
using Agibuild.Avalonia.WebView;
using Avalonia.Controls;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            // ── 1. Navigate to SPA entry point ──────────────────────────
            // Navigate FIRST so the page is loaded, then expose Bridge services
            // (JS stubs must be injected into the loaded page, not about:blank).
            try
            {
#if DEBUG
                // In Debug: load directly from Vite dev server
                // (run `npm run dev` in AvaloniReact.Web first).
                await WebView.NavigateAsync(new Uri("http://localhost:5173"));
#else
                // In Release: use SPA hosting with embedded resources via app:// scheme.
                WebView.EnableSpaHosting(new SpaHostingOptions
                {
                    EmbeddedResourcePrefix = "wwwroot",
                    ResourceAssembly = typeof(MainWindow).Assembly,
                });
                await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
#endif
            }
            catch (WebViewNavigationException ex)
            {
                Debug.WriteLine($"Navigation failed: {ex.Message}");
                await WebView.NavigateToStringAsync(
                    "<html><body style='font-family:system-ui;padding:2em;color:#333'>" +
                    "<h2>Navigation failed</h2>" +
                    $"<p>{ex.Message}</p>" +
#if DEBUG
                    "<p>Make sure the Vite dev server is running:<br>" +
                    "<code>cd AvaloniReact.Web && npm run dev</code></p>" +
#endif
                    "</body></html>");
                return;
            }

            // ── 2. Expose Bridge Services ([JsExport] — C# → JS) ───────
            // Must be called AFTER navigation completes so the RPC JS stubs
            // are injected into the actual page (React app polls for window.agWebView.rpc).
            WebView.Bridge.Expose<IAppShellService>(new AppShellService());
            WebView.Bridge.Expose<ISystemInfoService>(new SystemInfoService());
            WebView.Bridge.Expose<IChatService>(new ChatService());
            WebView.Bridge.Expose<IFileService>(new FileService());
            WebView.Bridge.Expose<ISettingsService>(new SettingsService());
        };
    }
}
