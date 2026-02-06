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

            // Subscribe to Apply & Reload: recreate WebView when environment options change.
            vm.RequestWebViewRecreation += () =>
            {
                RecreateWebView(vm);
            };
        }
    }

    /// <summary>
    /// Removes the existing WebView and creates a fresh one so that newly set
    /// <see cref="WebViewEnvironment.Options"/> (e.g. DevTools, EphemeralSession)
    /// are picked up during <c>NativeControlHost.CreateNativeControlCore</c>.
    /// </summary>
    private void RecreateWebView(AdvancedFeaturesE2EViewModel vm)
    {
        var host = this.FindControl<Border>("WebViewHost");
        if (host is null) return;

        // Remove old WebView (triggers DestroyNativeControlCore → dispose adapter).
        host.Child = null;

        // Create a fresh WebView that will pick up the new global environment options.
        var newWebView = new WebView
        {
            Source = new System.Uri("https://www.bing.com")
        };

        host.Child = newWebView;
        vm.WebViewControl = newWebView;
        vm.OnWebViewLoaded();
    }
}
