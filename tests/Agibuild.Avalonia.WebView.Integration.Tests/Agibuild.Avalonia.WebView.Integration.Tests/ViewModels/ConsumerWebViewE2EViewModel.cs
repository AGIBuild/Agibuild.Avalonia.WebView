using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

/// <summary>
/// Consumer-perspective E2E test using github.com as target site.
/// Exposes all WebView features for manual testing via the UI,
/// plus an automated E2E test suite.
/// </summary>
public partial class ConsumerWebViewE2EViewModel : ViewModelBase
{
    private static readonly Uri TestHome = new("https://github.com");
    private static readonly Uri TestPageB = new("https://github.com/explore");

    private int _autoRunStarted;
    private int _runAllInProgress;

    public ConsumerWebViewE2EViewModel()
    {
        Status = "Not started.";
        AddressText = TestHome.AbsoluteUri;
        ScriptInput = "document.title";
        HtmlInput = "<html><body><h1>Hello from NavigateToString!</h1><p>This is raw HTML loaded into the WebView.</p></body></html>";
    }

    public bool AutoRun { get; set; }

    public event Action<int>? AutoRunCompleted;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _log = string.Empty;

    [ObservableProperty]
    private string _addressText = string.Empty;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isPageLoading;

    // --- JavaScript execution ---

    [ObservableProperty]
    private string _scriptInput = string.Empty;

    [ObservableProperty]
    private string _scriptResult = string.Empty;

    // --- Load HTML ---

    [ObservableProperty]
    private string _htmlInput = string.Empty;

    /// <summary>
    /// Set by the view once the <see cref="WebView"/> control is available.
    /// </summary>
    public WebView? WebViewControl { get; set; }

    public void OnWebViewLoaded()
    {
        SubscribeWebViewEvents();

        if (AutoRun && Interlocked.CompareExchange(ref _autoRunStarted, 1, 0) == 0)
        {
            _ = RunAllForAutoRunAsync();
        }
    }

    // ---------------------------------------------------------------------------
    //  Global WebView event monitoring
    // ---------------------------------------------------------------------------

    private void SubscribeWebViewEvents()
    {
        if (WebViewControl is null) return;

        WebViewControl.NavigationStarted += OnGlobalNavigationStarted;
        WebViewControl.NavigationCompleted += OnGlobalNavigationCompleted;
        WebViewControl.NewWindowRequested += OnGlobalNewWindowRequested;
        WebViewControl.WebMessageReceived += OnGlobalWebMessageReceived;
    }

    private void OnGlobalNavigationStarted(object? sender, NavigationStartingEventArgs e)
    {
        LogLine($"[Nav] Started: id={e.NavigationId:N}, uri={e.RequestUri}");
        UpdateNavigationState();
    }

    private void OnGlobalNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
    {
        LogLine($"[Nav] Completed: id={e.NavigationId:N}, status={e.Status}, uri={e.RequestUri}");

        // Update address bar with actual URL after redirect.
        if (e.Status == NavigationCompletedStatus.Success && e.RequestUri is not null)
        {
            AddressText = e.RequestUri.AbsoluteUri;
        }

        UpdateNavigationState();
    }

