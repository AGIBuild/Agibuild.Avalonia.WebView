using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class FeatureE2EView : UserControl
{
    public FeatureE2EView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Hide native WebView when tools drawer opens
        var toolsToggle = this.FindControl<ToggleButton>("ToolsToggle");
        toolsToggle?.IsCheckedChanged += (_, _) => SyncWebViewVisibility();
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is FeatureE2EViewModel vm)
        {
            var webView = this.FindControl<WebView>("WebViewControl");
            if (webView is not null)
            {
                vm.WebViewControl = webView;
                vm.OnWebViewLoaded();
            }
        }
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

    private void SyncWebViewVisibility()
    {
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        var host = this.FindControl<Border>("WebViewHost");
        if (toggle is not null && host is not null)
            host.IsVisible = toggle.IsChecked != true;
    }
}
