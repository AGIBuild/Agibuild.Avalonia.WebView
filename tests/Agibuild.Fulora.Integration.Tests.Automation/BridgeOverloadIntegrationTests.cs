using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

[JsExport]
public interface IOverloadedCalcService
{
    Task<int> Compute(int a);
    Task<int> Compute(int a, int b);
    Task<string> GetName();
}

public class FakeOverloadedCalcService : IOverloadedCalcService
{
    public Task<int> Compute(int a) => Task.FromResult(a * 2);
    public Task<int> Compute(int a, int b) => Task.FromResult(a + b);
    public Task<string> GetName() => Task.FromResult("overloaded-calc");
}

/// <summary>
/// Integration tests for bridge overload resolution at runtime.
/// Exercises the full WebViewCore → Bridge.Expose → overload-disambiguated RPC → MockAdapter stack.
/// </summary>
public sealed class BridgeOverloadIntegrationTests
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
    public void Fewest_param_overload_uses_original_method_name()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IOverloadedCalcService>(new FakeOverloadedCalcService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ov-1","method":"OverloadedCalcService.compute","params":{"a":5}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        DispatcherTestPump.WaitUntil(_dispatcher,
            () => capturedScripts.Any(s => s.Contains("ov-1")),
            timeout: TimeSpan.FromSeconds(5));

        Assert.Contains(capturedScripts, s => s.Contains("10"));
        core.Dispose();
    }

    [Fact]
    public void More_params_overload_uses_suffixed_method_name()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IOverloadedCalcService>(new FakeOverloadedCalcService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ov-2","method":"OverloadedCalcService.compute$2","params":{"a":3,"b":7}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        DispatcherTestPump.WaitUntil(_dispatcher,
            () => capturedScripts.Any(s => s.Contains("ov-2")),
            timeout: TimeSpan.FromSeconds(5));

        Assert.Contains(capturedScripts, s => s.Contains("10"));
        core.Dispose();
    }

    [Fact]
    public void Non_overloaded_method_works_alongside_overloads()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IOverloadedCalcService>(new FakeOverloadedCalcService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ov-name","method":"OverloadedCalcService.getName","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains(capturedScripts, s => s.Contains("overloaded-calc"));
        core.Dispose();
    }

    [Fact]
    public void Generated_stub_includes_both_overload_variants()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IOverloadedCalcService>(new FakeOverloadedCalcService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("OverloadedCalcService.compute", serviceStub);
        Assert.Contains("OverloadedCalcService.compute$2", serviceStub);
        core.Dispose();
    }
}
