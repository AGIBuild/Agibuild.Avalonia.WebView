using Agibuild.Avalonia.WebView.Testing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            WebViewTest = new WebViewTestViewModel(new MockWebViewAdapter());
            WkWebViewSmoke = new WkWebViewSmokeViewModel();
            ConsumerE2E = new ConsumerWebViewE2EViewModel();
        }

        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";

        [ObservableProperty]
        private int _selectedTabIndex;

        public WebViewTestViewModel WebViewTest { get; }

        public WkWebViewSmokeViewModel WkWebViewSmoke { get; }

        public ConsumerWebViewE2EViewModel ConsumerE2E { get; }
    }
}
