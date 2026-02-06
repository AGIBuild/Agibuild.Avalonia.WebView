using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1LifecycleTests
{
    [Fact]
    public void Disposed_sync_apis_throw_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        webView.Dispose();

        Assert.Throws<ObjectDisposedException>(() => webView.Stop());
        Assert.Throws<ObjectDisposedException>(() => webView.GoBack());
        Assert.Throws<ObjectDisposedException>(() => webView.GoForward());
        Assert.Throws<ObjectDisposedException>(() => webView.Refresh());
    }

    [Fact]
    public async Task Disposed_async_apis_fault_with_ObjectDisposedException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        webView.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => webView.NavigateAsync(new Uri("https://example.test")));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => webView.NavigateToStringAsync("<html></html>"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => webView.InvokeScriptAsync("return 1;"));
    }

    [Fact]
    public void No_events_are_raised_after_dispose()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var raised = false;
        webView.NavigationCompleted += (_, _) => raised = true;
        webView.WebMessageReceived += (_, _) => raised = true;

        webView.Dispose();

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        adapter.RaiseWebMessage("{\"x\":1}", "https://example.test", Guid.NewGuid(), protocolVersion: 1);

        Assert.False(raised);
    }
}

