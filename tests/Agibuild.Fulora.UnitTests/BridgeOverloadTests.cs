using System.Collections.Immutable;
using System.Linq;
using Agibuild.Fulora.Bridge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BridgeOverloadTests
{
    private static (ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result) RunGenerator(string source)
    {
        var coreAssembly = typeof(JsExportAttribute).Assembly;
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
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

        return (diagnostics, driver.GetRunResult());
    }

    // --- AGBR006: Open Generic Interface ---

    [Fact]
    public void Open_generic_interface_reports_AGBR006()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IRepository<T>
            {
                Task<string> Get(string id);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "AGBR006");
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains("RepositoryBridgeRegistration"));
    }

    [Fact]
    public void Non_generic_interface_does_not_report_AGBR006()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IUserService
            {
                Task<string> GetUser(string id);
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR006");
    }

    // --- AGBR001: Improved message ---

    [Fact]
    public void AGBR001_message_suggests_alternatives()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IGenService
            {
                Task<T> GetItem<T>(string id);
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        var diag = diagnostics.FirstOrDefault(d => d.Id == "AGBR001");
        Assert.NotNull(diag);
        Assert.Contains("concrete method", diag.GetMessage());
    }

    // --- Overload RPC Naming ---

    [Fact]
    public void Overloaded_methods_get_unique_RPC_names()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface ISearchService
            {
                Task<string> Search(string query);
                Task<string> Search(string query, int limit);
                Task<string> Search(string query, int limit, int offset);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR002");
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("SearchServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("SearchService.search\"", generatedCode);
        Assert.Contains("SearchService.search$2\"", generatedCode);
        Assert.Contains("SearchService.search$3\"", generatedCode);
    }

    [Fact]
    public void Fewest_param_overload_keeps_original_RPC_name()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IFetchService
            {
                Task<string> Fetch();
                Task<string> Fetch(string url);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR002");
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("FetchServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("FetchService.fetch\"", generatedCode);
        Assert.Contains("FetchService.fetch$1\"", generatedCode);
    }

    [Fact]
    public void CancellationToken_excluded_from_overload_param_count()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading;
            using System.Threading.Tasks;

            [JsExport]
            public interface ICancelOverloadService
            {
                Task Process(string input, CancellationToken ct);
                Task Process(string input, int count);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR002");
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("CancelOverloadServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("CancelOverloadService.process\"", generatedCode);
        Assert.Contains("CancelOverloadService.process$2\"", generatedCode);
    }

    [Fact]
    public void Non_overloaded_methods_keep_original_names()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface ISimpleService
            {
                Task<string> GetItem(string id);
                Task SaveItem(string id, string value);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("AGBR"));
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("SimpleServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("SimpleService.getItem\"", generatedCode);
        Assert.Contains("SimpleService.saveItem\"", generatedCode);
        Assert.DoesNotContain("$", generatedCode);
    }

    // --- TypeScript Overloaded Signatures ---

    [Fact]
    public void TypeScript_emits_overloaded_signatures()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface ISearchService
            {
                Task<string> Search(string query);
                Task<string> Search(string query, int limit);
            }
            """;

        var (_, result) = RunGenerator(source);
        var tsTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("BridgeTypeScriptDeclarations"));
        Assert.NotNull(tsTree);

        var tsCode = tsTree.GetText().ToString();
        var searchOccurrences = tsCode.Split("search(").Length - 1;
        Assert.True(searchOccurrences >= 4, $"Expected at least 4 'search(' occurrences (2 in per-service + 2 in All), got {searchOccurrences}");
    }

    // --- JS Stub Dispatcher ---

    [Fact]
    public void JS_stub_generates_arguments_length_dispatcher_for_overloads()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IDispatchService
            {
                Task<string> Do();
                Task<string> Do(string input);
            }
            """;

        var (_, result) = RunGenerator(source);
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("DispatchServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("arguments.length", generatedCode);
        Assert.Contains("DispatchService.do", generatedCode);
        Assert.Contains("DispatchService.do$1", generatedCode);
    }

    [Fact]
    public void Non_overloaded_JS_stub_does_not_use_arguments_length()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IPlainService
            {
                Task<string> GetItem(string id);
            }
            """;

        var (_, result) = RunGenerator(source);
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("PlainServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.DoesNotContain("arguments.length", generatedCode);
    }

    // --- Edge Cases ---

    [Fact]
    public void Three_overloads_with_distinct_counts_all_generate()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IMultiOverload
            {
                Task<string> Query();
                Task<string> Query(string filter);
                Task<string> Query(string filter, int page, int size);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "AGBR002");
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MultiOverloadBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("MultiOverload.query\"", generatedCode);
        Assert.Contains("MultiOverload.query$1\"", generatedCode);
        Assert.Contains("MultiOverload.query$3\"", generatedCode);
    }

    [Fact]
    public void Mixed_overloaded_and_non_overloaded_methods()
    {
        var source = """
            using Agibuild.Fulora;
            using System.Threading.Tasks;

            [JsExport]
            public interface IMixedService
            {
                Task<string> GetById(string id);
                Task<string> Search(string query);
                Task<string> Search(string query, int limit);
            }
            """;

        var (diagnostics, result) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("AGBR"));
        var registrationTree = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MixedServiceBridgeRegistration"));
        Assert.NotNull(registrationTree);

        var generatedCode = registrationTree.GetText().ToString();
        Assert.Contains("MixedService.getById\"", generatedCode);
        Assert.Contains("MixedService.search\"", generatedCode);
        Assert.Contains("MixedService.search$2\"", generatedCode);
    }
}
