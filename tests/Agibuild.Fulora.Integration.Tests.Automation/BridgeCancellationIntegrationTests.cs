using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

[JsExport]
public interface IItCancellableService
{
    Task<string> LongOperation(string input, CancellationToken ct);
    Task<int> NormalOperation(int value);
}

public class FakeItCancellableService : IItCancellableService
{
    public async Task<string> LongOperation(string input, CancellationToken ct)
    {
        await Task.Delay(5000, ct);
        return $"done:{input}";
    }

    public Task<int> NormalOperation(int value) => Task.FromResult(value * 2);
}

/// <summary>
/// Integration tests for bridge CancellationToken support over RPC.
/// Exercises the full WebViewCore → Bridge.Expose → CancelRequest → MockAdapter stack.
/// </summary>
public sealed class BridgeCancellationIntegrationTests
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
    public void Cancel_request_produces_request_cancelled_error()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItCancellableService>(new FakeItCancellableService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"it-cancel-1","method":"ItCancellableService.longOperation","params":{"input":"test"}}""",
            "*", core.ChannelId);

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"it-cancel-1"}}""",
            "*", core.ChannelId);

        DispatcherTestPump.WaitUntil(_dispatcher,
            () => capturedScripts.Any(s => s.Contains("_onResponse") && s.Contains("-32800")),
            timeout: TimeSpan.FromSeconds(5));

        Assert.Contains(capturedScripts, s => s.Contains("-32800"));
        core.Dispose();
    }

    [Fact]
    public void Normal_method_works_alongside_cancellable_in_integration()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItCancellableService>(new FakeItCancellableService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"it-norm-1","method":"ItCancellableService.normalOperation","params":{"value":21}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains(capturedScripts, s => s.Contains("42"));
        core.Dispose();
    }

    [Fact]
    public void Generated_stub_includes_abort_signal_support()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IItCancellableService>(new FakeItCancellableService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("signal", serviceStub);
        Assert.Contains("options", serviceStub);
        core.Dispose();
    }
}
