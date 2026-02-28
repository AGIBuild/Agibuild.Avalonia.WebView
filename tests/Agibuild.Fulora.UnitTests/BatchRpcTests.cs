using System.Text.Json;
using Agibuild.Fulora.Bridge.Generator;
using Agibuild.Fulora.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BatchRpcTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter, List<string> Scripts) CreateCore()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        var scripts = new List<string>();
        adapter.ScriptCallback = script => { scripts.Add(script); return null; };
        return (core, adapter, scripts);
    }

    // ==================== Batch request processing ====================

    [Fact]
    public void Batch_request_returns_array_response()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        var batch = """[{"jsonrpc":"2.0","id":"b1","method":"AppService.getCurrentUser","params":{}},{"jsonrpc":"2.0","id":"b2","method":"AppService.getCurrentUser","params":{}}]""";
        adapter.RaiseWebMessage(batch, "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = scripts.FirstOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(responseScript);

        var json = ExtractResponseJson(responseScript);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        var ids = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();
        Assert.Contains("b1", ids);
        Assert.Contains("b2", ids);
    }

    [Fact]
    public void Batch_with_method_not_found_returns_error_for_missing_method()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        var batch = """[{"jsonrpc":"2.0","id":"ok1","method":"AppService.getCurrentUser","params":{}},{"jsonrpc":"2.0","id":"err1","method":"NoSuch.method","params":{}}]""";
        adapter.RaiseWebMessage(batch, "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = scripts.FirstOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(responseScript);

        var json = ExtractResponseJson(responseScript);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        var items = doc.RootElement.EnumerateArray().ToList();
        var ok = items.First(e => e.GetProperty("id").GetString() == "ok1");
        Assert.True(ok.TryGetProperty("result", out _));

        var err = items.First(e => e.GetProperty("id").GetString() == "err1");
        Assert.True(err.TryGetProperty("error", out var errorProp));
        Assert.Equal(-32601, errorProp.GetProperty("code").GetInt32());
    }

    [Fact]
    public void Batch_with_notification_does_not_produce_response_for_it()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        var batch = """[{"jsonrpc":"2.0","id":"r1","method":"AppService.getCurrentUser","params":{}},{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"none"}}]""";
        adapter.RaiseWebMessage(batch, "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = scripts.FirstOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(responseScript);

        var json = ExtractResponseJson(responseScript);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
        Assert.Equal("r1", doc.RootElement[0].GetProperty("id").GetString());
    }

    [Fact]
    public void Batch_with_invalid_jsonrpc_element_returns_invalid_request_error()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        var batch = """[{"jsonrpc":"2.0","id":"valid","method":"AppService.getCurrentUser","params":{}},{"id":"bad","method":"foo"}]""";
        adapter.RaiseWebMessage(batch, "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = scripts.FirstOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(responseScript);

        var json = ExtractResponseJson(responseScript);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        var items = doc.RootElement.EnumerateArray().ToList();
        var valid = items.First(e => e.GetProperty("id").GetString() == "valid");
        Assert.True(valid.TryGetProperty("result", out _));

        var bad = items.First(e => e.GetProperty("id").GetString() == "bad");
        Assert.True(bad.TryGetProperty("error", out var errProp));
        Assert.Equal(-32600, errProp.GetProperty("code").GetInt32());
    }

    [Fact]
    public void Empty_batch_produces_no_response()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        adapter.RaiseWebMessage("[]", "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.DoesNotContain(scripts, s => s.Contains("_onResponse"));
    }

    [Fact]
    public void All_notifications_batch_produces_no_response()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        _dispatcher.RunAll();
        scripts.Clear();

        var batch = """[{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"x"}}]""";
        adapter.RaiseWebMessage(batch, "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.DoesNotContain(scripts, s => s.Contains("_onResponse"));
    }

    // ==================== JS stub batch API ====================

    [Fact]
    public void JS_stub_contains_batch_method()
    {
        var stub = WebViewRpcService.JsStub;
        Assert.Contains("batch: function(calls)", stub);
        Assert.Contains("Promise.all(resultPromises)", stub);
        Assert.Contains("Array.isArray(msg)", stub);
    }

    [Fact]
    public void JS_stub_onResponse_handles_array()
    {
        var stub = WebViewRpcService.JsStub;
        Assert.Contains("Array.isArray(msg)", stub);
        Assert.Contains("resolveItem(msg[i])", stub);
        Assert.Contains("resolveItem(msg)", stub);
    }

    // ==================== TypeScript declarations ====================

    [Fact]
    public void TypeScript_declarations_include_batch_method()
    {
        var model = new BridgeInterfaceModel
        {
            InterfaceName = "ISimple",
            InterfaceFullName = "global::ISimple",
            Namespace = "TestNs",
            ServiceName = "Simple",
            Direction = BridgeDirection.Export,
            Methods = [new BridgeMethodModel { Name = "DoWork", CamelCaseName = "doWork", RpcMethodName = "Simple.doWork", ReturnTypeFullName = "void" }],
        };

        var tsCode = TypeScriptEmitter.EmitDeclarations([model], []);
        Assert.Contains("batch(calls: Array<{ method: string; params?: unknown }>): Promise<unknown[]>", tsCode);
    }

    // ==================== Helpers ====================

    private static string ExtractResponseJson(string script)
    {
        var marker = "_onResponse(";
        var start = script.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("No _onResponse call found");
        start += marker.Length;

        var jsonStr = script[start..^1]; // Remove trailing )
        return JsonSerializer.Deserialize<string>(jsonStr) ?? throw new InvalidOperationException("Failed to deserialize response JSON string");
    }
}
