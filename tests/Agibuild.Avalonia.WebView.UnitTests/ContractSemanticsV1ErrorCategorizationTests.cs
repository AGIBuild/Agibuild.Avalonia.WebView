using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1ErrorCategorizationTests
{
    [Fact]
    public async Task NavigationCompleted_with_WebViewNetworkException_preserves_type()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, e) => completed = e;

        var navTask = webView.NavigateAsync(new Uri("https://example.test"));
        Assert.True(SpinWait.SpinUntil(() =>
        {
            dispatcher.RunAll();
            return adapter.LastNavigationId.HasValue;
        }, TimeSpan.FromSeconds(2)));

        var navId = adapter.LastNavigationId!.Value;
        var navUri = adapter.LastNavigationUri!;
        var networkError = new WebViewNetworkException("DNS lookup failed", navId, navUri);

        adapter.RaiseNavigationCompleted(navId, navUri, NavigationCompletedStatus.Failure, networkError);
        dispatcher.RunAll();

        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Failure, completed!.Status);
        Assert.IsType<WebViewNetworkException>(completed.Error);

        var ex = await Assert.ThrowsAsync<WebViewNetworkException>(() => navTask);
        Assert.Equal(navId, ex.NavigationId);
    }

    [Fact]
    public async Task NavigationCompleted_with_WebViewSslException_preserves_type()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, e) => completed = e;

        var navTask = webView.NavigateAsync(new Uri("https://example.test"));
        Assert.True(SpinWait.SpinUntil(() =>
        {
            dispatcher.RunAll();
            return adapter.LastNavigationId.HasValue;
        }, TimeSpan.FromSeconds(2)));

        var navId = adapter.LastNavigationId!.Value;
        var navUri = adapter.LastNavigationUri!;
        var sslError = new WebViewSslException("Certificate untrusted", navId, navUri);

        adapter.RaiseNavigationCompleted(navId, navUri, NavigationCompletedStatus.Failure, sslError);
        dispatcher.RunAll();

        Assert.NotNull(completed);
        Assert.IsType<WebViewSslException>(completed!.Error);

        var ex = await Assert.ThrowsAsync<WebViewSslException>(() => navTask);
        Assert.Equal(navId, ex.NavigationId);
    }

    [Fact]
    public async Task NavigationCompleted_with_WebViewTimeoutException_preserves_type()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, e) => completed = e;

        var navTask = webView.NavigateAsync(new Uri("https://example.test"));
        Assert.True(SpinWait.SpinUntil(() =>
        {
            dispatcher.RunAll();
            return adapter.LastNavigationId.HasValue;
        }, TimeSpan.FromSeconds(2)));

        var navId = adapter.LastNavigationId!.Value;
        var navUri = adapter.LastNavigationUri!;
        var timeoutError = new WebViewTimeoutException("Request timed out", navId, navUri);

        adapter.RaiseNavigationCompleted(navId, navUri, NavigationCompletedStatus.Failure, timeoutError);
        dispatcher.RunAll();

        Assert.NotNull(completed);
        Assert.IsType<WebViewTimeoutException>(completed!.Error);

        var ex = await Assert.ThrowsAsync<WebViewTimeoutException>(() => navTask);
        Assert.Equal(navId, ex.NavigationId);
    }

}
