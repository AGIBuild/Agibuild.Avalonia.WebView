using Avalonia.Controls;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class AdvancedFeaturesE2EView : UserControl
{
    public AdvancedFeaturesE2EView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is AdvancedFeaturesE2EViewModel vm)
        {
            var webView = this.FindControl<WebView>("WebViewControl");
            if (webView is not null)
            {
                vm.WebViewControl = webView;
                vm.OnWebViewLoaded();
            }

            vm.RequestWebViewRecreation += () =>
            {
                RecreateWebView(vm);
            };
        }
    }

    private void RecreateWebView(AdvancedFeaturesE2EViewModel vm)
    {
        var host = this.FindControl<Border>("WebViewHost");
        if (host is null) return;

        host.Child = null;

        var newWebView = new WebView
        {
            Source = new System.Uri("https://github.com")
        };

        host.Child = newWebView;
        vm.WebViewControl = newWebView;
        vm.OnWebViewLoaded();
    }
}
