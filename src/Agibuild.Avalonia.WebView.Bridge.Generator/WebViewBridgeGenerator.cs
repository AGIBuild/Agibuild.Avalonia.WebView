using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Agibuild.Avalonia.WebView.Bridge.Generator;

/// <summary>
/// Roslyn Incremental Source Generator for [JsExport] and [JsImport] bridge interfaces.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class WebViewBridgeGenerator : IIncrementalGenerator
{
    private const string JsExportFullName = "Agibuild.Avalonia.WebView.JsExportAttribute";
    private const string JsImportFullName = "Agibuild.Avalonia.WebView.JsImportAttribute";

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
            var source = BridgeHostEmitter.Emit(model);
            spc.AddSource($"{model.ServiceName}BridgeRegistration.g.cs", source);
        });

        // Emit BridgeProxy for each [JsImport]
        context.RegisterSourceOutput(imports, static (spc, model) =>
        {
            var source = BridgeProxyEmitter.Emit(model);
            spc.AddSource($"{model.ServiceName}BridgeProxy.g.cs", source);
        });

        // Emit shared JsonOptions + TypeScript declarations (once per compilation)
        var allModels = exports.Collect().Combine(imports.Collect());
        context.RegisterSourceOutput(allModels, static (spc, combined) =>
        {
            var (exportList, importList) = combined;
            if (exportList.Length == 0 && importList.Length == 0) return;

            // Use the first namespace we find, or global
            var ns = exportList.Length > 0 ? exportList[0].Namespace
                   : importList.Length > 0 ? importList[0].Namespace
                   : "";

            // Shared JSON options
            var jsonSource = JsonOptionsEmitter.Emit(ns);
            spc.AddSource("BridgeGeneratedJsonOptions.g.cs", jsonSource);

            // TypeScript declarations as embedded string constants
            var tsSource = TypeScriptEmitter.EmitDeclarations(exportList, importList);
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
}
