using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

[JsExport]
public interface IItStreamingService
{
    IAsyncEnumerable<string> StreamMessages(string topic);
    Task<int> GetCount();
}

public class FakeItStreamingService : IItStreamingService
{
    public async IAsyncEnumerable<string> StreamMessages(string topic)
    {
        for (int i = 1; i <= 3; i++)
        {
            yield return $"{topic}-{i}";
            await Task.Delay(10);
        }
    }

    public Task<int> GetCount() => Task.FromResult(42);
}

/// <summary>
/// Integration tests for bridge IAsyncEnumerable streaming over RPC.
/// Exercises the full WebViewCore → Bridge.Expose → streaming protocol → MockAdapter stack.
/// </summary>
public sealed class BridgeStreamingIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithBridge()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        return (core, adapter);
    }

    [Fact]
    public void Streaming_method_returns_token_in_response()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItStreamingService>(new FakeItStreamingService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"it-stream-1","method":"ItStreamingService.streamMessages","params":{"topic":"integration"}}""",
            "*", core.ChannelId);

        _dispatcher.RunAll();
        Thread.Sleep(200);
        _dispatcher.RunAll();

        var response = capturedScripts.FirstOrDefault(s => s.Contains("_onResponse") && s.Contains("token"));
        Assert.NotNull(response);
        Assert.Contains("token", response!);
        core.Dispose();
    }

    [Fact]
    public void Non_streaming_method_still_works_with_streaming_service()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItStreamingService>(new FakeItStreamingService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"it-count-1","method":"ItStreamingService.getCount","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains(capturedScripts, s => s.Contains("42"));
        core.Dispose();
    }

    [Fact]
    public void Enumerator_abort_for_unknown_token_does_not_throw()
    {
        var (core, adapter) = CreateCoreWithBridge();

        core.Bridge.Expose<IItStreamingService>(new FakeItStreamingService());

        var exception = Record.Exception(() =>
        {
            adapter.RaiseWebMessage(
                """{"jsonrpc":"2.0","method":"$/enumerator/abort","params":{"token":"it-nonexistent"}}""",
                "*", core.ChannelId);
            _dispatcher.RunAll();
        });

        Assert.Null(exception);
        core.Dispose();
    }

    [Fact]
    public void Generated_stub_includes_createAsyncIterable()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItStreamingService>(new FakeItStreamingService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("_createAsyncIterable", serviceStub);
        core.Dispose();
    }
}
