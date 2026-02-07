using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class ConsumerWebViewE2EView : UserControl
{
    public ConsumerWebViewE2EView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Hide native WebView when tools drawer opens (native controls render above Avalonia UI)
        var toolsToggle = this.FindControl<ToggleButton>("ToolsToggle");
        toolsToggle?.IsCheckedChanged += (_, _) => SyncWebViewVisibility();
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ConsumerWebViewE2EViewModel vm)
        {
            var webView = this.FindControl<WebView>("WebViewControl");
            if (webView is not null)
            {
                vm.WebViewControl = webView;
                vm.OnWebViewLoaded();
            }
        }
    }

    private void OnAddressKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ConsumerWebViewE2EViewModel vm)
            vm.GoToAddressCommand.Execute(null);
    }

    private void OnScriptKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter
            && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            && DataContext is ConsumerWebViewE2EViewModel vm)
            vm.RunScriptCommand.Execute(null);
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
        var container = this.FindControl<Border>("WebViewContainer");
        if (toggle is not null && container is not null)
            container.IsVisible = toggle.IsChecked != true;
    }
}
