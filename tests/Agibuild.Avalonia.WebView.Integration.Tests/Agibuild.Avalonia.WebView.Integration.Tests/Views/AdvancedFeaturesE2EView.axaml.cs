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
        }
    }
}
