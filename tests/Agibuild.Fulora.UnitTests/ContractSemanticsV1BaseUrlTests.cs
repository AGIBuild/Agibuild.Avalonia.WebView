using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1BaseUrlTests
{
    [Fact]
    public void NavigateToStringAsync_with_baseUrl_sets_Source_to_baseUrl()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? started = null;
        webView.NavigationStarted += (_, e) => started = e;

        var baseUrl = new Uri("https://example.com/assets/");
        var navTask = Task.Run(
            () => webView.NavigateToStringAsync("<html></html>", baseUrl),
            TestContext.Current.CancellationToken);
        DispatcherTestPump.WaitUntil(dispatcher, () => started is not null);

        Assert.NotNull(started);
        Assert.Equal(baseUrl, started!.RequestUri);
        Assert.Equal(baseUrl, webView.Source);
        Assert.Equal(baseUrl, adapter.LastBaseUrl);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        DispatcherTestPump.WaitUntil(dispatcher, () => navTask.IsCompleted);
        navTask.GetAwaiter().GetResult();
    }

    [Fact]
    public void NavigateToStringAsync_with_null_baseUrl_preserves_about_blank_semantics()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? started = null;
        webView.NavigationStarted += (_, e) => started = e;

        var navTask = Task.Run(
            () => webView.NavigateToStringAsync("<html></html>", null),
            TestContext.Current.CancellationToken);
        DispatcherTestPump.WaitUntil(dispatcher, () => started is not null);

        Assert.NotNull(started);
        Assert.Equal(new Uri("about:blank"), started!.RequestUri);
        Assert.Equal(new Uri("about:blank"), webView.Source);
        Assert.Null(adapter.LastBaseUrl);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        DispatcherTestPump.WaitUntil(dispatcher, () => navTask.IsCompleted);
        navTask.GetAwaiter().GetResult();
    }

    [Fact]
    public void NavigateToStringAsync_single_param_delegates_to_overload_with_null_baseUrl()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var navTask = Task.Run(
            () => webView.NavigateToStringAsync("<html></html>"),
            TestContext.Current.CancellationToken);
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.LastNavigationId.HasValue);

        Assert.Null(adapter.LastBaseUrl);
        Assert.Equal(new Uri("about:blank"), webView.Source);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        DispatcherTestPump.WaitUntil(dispatcher, () => navTask.IsCompleted);
        navTask.GetAwaiter().GetResult();
    }
}
