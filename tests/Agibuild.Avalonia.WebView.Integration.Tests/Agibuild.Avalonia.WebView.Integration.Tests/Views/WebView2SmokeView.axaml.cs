using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Agibuild.Avalonia.WebView.Integration.Tests.Controls;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class WebView2SmokeView : UserControl
{
    public WebView2SmokeView()
    {
        InitializeComponent();

        var host = this.FindControl<AdapterNativeControlHost>("NativeHost");
        host!.HandleCreated += OnHandleCreated;
        host!.HandleDestroyed += OnHandleDestroyed;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnHandleCreated(global::Avalonia.Platform.IPlatformHandle handle)
    {
        if (DataContext is WebView2SmokeViewModel vm)
        {
            vm.SetHostHandle(handle);
        }
    }

    private void OnHandleDestroyed()
    {
        if (DataContext is WebView2SmokeViewModel vm)
        {
            vm.Detach();
        }
    }
}
