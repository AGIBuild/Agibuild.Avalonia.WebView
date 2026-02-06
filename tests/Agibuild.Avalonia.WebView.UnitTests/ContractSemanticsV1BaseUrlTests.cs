using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1BaseUrlTests
{
    [Fact]
    public async Task NavigateToStringAsync_with_baseUrl_sets_Source_to_baseUrl()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? started = null;
        webView.NavigationStarted += (_, e) => started = e;

        var baseUrl = new Uri("https://example.com/assets/");
        var navTask = webView.NavigateToStringAsync("<html></html>", baseUrl);
        dispatcher.RunAll();

        Assert.NotNull(started);
        Assert.Equal(baseUrl, started!.RequestUri);
        Assert.Equal(baseUrl, webView.Source);
        Assert.Equal(baseUrl, adapter.LastBaseUrl);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        await navTask;
    }

    [Fact]
    public async Task NavigateToStringAsync_with_null_baseUrl_preserves_about_blank_semantics()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? started = null;
        webView.NavigationStarted += (_, e) => started = e;

        var navTask = webView.NavigateToStringAsync("<html></html>", null);
        dispatcher.RunAll();

        Assert.NotNull(started);
        Assert.Equal(new Uri("about:blank"), started!.RequestUri);
        Assert.Equal(new Uri("about:blank"), webView.Source);
        Assert.Null(adapter.LastBaseUrl);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        await navTask;
    }

    [Fact]
    public async Task NavigateToStringAsync_single_param_delegates_to_overload_with_null_baseUrl()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var navTask = webView.NavigateToStringAsync("<html></html>");
        dispatcher.RunAll();

        Assert.Null(adapter.LastBaseUrl);
        Assert.Equal(new Uri("about:blank"), webView.Source);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        await navTask;
    }
}
