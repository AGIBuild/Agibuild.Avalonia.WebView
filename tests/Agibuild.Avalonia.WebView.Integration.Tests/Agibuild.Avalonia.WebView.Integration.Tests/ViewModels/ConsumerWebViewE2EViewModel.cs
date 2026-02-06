using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

/// <summary>
/// Consumer-perspective E2E test using bing.com as target site.
/// Also provides an address bar for free browsing.
/// </summary>
public partial class ConsumerWebViewE2EViewModel : ViewModelBase
{
    private static readonly Uri BingHome = new("https://www.bing.com");

    private int _autoRunStarted;
    private int _runAllInProgress;

    public ConsumerWebViewE2EViewModel()
    {
        Status = "Not started.";
        AddressText = BingHome.AbsoluteUri;
    }

    public bool AutoRun { get; set; }

    public event Action<int>? AutoRunCompleted;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _log = string.Empty;

    [ObservableProperty]
    private string _addressText = string.Empty;

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
    }

    private void OnGlobalNavigationStarted(object? sender, NavigationStartingEventArgs e)
    {
        LogLine($"[Nav] Started: id={e.NavigationId:N}, uri={e.RequestUri}");
    }

    private void OnGlobalNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
    {
        LogLine($"[Nav] Completed: id={e.NavigationId:N}, status={e.Status}, uri={e.RequestUri}");

        // Update address bar with actual URL after redirect.
        if (e.Status == NavigationCompletedStatus.Success && e.RequestUri is not null)
        {
            AddressText = e.RequestUri.AbsoluteUri;
        }
    }

    private void OnGlobalNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        LogLine($"[Nav] NewWindow blocked, navigating in current view: {e.Uri}");
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
    //  E2E test suite (bing.com)
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

            Status = "Running consumer E2E scenarios (bing.com)...";
            LogLine("=== Consumer WebView E2E (bing.com) ===");

            // Wait for the WebView to become attached by navigating via Source property first.
            await WaitForWebViewReadyAsync().ConfigureAwait(false);

            // Small stabilization delay: let WKWebView finish any residual sub-resource loads
            // from the readiness navigation before starting test scenarios.
            await Task.Delay(500).ConfigureAwait(false);

            var allPassed = true;

            allPassed &= await RunNavigateToBingAsync().ConfigureAwait(false);
            allPassed &= await RunSourcePropertyAsync().ConfigureAwait(false);
            allPassed &= await RunInvokeScriptAsync().ConfigureAwait(false);
            allPassed &= await RunNavigationEventsAsync().ConfigureAwait(false);

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

    // ---- Scenario 1: NavigateAsync to bing.com/search ----

    private async Task<bool> RunNavigateToBingAsync()
    {
        try
        {
            // Use a distinct URL (not bing.com which we already loaded in WaitForReady)
            // to guarantee a real navigation occurs.
            var uri = new Uri("https://www.bing.com/search?q=test");
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

    // ---- Scenario 2: Source property → bing.com/maps ----

    private async Task<bool> RunSourcePropertyAsync()
    {
        try
        {
            LogLine("[2] Source property navigation (bing.com/maps)");

            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += Handler;
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    WebViewControl.Source = new Uri("https://www.bing.com/maps"));
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

            // Navigate to bing.com first to ensure a fresh page.
            var navTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void NavHandler(object? s, NavigationCompletedEventArgs e) => navTcs.TrySetResult(e);

            WebViewControl!.NavigationCompleted += NavHandler;
            try
            {
                await WebViewControl.NavigateAsync(BingHome).ConfigureAwait(false);
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

    // ---- Scenario 4: NavigationStarted + NavigationCompleted events paired ----

    private async Task<bool> RunNavigationEventsAsync()
    {
        try
        {
            LogLine("[4] NavigationStarted + NavigationCompleted events");

            var startedTcs = new TaskCompletionSource<NavigationStartingEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completedTcs = new TaskCompletionSource<NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);

            void StartHandler(object? s, NavigationStartingEventArgs e) => startedTcs.TrySetResult(e);
            void DoneHandler(object? s, NavigationCompletedEventArgs e) => completedTcs.TrySetResult(e);

            WebViewControl!.NavigationStarted += StartHandler;
            WebViewControl!.NavigationCompleted += DoneHandler;
            try
            {
                await WebViewControl.NavigateAsync(new Uri("https://www.bing.com/images")).ConfigureAwait(false);

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

    // ---------------------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Sets Source to bing.com and waits for the first NavigationCompleted,
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
            await Dispatcher.UIThread.InvokeAsync(() => WebViewControl.Source = BingHome);
            await WaitAsync(readyTcs.Task, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            LogLine("WebView ready.");
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
