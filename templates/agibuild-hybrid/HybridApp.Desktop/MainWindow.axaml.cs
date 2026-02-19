using Agibuild.Avalonia.WebView;
using Avalonia.Controls;
using HybridApp.Bridge;

namespace HybridApp.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            // Enable SPA hosting — serves wwwroot/ as app://localhost/
            WebView.EnableSpaHosting(new SpaHostingOptions
            {
                EmbeddedResourcePrefix = "wwwroot",
                ResourceAssembly = typeof(MainWindow).Assembly,
            });

            InitializeShellPreset();

            // Expose the C# greeter service to JavaScript
            WebView.Bridge.Expose<IGreeterService>(new GreeterServiceImpl());

            // Navigate to the embedded SPA entry point
            await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
        };

        Unloaded += (_, _) => DisposeShellPreset();
    }

    partial void InitializeShellPreset();
    partial void DisposeShellPreset();
}
