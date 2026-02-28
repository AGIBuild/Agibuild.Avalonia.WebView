using System.Collections.Immutable;
using Agibuild.Fulora.Bridge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Tests that the bridge source generator reports correct diagnostics for V1-unsupported patterns.
/// </summary>
public sealed class BridgeDiagnosticsTests
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

        // Add reference to System.Runtime for basic types.
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

    [Fact]
    public void Generic_method_reports_AGBR001()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IGenericService
            {
                Task<T> GetItem<T>(string id);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR001");
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains("GenericServiceBridgeRegistration"));
    }

    [Fact]
    public void Same_param_count_overloads_report_AGBR002()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IOverloadService
            {
                Task Search(string query);
                Task Search(int id);
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR002");
    }

    [Fact]
    public void Different_param_count_overloads_are_allowed()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IOverloadService
            {
                Task Search(string query);
                Task Search(string query, int limit);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR002");
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("OverloadServiceBridgeRegistration"));
    }

    [Fact]
    public void Ref_parameter_reports_AGBR003()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IRefService
            {
                Task Process(ref string data);
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR003" && d.GetMessage().Contains("ref"));
    }

    [Fact]
    public void Out_parameter_reports_AGBR003()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsImport]
            public interface IOutService
            {
                Task<bool> TryParse(string input, out int result);
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR003" && d.GetMessage().Contains("out"));
    }

    [Fact]
    public void CancellationToken_parameter_is_now_allowed()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsExport]
            public interface ICancellableService
            {
                Task<string> Process(string input, CancellationToken ct);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR004");
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("CancellableServiceBridgeRegistration"));
    }

    [Fact]
    public void IAsyncEnumerable_return_is_now_allowed()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Collections.Generic;

            [JsExport]
            public interface IStreamService
            {
                IAsyncEnumerable<int> StreamData();
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR005");
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("StreamServiceBridgeRegistration"));
    }

    [Fact]
    public void Valid_interface_emits_code_while_invalid_does_not()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IInvalidService
            {
                Task<T> GetItem<T>(string id);
            }

            [JsExport]
            public interface IValidService
            {
                Task<string> GetName();
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR001");
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("ValidServiceBridgeRegistration"));
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains("InvalidServiceBridgeRegistration"));
    }

    [Fact]
    public void Non_generic_method_emits_no_diagnostics()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface INormalService
            {
                Task<string> GetItem(string id);
                Task SaveItem(string id, string value);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("AGBR")));
        Assert.Contains(result.GeneratedTrees, t => t.FilePath.Contains("NormalServiceBridgeRegistration"));
    }
}
