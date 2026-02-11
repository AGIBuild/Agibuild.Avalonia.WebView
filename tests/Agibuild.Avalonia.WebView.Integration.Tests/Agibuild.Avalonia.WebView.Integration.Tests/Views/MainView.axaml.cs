using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private MainViewModel? _vm;

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnViewModelPropertyChanged;

            _vm = DataContext as MainViewModel;
            if (_vm is not null)
            {
                _vm.PropertyChanged += OnViewModelPropertyChanged;
                LoadPage(_vm.SelectedTabIndex);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTabIndex) && _vm is not null)
                LoadPage(_vm.SelectedTabIndex);
        }

        /// <summary>
        /// Lazy page loading: only one view exists in the visual tree at a time.
        /// This prevents iOS stack overflow from deep visual trees.
        /// </summary>
        private void LoadPage(int index)
        {
            if (_vm is null) return;

            var host = this.FindControl<ContentControl>("PageHost");
            if (host is null) return;

            host.Content = index switch
            {
                0 => new ConsumerWebViewE2EView { DataContext = _vm.ConsumerE2E },
                1 => new AdvancedFeaturesE2EView { DataContext = _vm.AdvancedE2E },
                2 => CreatePlatformSmokeView(),
                3 => new FeatureE2EView { DataContext = _vm.FeatureE2E },
                _ => null
            };
        }

        /// <summary>
        /// Auto-detect the current platform and show the appropriate smoke test view.
        /// macOS → WKWebView smoke, Windows → WebView2 smoke.
        /// </summary>
        private UserControl? CreatePlatformSmokeView()
        {
            if (_vm is null) return null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new WkWebViewSmokeView { DataContext = _vm.WkWebViewSmoke };

            // Windows and other platforms use WebView2 smoke
            return new WebView2SmokeView { DataContext = _vm.WebView2Smoke };
        }

        private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Nav rail selection automatically triggers page load via SelectedTabIndex binding.
        }
    }
}
