using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

[JsExport]
public interface IBinaryService
{
    Task<byte[]> Echo(byte[] data);
    Task<byte[]> GenerateBytes(int count);
}

public class FakeBinaryService : IBinaryService
{
    public Task<byte[]> Echo(byte[] data) => Task.FromResult(data);

    public Task<byte[]> GenerateBytes(int count)
    {
        var result = new byte[count];
        for (int i = 0; i < count; i++)
            result[i] = (byte)(i % 256);
        return Task.FromResult(result);
    }
}

/// <summary>
/// Integration tests for bridge binary payload (byte[] ↔ Uint8Array).
/// Exercises the full WebViewCore → Bridge.Expose → MockAdapter stack
/// with byte[] parameters and return values.
/// </summary>
public sealed class BridgeBinaryPayloadIntegrationTests
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
    public void Generated_JS_stub_includes_binary_decode_helper()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IBinaryService>(new FakeBinaryService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("_decodeBinaryResult", serviceStub);
        core.Dispose();
    }

    [Fact]
    public void Binary_echo_round_trip_through_bridge()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IBinaryService>(new FakeBinaryService());
        capturedScripts.Clear();

        var testData = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0xFF });

        adapter.RaiseWebMessage(
            $"{{\"jsonrpc\":\"2.0\",\"id\":\"bin-1\",\"method\":\"BinaryService.echo\",\"params\":{{\"data\":\"{testData}\"}}}}",
            "*", core.ChannelId);

        DispatcherTestPump.WaitUntil(_dispatcher,
            () => capturedScripts.Any(s => s.Contains("_onResponse") && s.Contains("bin-1")),
            timeout: TimeSpan.FromSeconds(5));

        var response = capturedScripts.First(s => s.Contains("bin-1"));
        Assert.Contains(testData, response);
        core.Dispose();
    }

    [Fact]
    public void Binary_generation_returns_base64_encoded_result()
    {
        var (core, adapter) = CreateCoreWithBridge();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IBinaryService>(new FakeBinaryService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"bin-gen-1","method":"BinaryService.generateBytes","params":{"count":4}}""",
            "*", core.ChannelId);

        DispatcherTestPump.WaitUntil(_dispatcher,
            () => capturedScripts.Any(s => s.Contains("_onResponse") && s.Contains("bin-gen-1")),
            timeout: TimeSpan.FromSeconds(5));

        var response = capturedScripts.First(s => s.Contains("bin-gen-1"));
        var expected = Convert.ToBase64String(new byte[] { 0, 1, 2, 3 });
        Assert.Contains(expected, response);
        core.Dispose();
    }

    [Fact]
    public void RPC_bootstrap_stub_includes_uint8_conversion_functions()
    {
        Assert.Contains("_uint8ToBase64", WebViewRpcService.JsStub);
        Assert.Contains("_base64ToUint8", WebViewRpcService.JsStub);
        Assert.Contains("_encodeBinaryPayload", WebViewRpcService.JsStub);
        Assert.Contains("_decodeBinaryResult", WebViewRpcService.JsStub);
    }
}
