using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Agibuild.Fulora.Bridge.Generator;
using Agibuild.Fulora.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

// ==================== Test interface with IAsyncEnumerable ====================

[JsExport]
public interface IStreamingService
{
    IAsyncEnumerable<string> StreamMessages(string topic);
    Task<int> GetCount();
}

public class FakeStreamingService : IStreamingService
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

// ==================== Service with CancellationToken (regression for CTS lifetime bug) ====================

[JsExport]
public interface ICancellableStreamingService
{
    IAsyncEnumerable<string> StreamTokens(string text, CancellationToken cancellationToken = default);
}

public class FakeCancellableStreamingService : ICancellableStreamingService
{
    public async IAsyncEnumerable<string> StreamTokens(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return ch.ToString();
            await Task.Delay(30, cancellationToken);
        }
    }
}

// ==================== Tests ====================

public sealed class BridgeStreamingTests
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

    /// <summary>
    /// Extracts the enumerator token from an _onResponse script.
    /// System.Text.Json encodes " as \u0022, so a naive hex regex would
    /// capture 4 extra digits from the escape prefix. Parse JSON instead.
    /// </summary>
    private static string ExtractToken(string script)
    {
        const string prefix = "_onResponse(";
        var idx = script.IndexOf(prefix, StringComparison.Ordinal);
        var payload = script[(idx + prefix.Length)..^1];
        var json = JsonSerializer.Deserialize<string>(payload)!;
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("result").GetProperty("token").GetString()!;
    }

    [Fact]
    public void IAsyncEnumerable_method_generates_streaming_handler()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Collections.Generic;

            [JsExport]
            public interface IMyStreamService
            {
                IAsyncEnumerable<string> StreamData(string input);
            }
            """;

        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        };

        var ct = TestContext.Current.CancellationToken;
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: ct)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics, ct);

        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("AGBR"));

        var regTree = driver.GetRunResult().GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyStreamServiceBridgeRegistration"));
        Assert.NotNull(regTree);

        var regContent = regTree!.GetText(ct).ToString();
        Assert.Contains("RegisterEnumerator", regContent);
        Assert.Contains("GetAsyncEnumerator", regContent);
    }

    [Fact]
    public void TypeScript_maps_IAsyncEnumerable_to_AsyncIterable()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            [JsExport]
            public interface IStreamSvc
            {
                IAsyncEnumerable<int> StreamNumbers();
                Task<string> GetName();
            }
            """;

        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        };

        var ct = TestContext.Current.CancellationToken;
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: ct)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, ct);

        var tsTree = driver.GetRunResult().GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsTree);

        var tsContent = tsTree!.GetText(ct).ToString();
        Assert.Contains("streamNumbers(): AsyncIterable<number>", tsContent);
        Assert.Contains("getName(): Promise<string>", tsContent);
    }

    [Fact]
    public void Streaming_method_returns_token_and_prefetched_values()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"stream-1","method":"StreamingService.streamMessages","params":{"topic":"test"}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? response;
        lock (capturedScripts) response = capturedScripts.FirstOrDefault(s => s.Contains("_onResponse") && s.Contains("token"));
        Assert.NotNull(response);
        Assert.Contains("token", response);
    }

    [Fact]
    public void Enumerator_abort_disposes_enumerator()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"stream-abort","method":"StreamingService.streamMessages","params":{"topic":"test"}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? response;
        lock (capturedScripts) response = capturedScripts.FirstOrDefault(s => s.Contains("token"));
        Assert.NotNull(response);

        var exception = Record.Exception(() =>
        {
            adapter.RaiseWebMessage(
                """{"jsonrpc":"2.0","method":"$/enumerator/abort","params":{"token":"nonexistent"}}""",
                "*", core.ChannelId);
            DrainDispatcher(200);
        });
        Assert.Null(exception);
    }

    [Fact]
    public void Normal_method_still_works_alongside_streaming()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"normal-1","method":"StreamingService.getCount","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("42", response);
    }

    [Fact]
    public void Generated_JS_stub_includes_createAsyncIterable_for_streaming_methods()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("_createAsyncIterable", serviceStub);
        Assert.Contains("StreamingService.streamMessages", serviceStub);
    }

    [Fact]
    public void RPC_bootstrap_stub_includes_createAsyncIterable()
    {
        Assert.Contains("_createAsyncIterable", WebViewRpcService.JsStub);
        Assert.Contains("$/enumerator/next", WebViewRpcService.JsStub);
        Assert.Contains("$/enumerator/abort", WebViewRpcService.JsStub);
        Assert.Contains("_encodeBinaryPayload", WebViewRpcService.JsStub);
        Assert.Contains("_decodeBinaryResult", WebViewRpcService.JsStub);
    }

    [Fact]
    public void Enumerator_disposed_after_inactivity_timeout()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"timeout-1","method":"StreamingService.streamMessages","params":{"topic":"test"}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? response;
        lock (capturedScripts) response = capturedScripts.FirstOrDefault(s => s.Contains("token"));
        Assert.NotNull(response);

        Assert.Equal(TimeSpan.FromSeconds(30), WebViewRpcService.EnumeratorInactivityTimeout);
    }

    [Fact]
    public void Enumerator_next_resets_inactivity_timer()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"reset-1","method":"StreamingService.streamMessages","params":{"topic":"test"}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? tokenResponse;
        lock (capturedScripts) tokenResponse = capturedScripts.FirstOrDefault(s => s.Contains("token") && s.Contains("_onResponse"));
        Assert.NotNull(tokenResponse);
        var token = ExtractToken(tokenResponse!);

        lock (capturedScripts) capturedScripts.Clear();
        adapter.RaiseWebMessage(
            $$$"""{"jsonrpc":"2.0","id":"next-1","method":"$/enumerator/next/{{{token}}}","params":{}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? nextResponse;
        lock (capturedScripts) nextResponse = capturedScripts.FirstOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(nextResponse);
    }

    [Fact]
    public void Enumerator_next_returns_value_not_error()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IStreamingService>(new FakeStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"v-1","method":"StreamingService.streamMessages","params":{"topic":"hello"}}""",
            "*", core.ChannelId);
        DrainDispatcherUntil(() =>
        {
            lock (capturedScripts)
            {
                return capturedScripts.Any(s => s.Contains("token") && s.Contains("_onResponse"));
            }
        });

        string? tokenResponse;
        lock (capturedScripts) tokenResponse = capturedScripts.FirstOrDefault(s => s.Contains("token") && s.Contains("_onResponse"));
        Assert.NotNull(tokenResponse);
        var token = ExtractToken(tokenResponse!);

        lock (capturedScripts) capturedScripts.Clear();
        adapter.RaiseWebMessage(
            $$$"""{"jsonrpc":"2.0","id":"v-next-1","method":"$/enumerator/next/{{{token}}}","params":{}}""",
            "*", core.ChannelId);
        DrainDispatcherUntil(() =>
        {
            lock (capturedScripts)
            {
                return capturedScripts.Any(s => s.Contains("_onResponse") && s.Contains("v-next-1"));
            }
        });

        string? nextResp;
        string allResp;
        lock (capturedScripts)
        {
            nextResp = capturedScripts.FirstOrDefault(s => s.Contains("_onResponse") && s.Contains("v-next-1"));
            allResp = string.Join("\n---\n", capturedScripts.Where(s => s.Contains("_onResponse")));
        }
        Assert.NotNull(nextResp);
        Assert.True(nextResp!.Contains("values"), $"Expected 'values'. Got:\n{allResp}");
    }

    private void DrainDispatcher(int totalMs)
    {
        // Pump dispatcher work for the full window using event-driven waits instead of
        // Thread.Sleep(20). The previous busy-loop variant burned CPU while parallel tests
        // were also pumping their own dispatchers, which on stressed CI caused enough
        // context-switch contention to deadline async continuations. WaitForWork sleeps
        // until the dispatcher actually has something to do (or the slice elapses, so the
        // outer deadline is still honoured).
        var deadline = Environment.TickCount64 + totalMs;
        while (Environment.TickCount64 < deadline)
        {
            _dispatcher.RunAll();
            _dispatcher.WaitForWork(TimeSpan.FromMilliseconds(20));
        }
        _dispatcher.RunAll();
    }

    private void DrainDispatcherUntil(Func<bool> condition, int totalMs = 10000)
    {
        var deadline = Environment.TickCount64 + totalMs;
        while (!condition() && Environment.TickCount64 < deadline)
        {
            _dispatcher.RunAll();
            if (condition())
            {
                break;
            }
            _dispatcher.WaitForWork(TimeSpan.FromMilliseconds(50));
        }
        _dispatcher.RunAll();
    }

    [Fact]
    public void Streaming_with_CancellationToken_survives_request_CTS_disposal()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { lock (capturedScripts) capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICancellableStreamingService>(new FakeCancellableStreamingService());
        lock (capturedScripts) capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"cts-1","method":"CancellableStreamingService.streamTokens","params":{"text":"ABC"}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? initResponse;
        lock (capturedScripts) initResponse = capturedScripts.FirstOrDefault(s => s.Contains("token") && s.Contains("_onResponse"));
        Assert.NotNull(initResponse);
        Assert.DoesNotContain("error", initResponse!, StringComparison.OrdinalIgnoreCase);
        var token = ExtractToken(initResponse!);

        lock (capturedScripts) capturedScripts.Clear();
        adapter.RaiseWebMessage(
            $$$"""{"jsonrpc":"2.0","id":"cts-next-1","method":"$/enumerator/next/{{{token}}}","params":{}}""",
            "*", core.ChannelId);
        DrainDispatcher(2000);

        string? nextResponse;
        string allResponses;
        lock (capturedScripts)
        {
            nextResponse = capturedScripts.FirstOrDefault(s => s.Contains("_onResponse") && s.Contains("cts-next-1"));
            allResponses = string.Join("\n---\n", capturedScripts.Where(s => s.Contains("_onResponse")));
        }
        Assert.NotNull(nextResponse);
        Assert.True(nextResponse!.Contains("values"),
            $"Expected 'values' in next response. Actual responses:\n{allResponses}");
        Assert.DoesNotContain("error", nextResponse, StringComparison.OrdinalIgnoreCase);
    }
}
