using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            // Hide native WebView when nav drawer opens (native controls render above Avalonia UI)
            var navToggle = this.FindControl<ToggleButton>("NavToggle");
            navToggle?.IsCheckedChanged += (_, _) => SyncPageHostVisibility();
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
                0 => new WebViewTestView { DataContext = _vm.WebViewTest },
                1 => new WkWebViewSmokeView { DataContext = _vm.WkWebViewSmoke },
                2 => new WebView2SmokeView { DataContext = _vm.WebView2Smoke },
                3 => new ConsumerWebViewE2EView { DataContext = _vm.ConsumerE2E },
                4 => new AdvancedFeaturesE2EView { DataContext = _vm.AdvancedE2E },
                5 => new NavigationInterceptView { DataContext = _vm.NavigationIntercept },
                6 => new FeatureE2EView { DataContext = _vm.FeatureE2E },
                _ => null
            };
        }

        /// <summary>
        /// Hide PageHost when nav drawer is open so native controls don't obscure the drawer.
        /// </summary>
        private void SyncPageHostVisibility()
        {
            var navToggle = this.FindControl<ToggleButton>("NavToggle");
            var host = this.FindControl<ContentControl>("PageHost");
            if (navToggle is not null && host is not null)
                host.IsVisible = navToggle.IsChecked != true;
        }

        private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var toggle = this.FindControl<ToggleButton>("NavToggle");
            if (toggle is not null)
                toggle.IsChecked = false;
        }

        private void OnOverlayDismiss(object? sender, PointerPressedEventArgs e)
        {
            var toggle = this.FindControl<ToggleButton>("NavToggle");
            if (toggle is not null)
                toggle.IsChecked = false;
        }
    }
}
