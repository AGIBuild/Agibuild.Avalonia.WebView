using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class IsLoadingTests
{
    [Fact]
    public void IsLoading_is_false_initially()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(core.IsLoading);
    }

    [Fact]
    public void IsLoading_becomes_true_after_NavigateAsync()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        _ = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        Assert.True(core.IsLoading);
    }

    [Fact]
    public async Task IsLoading_becomes_false_after_NavigationCompleted()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var navTask = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        Assert.True(core.IsLoading);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        await navTask;

        Assert.False(core.IsLoading);
    }

    [Fact]
    public async Task IsLoading_becomes_false_after_NavigationFailed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var navTask = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Failure, new Exception("err"));
        dispatcher.RunAll();

        // NavigateAsync throws on failure; catch it.
        await Assert.ThrowsAsync<WebViewNavigationException>(() => navTask);

        Assert.False(core.IsLoading);
    }

    [Fact]
    public void IsLoading_becomes_false_after_Dispose()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        _ = core.NavigateAsync(new Uri("https://example.test"));
        dispatcher.RunAll();

        Assert.True(core.IsLoading);

        core.Dispose();

        Assert.False(core.IsLoading);
    }

    [Fact]
    public void IsLoading_becomes_true_after_Source_set()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Source = new Uri("https://example.test");

        Assert.True(core.IsLoading);
    }

    [Fact]
    public void IsLoading_becomes_false_after_Cancel_in_NavigationStarted()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.NavigationStarted += (_, args) => args.Cancel = true;

        core.Source = new Uri("https://example.test");

        Assert.False(core.IsLoading);
    }
}
