using System.Collections.Immutable;
using System.Text.Json;
using Agibuild.Fulora.Bridge.Generator;
using Agibuild.Fulora.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

// ==================== Test interface with CancellationToken ====================

[JsExport]
public interface ICancellableService
{
    Task<string> LongOperation(string input, CancellationToken ct);
    Task<int> NormalOperation(int value);
}

public class FakeCancellableService : ICancellableService
{
    public async Task<string> LongOperation(string input, CancellationToken ct)
    {
        await Task.Delay(5000, ct);
        return $"done:{input}";
    }

    public Task<int> NormalOperation(int value)
        => Task.FromResult(value * 2);
}

// ==================== Tests ====================

public sealed class BridgeCancellationTests
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
    public void CancellationToken_method_does_not_report_AGBR004()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsExport]
            public interface ICancellable
            {
                Task<string> Process(string input, CancellationToken ct);
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
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR004");
        Assert.Contains(driver.GetRunResult().GeneratedTrees, t => t.FilePath.Contains("CancellableBridgeRegistration"));
    }

    [Fact]
    public void Generated_TypeScript_includes_AbortSignal_option()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsExport]
            public interface ISearchService
            {
                Task<string> Search(string query, CancellationToken ct);
                Task<int> Count();
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
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var tsTree = driver.GetRunResult().GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));

        Assert.NotNull(tsTree);
        var tsContent = tsTree!.GetText().ToString();

        Assert.Contains("signal?: AbortSignal", tsContent);
        Assert.Contains("search(query: string, options?: { signal?: AbortSignal }): Promise<string>", tsContent);
        Assert.DoesNotContain("options", tsContent.Split('\n').First(l => l.Contains("count(")));
    }

    [Fact]
    public void CancelRequest_cancels_active_handler()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICancellableService>(new FakeCancellableService());
        capturedScripts.Clear();

        // Start a long operation
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"cancel-1","method":"CancellableService.longOperation","params":{"input":"test"}}""",
            "*", core.ChannelId);

        // Send cancel request immediately (as a notification, no id)
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"cancel-1"}}""",
            "*", core.ChannelId);

        DispatcherTestPump.WaitUntil(
            _dispatcher,
            () => capturedScripts.Any(s => s.Contains("_onResponse") && s.Contains("-32800")),
            timeout: TimeSpan.FromSeconds(5));

        var lastResponse = capturedScripts.LastOrDefault(s => s.Contains("_onResponse"));
        Assert.NotNull(lastResponse);
        Assert.Contains("-32800", lastResponse);
    }

    [Fact]
    public void CancelRequest_for_unknown_id_is_silently_ignored()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICancellableService>(new FakeCancellableService());
        capturedScripts.Clear();

        var exception = Record.Exception(() =>
        {
            adapter.RaiseWebMessage(
                """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"nonexistent"}}""",
                "*", core.ChannelId);
            _dispatcher.RunAll();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Normal_method_still_works_alongside_cancellable()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICancellableService>(new FakeCancellableService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"norm-1","method":"CancellableService.normalOperation","params":{"value":21}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("42", response);
    }

    [Fact]
    public void Generated_JS_stub_includes_signal_support_for_cancellable_methods()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICancellableService>(new FakeCancellableService());

        var serviceStub = capturedScripts.Last();
        Assert.Contains("options", serviceStub);
        Assert.Contains("signal", serviceStub);
    }

    [Fact]
    public void RPC_bootstrap_stub_includes_cancelRequest_support()
    {
        Assert.Contains("$/cancelRequest", WebViewRpcService.JsStub);
        Assert.Contains("signal", WebViewRpcService.JsStub);
    }
}
