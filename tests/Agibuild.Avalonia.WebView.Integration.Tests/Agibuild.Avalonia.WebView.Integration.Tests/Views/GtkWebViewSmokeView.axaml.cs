using Avalonia.Controls;
using Avalonia.Interactivity;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class GtkWebViewSmokeView : UserControl
{
    public GtkWebViewSmokeView()
    {
        InitializeComponent();

        var webView = this.FindControl<WebView>("WebViewControl");
        if (webView is not null)
        {
            // Must be set before the control is attached to the visual tree.
            webView.EnvironmentOptions = new WebViewEnvironmentOptions
            {
                EnableDevTools = true
            };
        }

        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnViewLoaded;

        if (DataContext is GtkWebViewSmokeViewModel vm)
        {
            var webView = this.FindControl<WebView>("WebViewControl");
            if (webView is not null)
            {
                vm.WebViewControl = webView;
                vm.OnWebViewLoaded();
            }
        }
    }
}

