using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class WebViewTestView : UserControl
{
    public WebViewTestView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
