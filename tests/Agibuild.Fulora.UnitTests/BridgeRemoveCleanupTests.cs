using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Tests that Remove cleans up JS stubs and handles script execution failures gracefully.
/// </summary>
public sealed class BridgeRemoveCleanupTests
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
    public void Remove_executes_delete_script_for_JS_stub()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        capturedScripts.Clear();

        core.Bridge.Remove<IAppService>();

        Assert.Contains(capturedScripts, s => s.Contains("delete window.agWebView.bridge.AppService"));
    }

    [Fact]
    public void Remove_executes_delete_script_for_custom_named_service()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICustomNameService>(new FakeCustomNameService());
        capturedScripts.Clear();

        core.Bridge.Remove<ICustomNameService>();

        Assert.Contains(capturedScripts, s => s.Contains("delete window.agWebView.bridge.api"));
    }

    [Fact]
    public void Remove_tolerates_script_execution_failure()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var scriptCallCount = 0;
        adapter.ScriptCallback = script =>
        {
            scriptCallCount++;
            if (script.Contains("delete"))
                throw new InvalidOperationException("Page not loaded");
            return null;
        };

        core.Bridge.Expose<IAppService>(new FakeAppService());

        var exception = Record.Exception(() => core.Bridge.Remove<IAppService>());

        Assert.Null(exception);
    }

    [Fact]
    public void Remove_still_unregisters_handlers_when_script_fails()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script =>
        {
            capturedScripts.Add(script);
            if (script.Contains("delete"))
                throw new InvalidOperationException("Page not loaded");
            return null;
        };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"after-rm","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("-32601", response);
    }
}
