using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            ConsumerE2E = new ConsumerWebViewE2EViewModel(AppendLog);
            AdvancedE2E = new AdvancedFeaturesE2EViewModel(AppendLog);
            WkWebViewSmoke = new WkWebViewSmokeViewModel(AppendLog);
            WebView2Smoke = new WebView2SmokeViewModel(AppendLog);
            FeatureE2E = new FeatureE2EViewModel(AppendLog);
        }

        [ObservableProperty]
        private int _selectedTabIndex;

        // --- Shared log panel ---

        [ObservableProperty]
        private string _sharedLog = string.Empty;

        [ObservableProperty]
        private bool _isLogPanelOpen = true;

        public void AppendLog(string line)
        {
            SharedLog = $"{SharedLog}{line}{Environment.NewLine}";
        }

        [RelayCommand]
        private void ClearLog() => SharedLog = string.Empty;

        [RelayCommand]
        private void ToggleLogPanel() => IsLogPanelOpen = !IsLogPanelOpen;

        // --- Page ViewModels (nav order) ---

        /// <summary>Tab 0: Browser — full navigation, JS, HTML, cookies.</summary>
        public ConsumerWebViewE2EViewModel ConsumerE2E { get; }

        /// <summary>Tab 1: Advanced — Dialog, Auth, DevTools, Environment.</summary>
        public AdvancedFeaturesE2EViewModel AdvancedE2E { get; }

        /// <summary>Tab 2 (macOS): WKWebView smoke tests.</summary>
        public WkWebViewSmokeViewModel WkWebViewSmoke { get; }

        /// <summary>Tab 2 (Windows): WebView2 smoke tests.</summary>
        public WebView2SmokeViewModel WebView2Smoke { get; }

        /// <summary>Tab 3: Feature E2E — automated 8-feature test dashboard.</summary>
        public FeatureE2EViewModel FeatureE2E { get; }
    }
}
