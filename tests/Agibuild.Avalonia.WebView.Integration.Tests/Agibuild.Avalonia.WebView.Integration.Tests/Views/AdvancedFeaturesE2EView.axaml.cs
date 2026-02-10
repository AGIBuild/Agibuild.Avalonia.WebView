using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class AdvancedFeaturesE2EView : UserControl
{
    public AdvancedFeaturesE2EView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Hide native WebView when tools drawer opens (native controls render above Avalonia UI)
        var toolsToggle = this.FindControl<ToggleButton>("ToolsToggle");
        toolsToggle?.IsCheckedChanged += (_, _) => SyncWebViewVisibility();
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

    private void OnCloseToolsPanel(object? sender, RoutedEventArgs e)
    {
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        if (toggle is not null)
            toggle.IsChecked = false;
    }

    private void OnOverlayDismiss(object? sender, PointerPressedEventArgs e)
    {
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        if (toggle is not null)
            toggle.IsChecked = false;
    }

    /// <summary>
    /// Hide the WebView native control when the tools overlay is open,
    /// because NativeControlHost always renders above Avalonia's managed UI.
    /// </summary>
    private void SyncWebViewVisibility()
    {
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        var host = this.FindControl<Border>("WebViewHost");
        if (toggle is not null && host is not null)
            host.IsVisible = toggle.IsChecked != true;
    }
}