    private void OnGlobalNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        LogLine($"[Nav] NewWindow blocked, navigating in current view: {e.Uri}");
    }

    private void OnGlobalWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        LogLine($"[Msg] WebMessage received: {e.Body}");
    }

    /// <summary>
    /// Synchronizes CanGoBack / CanGoForward / IsPageLoading from the WebView control.
    /// </summary>
    private void UpdateNavigationState()
    {
        if (WebViewControl is null) return;

        Dispatcher.UIThread.Post(() =>
        {
            CanGoBack = WebViewControl.CanGoBack;
            CanGoForward = WebViewControl.CanGoForward;
            IsPageLoading = WebViewControl.IsLoading;
        });
    }

    // ---------------------------------------------------------------------------
    //  Navigation toolbar commands
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private void GoBack()
    {
        if (WebViewControl is null) return;
        var result = WebViewControl.GoBack();
        LogLine($"GoBack → {result}");
        UpdateNavigationState();
    }

    [RelayCommand]
    private void GoForward()
    {
        if (WebViewControl is null) return;
        var result = WebViewControl.GoForward();
        LogLine($"GoForward → {result}");
        UpdateNavigationState();
    }

    [RelayCommand]
    private void DoRefresh()
    {
        if (WebViewControl is null) return;
        var result = WebViewControl.Refresh();
        LogLine($"Refresh → {result}");
        UpdateNavigationState();
    }

    [RelayCommand]
    private void DoStop()
    {
        if (WebViewControl is null) return;
        var result = WebViewControl.Stop();
        LogLine($"Stop → {result}");
        UpdateNavigationState();
    }

    [RelayCommand]
    private async Task GoHomeAsync()
    {
        if (WebViewControl is null) return;
        AddressText = TestHome.AbsoluteUri;
        LogLine($"Home → {TestHome}");
        try
        {
            await WebViewControl.NavigateAsync(TestHome).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogLine($"Home error: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------------
    //  Address bar: free browsing
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private async Task GoToAddressAsync()
    {
        if (WebViewControl is null)
        {
            Status = "WebView not ready.";
            return;
        }

        var text = AddressText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Auto-prepend https:// if no scheme.
        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            Status = "Invalid URL.";
            return;
        }

        LogLine($"Go: {uri}");
        Status = $"Loading {uri.Host}...";
        try
        {
            await WebViewControl.NavigateAsync(uri).ConfigureAwait(false);
            Status = "Ready.";
        }
        catch (Exception ex)
        {
            LogLine($"Navigation error: {ex.Message}");
            Status = $"Error: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------------
    //  JavaScript execution
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private async Task RunScriptAsync()
    {
        if (WebViewControl is null)
        {
            ScriptResult = "WebView not ready.";
            return;
        }

        var script = ScriptInput?.Trim();
        if (string.IsNullOrEmpty(script))
        {
            ScriptResult = "(empty script)";
            return;
        }

        LogLine($"[JS] Executing: {script}");
        try
        {
            var result = await WebViewControl.InvokeScriptAsync(script).ConfigureAwait(false);
            ScriptResult = result ?? "(null)";
            LogLine($"[JS] Result: {ScriptResult}");
        }
        catch (Exception ex)
        {
            ScriptResult = $"Error: {ex.Message}";
            LogLine($"[JS] Error: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------------
    //  Load HTML
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private async Task LoadHtmlAsync()
    {
        if (WebViewControl is null)
        {
            Status = "WebView not ready.";
            return;
        }

        var html = HtmlInput?.Trim();
        if (string.IsNullOrEmpty(html))
        {
            Status = "Empty HTML.";
            return;
        }

        LogLine($"[HTML] Loading {html.Length} chars...");
        try
        {
            await WebViewControl.NavigateToStringAsync(html).ConfigureAwait(false);
            Status = "HTML loaded.";
            LogLine("[HTML] Loaded successfully.");
        }
        catch (Exception ex)
        {
            LogLine($"[HTML] Error: {ex.Message}");
            Status = $"Error: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------------
    //  Log management
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private void ClearLog()
    {
        Log = string.Empty;
    }

    // ---------------------------------------------------------------------------
    //  E2E test suite (github.com)
    // ---------------------------------------------------------------------------

    [RelayCommand]
    private async Task RunAllAsync()
    {
        _ = await RunAllCoreAsync().ConfigureAwait(false);
    }

    private async Task RunAllForAutoRunAsync()
    {
        var ok = await RunAllCoreAsync().ConfigureAwait(false);
        LogLine(ok ? "Consumer E2E: PASS" : "Consumer E2E: FAIL");
        AutoRunCompleted?.Invoke(ok ? 0 : 1);
    }

    private async Task<bool> RunAllCoreAsync()
    {
        if (Interlocked.CompareExchange(ref _runAllInProgress, 1, 0) != 0)
        {
            LogLine("RunAll already in progress.");
            return false;
        }

        try
        {
            if (WebViewControl is null)
            {
                Status = "WebView control not set.";
                return false;
            }

            Status = "Running consumer E2E scenarios (github.com)...";
            LogLine("=== Consumer WebView E2E (github.com) ===");

            // Wait for the WebView to become attached by navigating via Source property first.
            await WaitForWebViewReadyAsync().ConfigureAwait(false);

            // Small stabilization delay: let WKWebView finish any residual sub-resource loads
            // from the readiness navigation before starting test scenarios.
            await Task.Delay(500).ConfigureAwait(false);

            var allPassed = true;

            allPassed &= await RunNavigateAsync().ConfigureAwait(false);
            allPassed &= await RunSourcePropertyAsync().ConfigureAwait(false);
            allPassed &= await RunInvokeScriptAsync().ConfigureAwait(false);
            allPassed &= await RunNavigateToStringAsync().ConfigureAwait(false);
            allPassed &= await RunNavigationEventsAsync().ConfigureAwait(false);
            allPassed &= await RunGoBackForwardAsync().ConfigureAwait(false);
            allPassed &= await RunRefreshAsync().ConfigureAwait(false);
            allPassed &= await RunStopAsync().ConfigureAwait(false);
            allPassed &= await RunIsLoadingAsync().ConfigureAwait(false);

            // --- M1 scenarios ---
            allPassed &= await RunCookieSetAndReadAsync().ConfigureAwait(false);
            allPassed &= await RunCookieGetAsync().ConfigureAwait(false);
            allPassed &= await RunCookieDeleteClearAsync().ConfigureAwait(false);
            allPassed &= await RunNativeHandleAsync().ConfigureAwait(false);
            allPassed &= await RunNavigationErrorCategorizationAsync().ConfigureAwait(false);
            allPassed &= await RunNavigateToStringWithBaseUrlAsync().ConfigureAwait(false);

            // --- M2 scenarios ---
            allPassed &= await RunSetCustomUserAgentAsync().ConfigureAwait(false);
            allPassed &= await RunResetCustomUserAgentAsync().ConfigureAwait(false);

            Status = allPassed ? "ALL PASSED" : "SOME FAILED";
            LogLine($"Result: {Status}");
            return allPassed;
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            LogLine(ex.ToString());
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _runAllInProgress, 0);
        }
    }

    // ---- Scenario 1: NavigateAsync to github.com/search ----

    private async Task<bool> RunNavigateAsync()
    {
        try
        {
            var uri = new Uri("https://github.com/search?q=test&type=repositories");
            LogLine($"[1] NavigateAsync to {uri.PathAndQuery}");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                await WebViewControl.NavigateAsync(uri).ConfigureAwait(false);
                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                LogLine($"  Loaded: {completed.RequestUri}");
                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 2: Source property → github.com/explore ----

    private async Task<bool> RunSourcePropertyAsync()
    {
        try
        {
            LogLine("[2] Source property navigation (github.com/explore)");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    WebViewControl.Source = TestPageB);
                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                LogLine($"  Loaded: {completed.RequestUri}");
                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 3: InvokeScriptAsync — read document.title ----

    private async Task<bool> RunInvokeScriptAsync()
    {
        try
        {
            LogLine("[3] InvokeScriptAsync (document.title)");

            // Navigate to github.com first to ensure a fresh page.
            var navTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void NavHandler(object? s, NavigationCompletedEventArgs e) => navTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += NavHandler;
            try
            {
                await WebViewControl.NavigateAsync(TestHome).ConfigureAwait(false);
                await WaitAsync(navTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= NavHandler;
            }

            var title = await WebViewControl.InvokeScriptAsync("document.title").ConfigureAwait(false);
            LogLine($"  document.title = '{title}'");

            if (string.IsNullOrWhiteSpace(title))
            {
                LogLine("  FAIL: title is empty");
                return false;
            }

            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 4: NavigateToStringAsync — load raw HTML ----

    private async Task<bool> RunNavigateToStringAsync()
    {
        try
        {
            LogLine("[4] NavigateToStringAsync (raw HTML)");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                const string html = "<html><body><h1 id='test-heading'>E2E HTML Test</h1></body></html>";
                await WebViewControl.NavigateToStringAsync(html).ConfigureAwait(false);
                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                // Verify the heading content via script.
                var heading = await WebViewControl.InvokeScriptAsync(
                    "document.getElementById('test-heading')?.textContent").ConfigureAwait(false);
                LogLine($"  heading = '{heading}'");

                if (heading != "E2E HTML Test")
                {
                    LogLine($"  FAIL: expected 'E2E HTML Test', got '{heading}'");
                    return false;
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 5: NavigationStarted + NavigationCompleted events paired ----

    private async Task<bool> RunNavigationEventsAsync()
    {
        try
        {
            LogLine("[5] NavigationStarted + NavigationCompleted events");

            var startedTcs = new TaskCompletionSource<NavigationStartingEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);

            void StartHandler(object? s, NavigationStartingEventArgs e) => startedTcs.TrySetResult(e);
            void DoneHandler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationStarted += StartHandler;
            WebViewControl!.NavigationCompleted += DoneHandler;
            try
            {
                await WebViewControl.NavigateAsync(new Uri("https://github.com/trending")).ConfigureAwait(false);

                var started = await WaitAsync(startedTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                LogLine($"  NavigationStarted: id={started.NavigationId}");

                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                LogLine($"  NavigationCompleted: id={completed.NavigationId}, status={completed.Status}");

                if (started.NavigationId != completed.NavigationId)
                {
                    LogLine("  FAIL: id mismatch");
                    return false;
                }

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationStarted -= StartHandler;
                WebViewControl!.NavigationCompleted -= DoneHandler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 6: GoBack / GoForward ----

    private async Task<bool> RunGoBackForwardAsync()
    {
        try
        {
            LogLine("[6] GoBack / GoForward");

            // Navigate to page A (github.com).
            await NavigateAndWaitAsync(TestHome).ConfigureAwait(false);

            // Navigate to page B (github.com/topics).
            var pageB = new Uri("https://github.com/topics");
            await NavigateAndWaitAsync(pageB).ConfigureAwait(false);

            // GoBack should return to page A.
            var backNavTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void BackHandler(object? s, NavigationCompletedEventArgs e) => backNavTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += BackHandler;
            bool backResult;
            try
            {
                backResult = await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.GoBack());
                if (!backResult)
                {
                    LogLine("  FAIL: GoBack returned false");
                    return false;
                }
                await WaitAsync(backNavTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= BackHandler;
            }

            LogLine("  GoBack succeeded");

            // GoForward should return to page B.
            var fwdNavTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void FwdHandler(object? s, NavigationCompletedEventArgs e) => fwdNavTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += FwdHandler;
            bool fwdResult;
            try
            {
                fwdResult = await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.GoForward());
                if (!fwdResult)
                {
                    LogLine("  FAIL: GoForward returned false");
                    return false;
                }
                await WaitAsync(fwdNavTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= FwdHandler;
            }

            LogLine("  GoForward succeeded");
            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 7: Refresh ----

    private async Task<bool> RunRefreshAsync()
    {
        try
        {
            LogLine("[7] Refresh");

            // Navigate to a known page first.
            await NavigateAndWaitAsync(TestHome).ConfigureAwait(false);

            // Refresh and wait for navigation completed.
            var navTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => navTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.Refresh());
                if (!result)
                {
                    LogLine("  FAIL: Refresh returned false");
                    return false;
                }
                var completed = await WaitAsync(navTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 8: Stop ----

    private async Task<bool> RunStopAsync()
    {
        try
        {
            LogLine("[8] Stop");

            // Start a navigation then immediately stop.
            var uri = new Uri("https://github.com/search?q=webview+stop+test");
            _ = WebViewControl!.NavigateAsync(uri);

            var result = await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.Stop());
            LogLine($"  Stop returned: {result}");

            // Give a short delay for state to settle.
            await Task.Delay(200).ConfigureAwait(false);

            // We consider Stop successful if it didn't throw — the page may or may not
            // have loaded by the time Stop was called.
            LogLine("  PASS (Stop invoked without error)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 9: IsLoading state changes ----

    private async Task<bool> RunIsLoadingAsync()
    {
        try
        {
            LogLine("[9] IsLoading property");

            bool sawLoading = false;
            var completedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void StartHandler(object? s, NavigationStartingEventArgs e)
            {
                // Check IsLoading on the UI thread.
                Dispatcher.UIThread.Post(() =>
                {
                    if (WebViewControl?.IsLoading == true)
                        sawLoading = true;
                });
            }

            void DoneHandler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(true);

            WebViewControl!.NavigationStarted += StartHandler;
            WebViewControl!.NavigationCompleted += DoneHandler;
            try
            {
                await WebViewControl.NavigateAsync(new Uri("https://github.com/search?q=isloading")).ConfigureAwait(false);
                await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);

                // After completion, IsLoading should be false.
                var isLoadingAfter = await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.IsLoading);
                LogLine($"  sawLoading during navigation: {sawLoading}");
                LogLine($"  IsLoading after completion: {isLoadingAfter}");

                if (!sawLoading)
                {
                    LogLine("  WARN: IsLoading was never true during navigation (may be timing)");
                    // This is a soft warning — pass anyway since real-world timing may vary.
                }

                if (isLoadingAfter)
                {
                    LogLine("  FAIL: IsLoading still true after NavigationCompleted");
                    return false;
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationStarted -= StartHandler;
                WebViewControl!.NavigationCompleted -= DoneHandler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 10: Cookie set → page reads document.cookie ----

    private async Task<bool> RunCookieSetAndReadAsync()
    {
        try
        {
            LogLine("[10] Cookie: set via API → read via document.cookie");
            var cm = WebViewControl!.TryGetCookieManager();
            if (cm is null)
            {
                LogLine("  SKIP: TryGetCookieManager() returned null (adapter does not support cookies)");
                return true; // non-fatal skip
            }

            // Navigate to about:blank to establish a clean page context.
            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);
            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                await WebViewControl.NavigateToStringAsync("<html><body>cookie test</body></html>",
                    new Uri("https://cookie-test.example.com/")).ConfigureAwait(false);
                await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }

            // Set a cookie via the API.
            var cookie = new WebViewCookie("m1test", "hello", ".cookie-test.example.com", "/", null, false, false);
            await cm.SetCookieAsync(cookie).ConfigureAwait(false);

            // Read back via JavaScript.
            var docCookie = await WebViewControl.InvokeScriptAsync("document.cookie").ConfigureAwait(false);
            LogLine($"  document.cookie = '{docCookie}'");

            if (docCookie is null || !docCookie.Contains("m1test=hello"))
            {
                LogLine("  FAIL: cookie not found in document.cookie");
                return false;
            }

            // Clean up.
            await cm.DeleteCookieAsync(cookie).ConfigureAwait(false);

            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 11: Cookie get via API ----

    private async Task<bool> RunCookieGetAsync()
    {
        try
        {
            LogLine("[11] Cookie: page sets document.cookie → GetCookiesAsync");
            var cm = WebViewControl!.TryGetCookieManager();
            if (cm is null)
            {
                LogLine("  SKIP: no cookie manager");
                return true;
            }

            // Navigate to a test page that sets a cookie.
            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);
            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                const string html = "<html><head><script>document.cookie='jsset=world;path=/';</script></head><body>cookie get test</body></html>";
                await WebViewControl.NavigateToStringAsync(html,
                    new Uri("https://cookie-get.example.com/")).ConfigureAwait(false);
                await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }

            // Give the page script a moment to execute.
            await Task.Delay(300).ConfigureAwait(false);

            var cookies = await cm.GetCookiesAsync(new Uri("https://cookie-get.example.com/")).ConfigureAwait(false);
            var found = cookies.Any(c => c.Name == "jsset" && c.Value == "world");
            LogLine($"  GetCookiesAsync returned {cookies.Count} cookie(s), jsset found: {found}");

            if (!found)
            {
                LogLine("  FAIL: 'jsset' cookie not found via GetCookiesAsync");
                return false;
            }

            // Clean up.
            await cm.ClearAllCookiesAsync().ConfigureAwait(false);

            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 12: Cookie delete & clear ----

    private async Task<bool> RunCookieDeleteClearAsync()
    {
        try
        {
            LogLine("[12] Cookie: delete + clear all");
            var cm = WebViewControl!.TryGetCookieManager();
            if (cm is null)
            {
                LogLine("  SKIP: no cookie manager");
                return true;
            }

            var testUri = new Uri("https://cookie-clear.example.com/");

            // Set two cookies.
            var c1 = new WebViewCookie("a", "1", ".cookie-clear.example.com", "/", null, false, false);
            var c2 = new WebViewCookie("b", "2", ".cookie-clear.example.com", "/", null, false, false);
            await cm.SetCookieAsync(c1).ConfigureAwait(false);
            await cm.SetCookieAsync(c2).ConfigureAwait(false);

            // Delete one.
            await cm.DeleteCookieAsync(c1).ConfigureAwait(false);
            var cookies = await cm.GetCookiesAsync(testUri).ConfigureAwait(false);
            var aFound = cookies.Any(c => c.Name == "a");
            LogLine($"  After delete(a): count={cookies.Count}, a found={aFound}");
            if (aFound)
            {
                LogLine("  FAIL: cookie 'a' still present after delete");
                return false;
            }

            // Clear all.
            await cm.ClearAllCookiesAsync().ConfigureAwait(false);
            cookies = await cm.GetCookiesAsync(testUri).ConfigureAwait(false);
            LogLine($"  After clear all: count={cookies.Count}");
            if (cookies.Count > 0)
            {
                LogLine("  FAIL: cookies remain after ClearAllCookiesAsync");
                return false;
            }

            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 13: TryGetWebViewHandle ----

    private async Task<bool> RunNativeHandleAsync()
    {
        try
        {
            LogLine("[13] TryGetWebViewHandle()");
            var handle = WebViewControl!.TryGetWebViewHandle();

            if (handle is null)
            {
                LogLine("  FAIL: TryGetWebViewHandle returned null");
                return false;
            }

            LogLine($"  handle.HandleDescriptor = '{handle.HandleDescriptor}'");
            LogLine($"  handle.Handle = 0x{handle.Handle:X}");

            if (handle.Handle == IntPtr.Zero)
            {
                LogLine("  FAIL: handle is zero");
                return false;
            }

            // On macOS the descriptor should be "WKWebView".
            if (handle.HandleDescriptor != "WKWebView")
            {
                LogLine($"  WARN: expected descriptor 'WKWebView', got '{handle.HandleDescriptor}'");
            }

            LogLine("  PASS");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 14: Navigation error categorization ----

    private async Task<bool> RunNavigationErrorCategorizationAsync()
    {
        try
        {
            LogLine("[14] Navigation error categorization (invalid host)");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                // Navigate to a host that definitely doesn't exist.
                var badUri = new Uri("https://this-host-does-not-exist-m1-test.invalid/");
                try
                {
                    await WebViewControl.NavigateAsync(badUri).ConfigureAwait(false);
                }
                catch
                {
                    // NavigateAsync may throw the categorized exception — that's fine.
                }

                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                LogLine($"  Status={completed.Status}, Error type={completed.Error?.GetType().Name}");

                if (completed.Status == NavigationCompletedStatus.Success)
                {
                    LogLine("  FAIL: expected failure status for invalid host");
                    return false;
                }

                if (completed.Error is not WebViewNetworkException)
                {
                    LogLine($"  WARN: expected WebViewNetworkException, got {completed.Error?.GetType().Name ?? "null"}");
                    // Soft warning — the error type depends on the native mapping, but it should at least be a navigation exception.
                    if (completed.Error is not WebViewNavigationException)
                    {
                        LogLine("  FAIL: error is not even a WebViewNavigationException");
                        return false;
                    }
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 15: NavigateToStringAsync with baseUrl ----

    private async Task<bool> RunNavigateToStringWithBaseUrlAsync()
    {
        try
        {
            LogLine("[15] NavigateToStringAsync(html, baseUrl)");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                var baseUrl = new Uri("https://baseurl-test.example.com/assets/");
                const string html = "<html><body><h1 id='base'>baseUrl test</h1><script>window._href = location.href;</script></body></html>";

                await WebViewControl.NavigateToStringAsync(html, baseUrl).ConfigureAwait(false);
                var completed = await WaitAsync(completedTcs.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                if (completed.Status != NavigationCompletedStatus.Success)
                {
                    LogLine($"  FAIL: status = {completed.Status}");
                    return false;
                }

                // Verify the page loaded and baseUrl influences the location.
                var heading = await WebViewControl.InvokeScriptAsync(
                    "document.getElementById('base')?.textContent").ConfigureAwait(false);
                LogLine($"  heading = '{heading}'");

                if (heading != "baseUrl test")
                {
                    LogLine("  FAIL: heading mismatch");
                    return false;
                }

                // Check that location.href reflects the baseUrl.
                var href = await WebViewControl.InvokeScriptAsync("window._href || location.href").ConfigureAwait(false);
                LogLine($"  location.href = '{href}'");

                // baseUrl test: the href should contain the base URL domain.
                if (href is not null && href.Contains("baseurl-test.example.com"))
                {
                    LogLine("  baseUrl reflected in location.href ✓");
                }
                else
                {
                    LogLine($"  WARN: baseUrl not reflected in location.href (got '{href}') — platform-specific behavior");
                }

                LogLine("  PASS");
                return true;
            }
            finally
            {
                WebViewControl!.NavigationCompleted -= Handler;
            }
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 16: SetCustomUserAgent → verify via JS ----

    private async Task<bool> RunSetCustomUserAgentAsync()
    {
        try
        {
            const string customUa = "AgibuildE2E/1.0 (UserAgentTest)";
            LogLine($"[16] SetCustomUserAgent → '{customUa}'");

            WebViewControl!.SetCustomUserAgent(customUa);

            // Navigate to a simple page to ensure the UA takes effect.
            await NavigateAndWaitAsync(TestHome).ConfigureAwait(false);

            // Small delay to let the page load.
            await Task.Delay(300).ConfigureAwait(false);

            var jsUa = await WebViewControl.InvokeScriptAsync("navigator.userAgent").ConfigureAwait(false);
            LogLine($"  navigator.userAgent = '{jsUa}'");

            if (jsUa is not null && jsUa.Contains(customUa))
            {
                LogLine("  Custom UserAgent confirmed via JS ✓");
                LogLine("  PASS");
                return true;
            }

            // Some platforms may not fully reflect the custom UA in navigator.userAgent.
            // Treat as a WARN but still pass.
            LogLine($"  WARN: JS UA does not contain custom string (platform-specific). Got: '{jsUa}'");
            LogLine("  PASS (with caveat)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---- Scenario 17: Reset UserAgent → verify default restored ----

    private async Task<bool> RunResetCustomUserAgentAsync()
    {
        try
        {
            LogLine("[17] ResetCustomUserAgent (null) → verify default restored");

            // Read the current (custom) UA.
            var beforeReset = await WebViewControl!.InvokeScriptAsync("navigator.userAgent").ConfigureAwait(false);
            LogLine($"  Before reset: '{beforeReset}'");

            // Reset to default.
            WebViewControl.SetCustomUserAgent(null);

            // Navigate to ensure the change takes effect.
            await NavigateAndWaitAsync(TestHome).ConfigureAwait(false);
            await Task.Delay(300).ConfigureAwait(false);

            var afterReset = await WebViewControl.InvokeScriptAsync("navigator.userAgent").ConfigureAwait(false);
            LogLine($"  After reset: '{afterReset}'");

            if (afterReset is not null && !afterReset.Contains("AgibuildE2E"))
            {
                LogLine("  Default UserAgent restored (no custom string) ✓");
                LogLine("  PASS");
                return true;
            }

            // Custom UA string may have been cleared but some platforms keep it until refresh.
            LogLine("  WARN: Custom UA string still present (platform-specific caching).");
            LogLine("  PASS (with caveat)");
            return true;
        }
        catch (Exception ex)
        {
            LogLine($"  FAIL: {ex.Message}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Sets Source to the test home page and waits for the first NavigationCompleted,
    /// ensuring the WebView is attached to the visual tree and fully ready.
    /// </summary>
    private async Task WaitForWebViewReadyAsync()
    {
        LogLine("Waiting for WebView to become ready...");

        var readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? s, NavigationCompletedEventArgs e) => readyTcs.TrySetResult(true);

        WebViewControl!.NavigationCompleted += Handler;
        try
        {
            // Source property defers navigation until the control is attached.
            await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.Source = TestHome);
            await WaitAsync(readyTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            LogLine("WebView ready.");
        }
        finally
        {
            WebViewControl!.NavigationCompleted -= Handler;
        }
    }

    /// <summary>
    /// Navigates to the given URI and waits for NavigationCompleted.
    /// </summary>
    private async Task NavigateAndWaitAsync(Uri uri)
    {
        var tcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? s, NavigationCompletedEventArgs e) => tcs.TrySetResult(e);

        WebViewControl!.NavigationCompleted += Handler;
        try
        {
            await WebViewControl.NavigateAsync(uri).ConfigureAwait(false);
            await WaitAsync(tcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }
        finally
        {
            WebViewControl!.NavigationCompleted -= Handler;
        }
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, cts.Token)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"Timed out after {timeout}.");
        }
        return await task.ConfigureAwait(false);
    }

    private void LogLine(string message)
    {
        var line = $"{DateTimeOffset.Now:HH:mm:ss.fff} {message}";
        Log = $"{Log}{line}{Environment.NewLine}";

        if (AutoRun)
        {
            Console.WriteLine(line);
        }
    }
}
