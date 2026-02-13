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
            // ── SPA Hosting ─────────────────────────────────────────────
#if DEBUG
            WebView.EnableSpaHosting(new SpaHostingOptions
            {
                DevServerUrl = "http://localhost:5173",
            });
#else
            WebView.EnableSpaHosting(new SpaHostingOptions
            {
                EmbeddedResourcePrefix = "wwwroot",
                ResourceAssembly = typeof(MainWindow).Assembly,
            });
#endif

            // ── Bridge Services ([JsExport] — C# exposed to JS) ────────
            WebView.Bridge.Expose<IAppShellService>(new AppShellService());
            WebView.Bridge.Expose<ISystemInfoService>(new SystemInfoService());
            WebView.Bridge.Expose<IChatService>(new ChatService());
            WebView.Bridge.Expose<IFileService>(new FileService());
            WebView.Bridge.Expose<ISettingsService>(new SettingsService());

            // ── Navigate to SPA entry point ─────────────────────────────
            await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
        };
    }
}
