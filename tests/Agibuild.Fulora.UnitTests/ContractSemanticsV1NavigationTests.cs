using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1NavigationTests
{
    [Fact]
    public void NavigateAsync_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => { _ = webView.NavigateAsync(null!); });
    }

    [Fact]
    public void NavigateToStringAsync_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => { _ = webView.NavigateToStringAsync(null!); });
    }

    [Fact]
    public async Task Async_navigation_marshals_to_ui_thread_and_events_are_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var startedThreadId = -1;
        webView.NavigationStarted += (_, _) => startedThreadId = Environment.CurrentManagedThreadId;

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));

        dispatcher.RunAll();

        Assert.Equal(dispatcher.UiThreadId, adapter.LastNavigateThreadId);
        Assert.Equal(dispatcher.UiThreadId, startedThreadId);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        await navTask;
    }

    [Fact]
    public async Task Cancel_in_NavigationStarted_prevents_adapter_navigation_and_completes_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationStarted += (_, args) => args.Cancel = true;
        webView.NavigationCompleted += (_, args) => completed = args;

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));

        dispatcher.RunAll();

        Assert.Null(adapter.LastNavigateThreadId);

        await navTask;

        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
        Assert.Null(completed.Error);
    }

    [Fact]
    public async Task Latest_wins_supersedes_active_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var completedStatuses = new List<NavigationCompletedStatus>();
        webView.NavigationCompleted += (_, args) => completedStatuses.Add(args.Status);

        var nav1 = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test/1")));
        dispatcher.RunAll();

        var nav2 = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test/2")));
        dispatcher.RunAll();

        // nav1 should have completed successfully as superseded when nav2 started.
        ThreadingTestHelper.PumpUntil(dispatcher, () => completedStatuses.Contains(NavigationCompletedStatus.Superseded));
        Assert.Contains(NavigationCompletedStatus.Superseded, completedStatuses);

        // Complete nav2 on the UI thread (where dispatcher.RunAll works).
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        // Await both tasks after all dispatch work is done (avoids thread affinity issues).
        await nav1.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await nav2.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NavigationCompleted_is_exactly_once_per_navigation_id()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var count = 0;
        webView.NavigationCompleted += (_, _) => count++;

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));
        dispatcher.RunAll();

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        await navTask;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Navigation_failure_faults_with_WebViewNavigationException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));
        dispatcher.RunAll();

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Failure, new Exception("boom"));

        var ex = await Assert.ThrowsAsync<WebViewNavigationException>(() => navTask);
        Assert.Equal(new Uri("https://example.test"), ex.RequestUri);
    }

}

