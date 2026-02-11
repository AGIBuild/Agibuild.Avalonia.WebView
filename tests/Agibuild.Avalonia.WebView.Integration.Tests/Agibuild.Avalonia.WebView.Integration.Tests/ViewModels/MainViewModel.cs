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
            WebView2Smoke = new WebView2SmokeViewModel();
            ConsumerE2E = new ConsumerWebViewE2EViewModel();
            AdvancedE2E = new AdvancedFeaturesE2EViewModel();
            NavigationIntercept = new NavigationInterceptViewModel();
            FeatureE2E = new FeatureE2EViewModel();
        }

        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";

        [ObservableProperty]
        private int _selectedTabIndex;

        public WebViewTestViewModel WebViewTest { get; }

        public WkWebViewSmokeViewModel WkWebViewSmoke { get; }

        public WebView2SmokeViewModel WebView2Smoke { get; }

        public ConsumerWebViewE2EViewModel ConsumerE2E { get; }

        public AdvancedFeaturesE2EViewModel AdvancedE2E { get; }

        public NavigationInterceptViewModel NavigationIntercept { get; }

        public FeatureE2EViewModel FeatureE2E { get; }
    }
}
