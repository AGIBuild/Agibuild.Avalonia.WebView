using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1ThreadingTests
{
    [Fact]
    public void Async_apis_are_callable_from_non_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var stopTask = Task.Run(() => webView.StopAsync());
        SpinWait.SpinUntil(() => dispatcher.QueuedCount > 0, TimeSpan.FromSeconds(2));
        dispatcher.RunAll();
        var result = stopTask.GetAwaiter().GetResult();
        Assert.False(result);
    }

    [Fact]
    public async Task InvokeScriptAsync_from_non_ui_thread_is_marshaled_to_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter
        {
            ScriptResult = "ok"
        };
        using var webView = new WebViewCore(adapter, dispatcher);

        var task = Task.Run(() => webView.InvokeScriptAsync("1+1"));
        SpinWait.SpinUntil(() => dispatcher.QueuedCount > 0, TimeSpan.FromSeconds(2));
        dispatcher.RunAll();

        Assert.Equal("ok", await task);
        Assert.Equal(dispatcher.UiThreadId, adapter.LastInvokeScriptThreadId);
    }

    [Fact]
    public async Task CommandManager_async_calls_from_non_ui_thread_use_queue()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithCommands();
        using var webView = new WebViewCore(adapter, dispatcher);
        var commands = webView.TryGetCommandManager()!;

        var task = Task.Run(() => commands.CopyAsync());
        SpinWait.SpinUntil(() => dispatcher.QueuedCount > 0, TimeSpan.FromSeconds(2));
        dispatcher.RunAll();
        await task;

        Assert.Single(adapter.ExecutedCommands);
        Assert.Equal(WebViewCommand.Copy, adapter.ExecutedCommands[0]);
    }
}

