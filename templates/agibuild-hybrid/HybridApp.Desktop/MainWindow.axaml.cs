using Agibuild.Fulora;
using Avalonia.Controls;
using Avalonia.Input;
using HybridApp.Bridge;

namespace HybridApp.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            WebView.EnableSpaHosting(new SpaHostingOptions
            {
                EmbeddedResourcePrefix = "wwwroot",
                ResourceAssembly = typeof(MainWindow).Assembly,
            });

            InitializeShellPreset();

#if DEBUG
            DevToolsOverlay.Attach(WebView);
            DevToolsOverlay.RegisterToggleShortcut(this,
                new KeyGesture(Key.D, KeyModifiers.Control | KeyModifiers.Shift));
#endif

            WebView.Bridge.Expose<IGreeterService>(new GreeterServiceImpl());
            RegisterShellPresetBridgeServices();

            await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
        };

        Unloaded += (_, _) =>
        {
            DevToolsOverlay.Dispose();
            DisposeShellPreset();
        };
    }

    partial void InitializeShellPreset();
    partial void DisposeShellPreset();
    partial void RegisterShellPresetBridgeServices();
}
