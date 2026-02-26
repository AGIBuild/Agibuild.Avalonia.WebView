using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class OperationFailureTaxonomyTests
{
    [Fact]
    public void TryGetCategory_parses_case_insensitive_string_payload()
    {
        var ex = new InvalidOperationException("payload");
        ex.Data[GetCategoryDataKey()] = "dispatchfailed";

        var ok = WebViewOperationFailure.TryGetCategory(ex, out var category);

        Assert.True(ok);
        Assert.Equal(WebViewOperationFailureCategory.DispatchFailed, category);
    }

    [Fact]
    public void TryGetCategory_rejects_invalid_string_payload()
    {
        var ex = new InvalidOperationException("payload");
        ex.Data[GetCategoryDataKey()] = "not-a-category";

        var ok = WebViewOperationFailure.TryGetCategory(ex, out var category);

        Assert.False(ok);
        Assert.Equal(default, category);
    }

    [Fact]
    public async Task Async_api_after_dispose_has_Disposed_category()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var webView = new WebViewCore(adapter, dispatcher);
        webView.Dispose();

        var ex = await Assert.ThrowsAsync<ObjectDisposedException>(() => webView.StopAsync());
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.Disposed, category);
    }

    [Fact]
    public async Task Async_api_in_detaching_state_has_NotReady_category()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var webView = new WebViewCore(adapter, dispatcher);
        webView.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));
        webView.Detach();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => webView.StopAsync());
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.NotReady, category);
    }

    [Fact]
    public async Task Dispatch_failure_has_DispatchFailed_category()
    {
        var dispatcher = new ThrowingDispatcher();
        var adapter = new MockWebViewAdapter();
        using var webView = new WebViewCore(adapter, dispatcher);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => webView.StopAsync());
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.DispatchFailed, category);
    }

    [Fact]
    public async Task Adapter_failure_has_AdapterFailed_category()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter
        {
            ScriptException = new InvalidOperationException("adapter failure")
        };
        using var webView = new WebViewCore(adapter, dispatcher);

        var task = webView.InvokeScriptAsync("throw new Error('x')");
        Assert.True(SpinWait.SpinUntil(() =>
        {
            dispatcher.RunAll();
            return task.IsCompleted;
        }, TimeSpan.FromSeconds(2)));

        var ex = await Assert.ThrowsAsync<WebViewScriptException>(() => task);
        Assert.True(WebViewOperationFailure.TryGetCategory(ex, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
    }

    private sealed class ThrowingDispatcher : IWebViewDispatcher
    {
        public bool CheckAccess() => false;

        public Task InvokeAsync(Action action) => throw new InvalidOperationException("dispatch failure");
        public Task<T> InvokeAsync<T>(Func<T> func) => throw new InvalidOperationException("dispatch failure");
        public Task InvokeAsync(Func<Task> func) => throw new InvalidOperationException("dispatch failure");
        public Task<T> InvokeAsync<T>(Func<Task<T>> func) => throw new InvalidOperationException("dispatch failure");
    }

    private static string GetCategoryDataKey()
    {
        var keyField = typeof(WebViewOperationFailure).GetField(
            "CategoryDataKey",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(keyField);
        return Assert.IsType<string>(keyField!.GetValue(null));
    }
}
