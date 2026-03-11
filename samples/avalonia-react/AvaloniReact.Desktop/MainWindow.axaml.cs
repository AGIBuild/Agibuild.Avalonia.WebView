using Agibuild.Fulora;
using Avalonia.Controls;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WebView.EnvironmentOptions = new WebViewEnvironmentOptions { EnableDevTools = true };

        Loaded += async (_, _) =>
        {
#if !DEBUG
            WebView.EnableSpaHosting(new SpaHostingOptions
            {
                EmbeddedResourcePrefix = "wwwroot",
                ResourceAssembly = typeof(MainWindow).Assembly,
            });
#endif
            await WebView.BootstrapSpaAsync(new SpaBootstrapOptions
            {
#if DEBUG
                DevServerUrl = "http://localhost:5173",
#endif
                ConfigureBridge = (bridge, _) =>
                {
                    bridge.Expose<IAppShellService>(new AppShellService());
                    bridge.Expose<ISystemInfoService>(new SystemInfoService());
                    bridge.Expose<IChatService>(new ChatService());
                    bridge.Expose<IFileService>(new FileService());
                    bridge.Expose<ISettingsService>(new SettingsService());
                },
            });
        };
    }
}
