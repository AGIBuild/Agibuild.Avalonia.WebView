using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views;

public partial class NavigationInterceptView : UserControl
{
    private NavigationInterceptViewModel? _vm;
    private WebView? _webView;
    private bool _testPageLoaded;

    public NavigationInterceptView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        // Sync WebView visibility when log drawer toggles (native z-index issue)
        var logToggle = this.FindControl<ToggleButton>("LogToggle");
        if (logToggle is not null)
            logToggle.IsCheckedChanged += (_, _) => SyncWebViewVisibility();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old VM
        if (_vm is not null)
            _vm.NavigationDecisionMade -= OnNavigationDecisionMade;

        _vm = DataContext as NavigationInterceptViewModel;

        if (_vm is null) return;

        _vm.NavigationDecisionMade += OnNavigationDecisionMade;

        // Wire up WebView events
        _webView = this.FindControl<WebView>("WebViewControl");
        if (_webView is null) return;

        _webView.NavigationStarted += OnWebViewNavigationStarted;
        _webView.NavigationCompleted += OnWebViewNavigationCompleted;

        // Do NOT call LoadTestPage here — WebView native core is not yet created.
        // It will be loaded in OnAttachedToVisualTree instead.
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_testPageLoaded)
        {
            _testPageLoaded = true;
            // Use a delayed dispatch to ensure the WebView native control is created
            Dispatcher.UIThread.Post(
                () => LoadTestPage(),
                DispatcherPriority.Loaded);
        }
    }

    private async void LoadTestPage()
    {
        if (_vm is null || _webView is null) return;

        // Retry with delay — the WebView native core may need a moment after visual tree attachment
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await _webView.NavigateToStringAsync(_vm.TestPageHtml);
                _vm.LogLine("[Init] Test page loaded");
                return;
            }
            catch (InvalidOperationException) when (attempt < 4)
            {
                // WebView not ready yet; wait and retry
                await Task.Delay(300 * (attempt + 1));
            }
            catch (Exception ex)
            {
                _vm.LogLine($"[Init] Failed: {ex.Message}");
                return;
            }
        }
    }

    /// <summary>
    /// Synchronous navigation interception: if the VM says we should intercept,
    /// set Cancel = true immediately, then let the VM show the confirmation overlay.
    /// </summary>
    private void OnWebViewNavigationStarted(object? sender, NavigationStartingEventArgs e)
    {
        if (_vm is null) return;

        if (_vm.ShouldIntercept(e.RequestUri))
        {
            e.Cancel = true;

            // Also hide WebView while the dialog is showing (native z-index issue)
            SyncWebViewVisibility();
        }
    }

    private void OnWebViewNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
    {
        _vm?.LogLine($"[Completed] {e.Status} → {e.RequestUri}");
    }

    /// <summary>
    /// Called after the user picks Allow / Block / SafePage.
    /// </summary>
    private async void OnNavigationDecisionMade(Uri? targetUri)
    {
        // Re-show the WebView (dialog is now hidden)
        SyncWebViewVisibility();

        if (targetUri is null) return; // Blocked — nothing to do

        if (targetUri.AbsoluteUri == "about:reload-test-page")
        {
            _testPageLoaded = false;
            LoadTestPage();
            return;
        }

        // Navigate to the decided URI (allowed or safe page)
        try
        {
            if (_webView is not null)
                await _webView.NavigateAsync(targetUri);
        }
        catch (Exception ex)
        {
            _vm?.LogLine($"[Error] Navigation failed: {ex.Message}");
        }
    }

    // ── UI helpers ──────────────────────────────────

    private void SyncWebViewVisibility()
    {
        var logToggle = this.FindControl<ToggleButton>("LogToggle");
        var container = this.FindControl<Border>("WebViewContainer");
        if (container is null) return;

        var logOpen = logToggle?.IsChecked == true;
        var dialogOpen = _vm?.IsInterceptDialogVisible == true;

        container.IsVisible = !logOpen && !dialogOpen;
    }

    private void OnCloseLogPanel(object? sender, RoutedEventArgs e)
    {
        var toggle = this.FindControl<ToggleButton>("LogToggle");
        if (toggle is not null)
            toggle.IsChecked = false;
    }

    private void OnOverlayDismiss(object? sender, PointerPressedEventArgs e)
    {
        var toggle = this.FindControl<ToggleButton>("LogToggle");
        if (toggle is not null)
            toggle.IsChecked = false;
    }
}
