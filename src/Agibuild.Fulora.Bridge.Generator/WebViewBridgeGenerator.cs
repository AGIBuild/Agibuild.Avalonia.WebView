using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agibuild.Fulora.Bridge.Generator;

/// <summary>
/// Roslyn Incremental Source Generator for [JsExport] and [JsImport] bridge interfaces.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class WebViewBridgeGenerator : IIncrementalGenerator
{
    private const string JsExportFullName = "Agibuild.Fulora.JsExportAttribute";
    private const string JsImportFullName = "Agibuild.Fulora.JsImportAttribute";

    /// <summary>
    /// Configures incremental pipelines for JsExport and JsImport bridge source generation.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [JsExport] interfaces
        var exports = context.SyntaxProvider.ForAttributeWithMetadataName(
            JsExportFullName,
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (ctx, _) => ExtractModel(ctx, BridgeDirection.Export)
        ).Where(static m => m is not null).Select(static (m, _) => m!);

        // [JsImport] interfaces
        var imports = context.SyntaxProvider.ForAttributeWithMetadataName(
            JsImportFullName,
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (ctx, _) => ExtractModel(ctx, BridgeDirection.Import)
        ).Where(static m => m is not null).Select(static (m, _) => m!);

        // Emit BridgeRegistration for each [JsExport]
        context.RegisterSourceOutput(exports, static (spc, model) =>
        {
            ReportDiagnostics(spc, model);
            if (!model.IsValid) return;

            var source = BridgeHostEmitter.Emit(model);
            spc.AddSource($"{model.ServiceName}BridgeRegistration.g.cs", source);
        });

        // Emit BridgeProxy for each [JsImport]
        context.RegisterSourceOutput(imports, static (spc, model) =>
        {
            ReportDiagnostics(spc, model);
            if (!model.IsValid) return;

            var source = BridgeProxyEmitter.Emit(model);
            spc.AddSource($"{model.ServiceName}BridgeProxy.g.cs", source);
        });

        // Emit shared JsonOptions + TypeScript declarations (once per compilation)
        var allModels = exports.Collect().Combine(imports.Collect());
        context.RegisterSourceOutput(allModels, static (spc, combined) =>
        {
            var (exportList, importList) = combined;

            var validExports = exportList.Where(m => m.IsValid).ToImmutableArray();
            var validImports = importList.Where(m => m.IsValid).ToImmutableArray();

            if (validExports.Length == 0 && validImports.Length == 0) return;

            var ns = validExports.Length > 0 ? validExports[0].Namespace
                   : validImports.Length > 0 ? validImports[0].Namespace
                   : "";

            var jsonSource = JsonOptionsEmitter.Emit(ns);
            spc.AddSource("BridgeGeneratedJsonOptions.g.cs", jsonSource);

            var tsSource = TypeScriptEmitter.EmitDeclarations(validExports, validImports);
            spc.AddSource("BridgeTypeScriptDeclarations.g.cs", tsSource);
        });
    }

    private static BridgeInterfaceModel? ExtractModel(GeneratorAttributeSyntaxContext ctx, BridgeDirection direction)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
            return null;

        var attribute = ctx.Attributes.FirstOrDefault();
        if (attribute is null) return null;

        return ModelExtractor.Extract(interfaceSymbol, attribute, direction);
    }

    private static void ReportDiagnostics(SourceProductionContext spc, BridgeInterfaceModel model)
    {
        if (model.IsValid) return;

        foreach (var info in model.ValidationErrors)
        {
            var descriptor = info.DiagnosticId switch
            {
                "AGBR001" => BridgeDiagnostics.GenericMethodNotSupported,
                "AGBR002" => BridgeDiagnostics.OverloadNotSupported,
                "AGBR003" => BridgeDiagnostics.RefOutInNotSupported,
                "AGBR004" => BridgeDiagnostics.CancellationTokenNotSupported,
                "AGBR005" => BridgeDiagnostics.AsyncEnumerableNotSupported,
                "AGBR006" => BridgeDiagnostics.OpenGenericInterfaceNotSupported,
                "AGBR007" => BridgeDiagnostics.BridgeEventOnImportNotSupported,
                _ => null,
            };

            if (descriptor is null) continue;

            spc.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                Location.None,
                info.Arg0, info.Arg1, info.Arg2));
        }
    }
}
