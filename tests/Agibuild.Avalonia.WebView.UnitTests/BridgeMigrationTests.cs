using System.Text.Json;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Backward compatibility tests: raw RPC (F6) and typed Bridge coexist.
/// Deliverable 1.6.
/// </summary>
public sealed class BridgeMigrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithRpc()
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
    public void Raw_RPC_handlers_coexist_with_Bridge_Expose()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        // Register a raw RPC handler (existing F6 pattern).
        bool rawHandlerCalled = false;
        core.Rpc!.Handle("legacy.ping", (JsonElement? args) =>
        {
            rawHandlerCalled = true;
            return Task.FromResult<object?>("pong");
        });

        // Also expose a typed Bridge service.
        core.Bridge.Expose<IAppService>(new FakeAppService());

        // Call the raw handler.
        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"leg-1","method":"legacy.ping","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(rawHandlerCalled);
        Assert.Contains("pong", capturedScripts.Last());

        // Call the typed handler.
        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"leg-2","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains("Alice", capturedScripts.Last());
    }

    [Fact]
    public void Bridge_Expose_is_opt_in_raw_RPC_still_works_alone()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        // Only use raw RPC — no Bridge at all.
        int addResult = 0;
        core.Rpc!.Handle("add", (JsonElement? args) =>
        {
            int a = args?.GetProperty("a").GetInt32() ?? 0;
            int b = args?.GetProperty("b").GetInt32() ?? 0;
            addResult = a + b;
            return Task.FromResult<object?>(addResult);
        });

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"add-1","method":"add","params":{"a":3,"b":7}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Equal(10, addResult);
    }

    [Fact]
    public void Bridge_auto_enable_does_not_break_subsequent_raw_RPC()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        // Access Bridge first (auto-enables).
        core.Bridge.Expose<IAppService>(new FakeAppService());

        // Now register raw handler on the same RPC service.
        bool rawCalled = false;
        core.Rpc!.Handle("raw.test", (JsonElement? args) =>
        {
            rawCalled = true;
            return Task.FromResult<object?>(42);
        });

        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"raw-1","method":"raw.test","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(rawCalled);
    }

    [Fact]
    public void Remove_typed_service_does_not_affect_raw_handlers()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Rpc!.Handle("raw.alive", (JsonElement? args) => Task.FromResult<object?>(true));
        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();

        // Raw handler should still work.
        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"alive-1","method":"raw.alive","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("true", response);
    }
}
