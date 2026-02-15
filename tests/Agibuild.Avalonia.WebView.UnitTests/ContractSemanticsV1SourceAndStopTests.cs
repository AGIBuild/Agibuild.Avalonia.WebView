using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

[Collection("NavigationSemantics")]
public sealed class ContractSemanticsV1SourceAndStopTests
{
    [Fact]
    public void Source_set_updates_last_requested_uri_and_starts_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Uri? startedUri = null;
        NavigationCompletedEventArgs? completed = null;

        webView.NavigationStarted += (_, args) => startedUri = args.RequestUri;
        webView.NavigationCompleted += (_, args) => completed = args;

        var uri = new Uri("https://example.test/source");
        webView.Source = uri;
        DispatcherTestPump.WaitUntil(dispatcher, () => startedUri == uri && adapter.LastNavigationUri == uri);

        Assert.Equal(uri, webView.Source);
        Assert.Equal(uri, adapter.LastNavigationUri);
        Assert.Equal(uri, startedUri);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        DispatcherTestPump.WaitUntil(dispatcher, () => completed is not null);
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Success, completed!.Status);
    }

    [Fact]
    public void Source_set_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => webView.Source = null!);
    }

    [Fact]
    public void NavigateToString_sets_Source_to_about_blank_and_Started_uses_about_blank()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Uri? startedUri = null;
        webView.NavigationStarted += (_, args) => startedUri = args.RequestUri;

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateToStringAsync("<html></html>"));
        dispatcher.RunAll();

        Assert.Equal(new Uri("about:blank"), webView.Source);
        Assert.Equal(new Uri("about:blank"), startedUri);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        DispatcherTestPump.WaitUntil(dispatcher, () => navTask.IsCompleted);
        navTask.GetAwaiter().GetResult();
    }

    [Fact]
    public void StopAsync_returns_false_when_idle()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => webView.StopAsync()));
    }

    [Fact]
    public void StopAsync_cancels_active_navigation_and_completes_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, args) => completed = args;

        var navTask = ThreadingTestHelper.RunOffThread(() => webView.NavigateAsync(new Uri("https://example.test")));
        dispatcher.RunAll();

        var stopTask = ThreadingTestHelper.RunOffThread(() => webView.StopAsync());
        DispatcherTestPump.WaitUntil(dispatcher, () => stopTask.IsCompleted);
        Assert.True(stopTask.GetAwaiter().GetResult());

        DispatcherTestPump.WaitUntil(dispatcher, () => navTask.IsCompleted);
        navTask.GetAwaiter().GetResult();

        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
    }

}
