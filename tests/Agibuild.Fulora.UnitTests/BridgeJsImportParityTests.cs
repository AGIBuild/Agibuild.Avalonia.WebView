using System.Collections.Immutable;
using Agibuild.Fulora.Bridge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BridgeJsImportParityTests
{
    private static (ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result) RunGenerator(string source)
    {
        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
        };

        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references =
        [
            .. references,
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
        ];

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WebViewBridgeGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        return (diagnostics, driver.GetRunResult());
    }

    // ==================== CancellationToken on JsImport proxy ====================

    [Fact]
    public void JsImport_proxy_with_CancellationToken_generates_correctly()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsImport]
            public interface IRemoteService
            {
                Task<string> LongOperation(string input, CancellationToken ct);
                Task SimpleOp();
            }
            """;

        var (diagnostics, result) = RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var proxyFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("RemoteServiceBridgeProxy"));
        Assert.NotNull(proxyFile);

        var text = proxyFile!.GetText().ToString();
        Assert.Contains("InvokeAsync<string>", text);
        Assert.Contains("ct)", text);
        Assert.DoesNotContain("ct = ct", text);
    }

    [Fact]
    public void JsImport_proxy_with_CancellationToken_excludes_ct_from_params()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsImport]
            public interface ICtService
            {
                Task<int> Calculate(int a, int b, CancellationToken ct);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var proxyFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CtServiceBridgeProxy"));
        Assert.NotNull(proxyFile);

        var text = proxyFile!.GetText().ToString();
        Assert.Contains("var __params = new { a, b }", text);
        Assert.DoesNotContain("ct }", text);
    }

    // ==================== IAsyncEnumerable on JsImport proxy ====================

    [Fact]
    public void JsImport_proxy_with_IAsyncEnumerable_generates_iterator()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            [JsImport]
            public interface IDataSource
            {
                IAsyncEnumerable<string> StreamRecords(string query);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var proxyFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("DataSourceBridgeProxy"));
        Assert.NotNull(proxyFile);

        var text = proxyFile!.GetText().ToString();
        Assert.Contains("yield return", text);
        Assert.Contains("$/enumerator/next/", text);
        Assert.Contains("token", text);
    }

    // ==================== TypeScript for JsImport with CT ====================

    [Fact]
    public void TypeScript_JsImport_with_CancellationToken_maps_to_AbortSignal()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsImport]
            public interface IImportedService
            {
                Task<string> LongOp(string input, CancellationToken ct);
            }
            """;

        var (_, result) = RunGenerator(source);

        var tsFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsFile);

        var text = tsFile!.GetText().ToString();
        Assert.Contains("signal?: AbortSignal", text);
    }

    // ==================== TypeScript for JsImport with IAsyncEnumerable ====================

    [Fact]
    public void TypeScript_JsImport_with_IAsyncEnumerable_maps_to_AsyncIterable()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            [JsImport]
            public interface IStreamImport
            {
                IAsyncEnumerable<int> GetNumbers();
            }
            """;

        var (_, result) = RunGenerator(source);

        var tsFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsFile);

        var text = tsFile!.GetText().ToString();
        Assert.Contains("AsyncIterable<number>", text);
    }

    [Fact]
    public void TypeScript_byte_array_maps_to_Uint8Array()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IBlobService
            {
                Task<byte[]> Echo(byte[] payload);
            }
            """;

        var (_, result) = RunGenerator(source);
        var tsFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsFile);

        var text = tsFile!.GetText().ToString();
        Assert.Contains("echo(payload: Uint8Array): Promise<Uint8Array>", text);
    }

    [Fact]
    public void JsExport_binary_return_method_stub_decodes_base64_to_uint8array()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IBlobService
            {
                Task<byte[]> Load();
            }
            """;

        var (_, result) = RunGenerator(source);
        var regFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BlobServiceBridgeRegistration"));
        Assert.NotNull(regFile);

        var text = regFile!.GetText().ToString();
        Assert.Contains("_decodeBinaryResult", text);
        Assert.Contains("then(function(__r)", text);
    }

    // ==================== InvokeAsync CT overload on RPC service ====================

    [Fact]
    public void IWebViewRpcService_has_InvokeAsync_with_CancellationToken()
    {
        var methods = typeof(IWebViewRpcService).GetMethods()
            .Where(m => m.Name == "InvokeAsync")
            .ToList();

        var ctOverloads = methods.Where(m =>
            m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
            .ToList();

        Assert.True(ctOverloads.Count >= 2, "Expected at least 2 CT overloads (generic and non-generic)");
    }
}
