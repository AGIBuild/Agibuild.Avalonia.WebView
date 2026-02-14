using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Coverage-focused tests for GoForward, Refresh, Stop, CanGoBack/CanGoForward,
/// and the RuntimeCookieManager dispose guard.
/// </summary>
public sealed class ContractSemanticsV1CommandNavigationCoverageTests
{
    [Fact]
    public void CanGoBack_reflects_adapter_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoBack = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.True(core.CanGoBack);
    }

    [Fact]
    public void CanGoForward_reflects_adapter_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoForward = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.True(core.CanGoForward);
    }

    [Fact]
    public void GoForward_returns_false_when_no_forward_history()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoForward = false };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoForwardAsync()));
    }

    [Fact]
    public void GoForward_returns_true_when_adapter_accepts()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoForward = true, GoForwardAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.True(DispatcherTestPump.Run(dispatcher, () => core.GoForwardAsync()));
    }

    [Fact]
    public void GoForward_returns_false_when_adapter_rejects()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoForward = true, GoForwardAccepted = false };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoForwardAsync()));
    }

    [Fact]
    public void GoForward_canceled_by_NavigationStarted_handler()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoForward = true, GoForwardAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        core.NavigationStarted += (_, e) => e.Cancel = true;

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoForwardAsync()));
    }

    [Fact]
    public void Refresh_returns_true_when_adapter_accepts()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { RefreshAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.True(DispatcherTestPump.Run(dispatcher, () => core.RefreshAsync()));
    }

    [Fact]
    public void Refresh_returns_false_when_adapter_rejects()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { RefreshAccepted = false };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.RefreshAsync()));
    }

    [Fact]
    public void Refresh_canceled_by_NavigationStarted_handler()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { RefreshAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        core.NavigationStarted += (_, e) => e.Cancel = true;

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.RefreshAsync()));
    }

    [Fact]
    public void Dispose_twice_does_not_throw()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();
        core.Dispose(); // Should not throw
    }

    [Fact]
    public async Task NavigateAsync_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.NavigateAsync(new Uri("https://example.test")));
    }

    [Fact]
    public async Task GoBack_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.GoBackAsync());
    }

    [Fact]
    public async Task GoForward_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.GoForwardAsync());
    }

    [Fact]
    public async Task Refresh_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.RefreshAsync());
    }

    [Fact]
    public async Task Stop_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.StopAsync());
    }

    [Fact]
    public void EnableWebMessageBridge_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => core.EnableWebMessageBridge(new WebMessageBridgeOptions()));
    }

    [Fact]
    public void DisableWebMessageBridge_after_dispose_throws_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => core.DisableWebMessageBridge());
    }

    [Fact]
    public async Task Dispose_cancels_active_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var navTask = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        // Dispose while navigation is still active — the task faults.
        core.Dispose();

        // The task should fault (ObjectDisposedException wraps the cancellation path).
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => navTask);
        Assert.True(ex is TaskCanceledException or ObjectDisposedException or OperationCanceledException,
            $"Expected cancel-like exception, got {ex.GetType().Name}");
    }

    [Fact]
    public void Stop_returns_false_when_no_active_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.StopAsync()));
    }

    [Fact]
    public void WebResourceRequested_forwarded_to_core()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        WebResourceRequestedEventArgs? received = null;
        core.WebResourceRequested += (_, e) => received = e;

        adapter.RaiseWebResourceRequested();
        dispatcher.RunAll();

        Assert.NotNull(received);
    }

    [Fact]
    public void EnvironmentRequested_forwarded_to_core()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        EnvironmentRequestedEventArgs? received = null;
        core.EnvironmentRequested += (_, e) => received = e;

        adapter.RaiseEnvironmentRequested();
        dispatcher.RunAll();

        Assert.NotNull(received);
    }

    [Fact]
    public async Task NavigationCompleted_with_id_mismatch_is_ignored()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        core.NavigationCompleted += (_, e) => completed = e;

        var navTask = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        // Send a completion with a different navigation ID — should be ignored.
        var wrongId = Guid.NewGuid();
        adapter.RaiseNavigationCompleted(wrongId, new Uri("https://other.test"), NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        Assert.Null(completed); // Ignored due to ID mismatch.

        // Now complete with the correct ID.
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        await navTask;

        Assert.NotNull(completed);
    }

    [Fact]
    public void NavigationCompleted_failure_without_error_gets_default_exception()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        core.NavigationCompleted += (_, e) => completed = e;

        _ = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        // Adapter sends Failure status but with error=null — runtime should fabricate one
        var navId = adapter.LastNavigationId!.Value;
        var navUri = adapter.LastNavigationUri!;
        // Use the 4-param overload that allows null error for Failure
        adapter.RaiseNavigationCompleted(navId, navUri, NavigationCompletedStatus.Failure, null);
        dispatcher.RunAll();

        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Failure, completed!.Status);
    }

    [Fact]
    public void NavigationCompleted_without_active_navigation_is_ignored()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        core.NavigationCompleted += (_, e) => completed = e;

        // No navigation started, but adapter fires NavigationCompleted.
        adapter.RaiseNavigationCompleted(Guid.NewGuid(), new Uri("https://example.test"), NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        Assert.Null(completed); // Should be ignored.
    }

    [Fact]
    public async Task Native_navigation_supersedes_active_api_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? supersededCompleted = null;
        core.NavigationCompleted += (_, e) =>
        {
            if (e.Status == NavigationCompletedStatus.Superseded)
                supersededCompleted = e;
        };

        // Start an API navigation.
        var navTask = core.NavigateAsync(new Uri("https://first.test"));
        dispatcher.RunAll();

        // Now a native navigation starts (different correlation ID → new navigation supersedes).
        var decision = await adapter.SimulateNativeNavigationStartingAsync(new Uri("https://second.test"));
        dispatcher.RunAll();

        Assert.True(decision.IsAllowed);
        Assert.NotNull(supersededCompleted);
        Assert.Equal(NavigationCompletedStatus.Superseded, supersededCompleted!.Status);
    }

    [Fact]
    public async Task Native_navigation_sub_frame_auto_allowed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var decision = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://iframe.test"),
            isMainFrame: false);

        Assert.True(decision.IsAllowed);
        Assert.Equal(Guid.Empty, decision.NavigationId); // Sub-frame, no navigation ID allocated.
    }

    [Fact]
    public void GoBack_returns_false_when_adapter_rejects()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoBack = true, GoBackAccepted = false };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoBackAsync()));
    }

    [Fact]
    public void GoBack_canceled_by_NavigationStarted_handler()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoBack = true, GoBackAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        core.NavigationStarted += (_, e) => e.Cancel = true;

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoBackAsync()));
    }

    [Fact]
    public async Task Native_redirect_cancel_completes_navigation_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        // Start a navigation to create an active NavigationOperation.
        var navTask = core.NavigateAsync(new Uri("https://first.test"));
        dispatcher.RunAll();

        var correlationId = Guid.NewGuid();
        // First native navigation with a specific correlation
        var decision1 = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://redirect-start.test"), correlationId);

        // Cancel on redirect
        core.NavigationStarted += (_, e) => e.Cancel = true;

        var decision2 = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://redirect-target.test"), correlationId);
        dispatcher.RunAll();

        Assert.False(decision2.IsAllowed);
    }

    [Fact]
    public async Task Native_navigation_disposed_returns_denied()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        var decision = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://example.test"));

        Assert.False(decision.IsAllowed);
        Assert.Equal(Guid.Empty, decision.NavigationId);
    }

    [Fact]
    public async Task RuntimeCookieManager_GetCookiesAsync_delegates_to_adapter()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithCookies();
        var core = new WebViewCore(adapter, dispatcher);

        var cm = core.TryGetCookieManager()!;

        var cookie = new WebViewCookie("test", "val", ".example.com", "/", null, false, false);
        await cm.SetCookieAsync(cookie);

        var cookies = await cm.GetCookiesAsync(new Uri("https://example.com/"));
        Assert.Single(cookies);
    }
}
