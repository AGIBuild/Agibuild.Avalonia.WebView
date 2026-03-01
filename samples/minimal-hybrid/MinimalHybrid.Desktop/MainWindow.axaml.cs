using System.Diagnostics;
using Agibuild.Fulora;
using Avalonia.Controls;
using MinimalHybrid.Bridge;

namespace MinimalHybrid.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            try
            {
                WebView.EnableSpaHosting(new SpaHostingOptions
                {
                    EmbeddedResourcePrefix = "wwwroot",
                    ResourceAssembly = typeof(MainWindow).Assembly,
                });
                await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
            }
            catch (WebViewNavigationException ex)
            {
                Debug.WriteLine($"Navigation failed: {ex.Message}");
                await WebView.NavigateToStringAsync(
                    "<html><body style='font-family:system-ui;padding:2em;color:#333'>" +
                    $"<h2>Navigation failed</h2><p>{ex.Message}</p>" +
                    "</body></html>");
                return;
            }

            WebView.Bridge.Expose<IAppService>(new AppService());
        };
    }
}
