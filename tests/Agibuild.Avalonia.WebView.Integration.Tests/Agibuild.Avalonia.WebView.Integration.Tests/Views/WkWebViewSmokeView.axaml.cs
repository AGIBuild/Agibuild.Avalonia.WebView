using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Agibuild.Avalonia.WebView.Integration.Tests.Controls;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class WkWebViewSmokeView : UserControl
{
    public WkWebViewSmokeView()
    {
        InitializeComponent();

        var host = this.FindControl<AdapterNativeControlHost>("NativeHost");
        host!.HandleCreated += OnHandleCreated;
        host!.HandleDestroyed += OnHandleDestroyed;

        // Sync native host visibility when tools drawer toggles
        // (native controls render above Avalonia UI, must hide when overlay is open)
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        if (toggle is not null)
            toggle.IsCheckedChanged += (_, _) => SyncNativeHostVisibility();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnHandleCreated(global::Avalonia.Platform.IPlatformHandle handle)
    {
        if (DataContext is WkWebViewSmokeViewModel vm)
        {
            vm.SetHostHandle(handle);
        }
    }

    private void OnHandleDestroyed()
    {
        if (DataContext is WkWebViewSmokeViewModel vm)
        {
            vm.Detach();
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

    private void SyncNativeHostVisibility()
    {
        var toggle = this.FindControl<ToggleButton>("ToolsToggle");
        var container = this.FindControl<Border>("NativeHostContainer");
        if (toggle is not null && container is not null)
            container.IsVisible = toggle.IsChecked != true;
    }
}
