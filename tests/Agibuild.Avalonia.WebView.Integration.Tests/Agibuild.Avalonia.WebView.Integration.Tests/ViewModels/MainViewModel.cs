using Agibuild.Avalonia.WebView.Testing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            WebViewTest = new WebViewTestViewModel(new MockWebViewAdapter());
        }

        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";

        public WebViewTestViewModel WebViewTest { get; }
    }
}
