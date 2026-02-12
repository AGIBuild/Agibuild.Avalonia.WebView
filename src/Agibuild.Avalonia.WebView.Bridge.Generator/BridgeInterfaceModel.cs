using System.Collections.Immutable;

namespace Agibuild.Avalonia.WebView.Bridge.Generator;

/// <summary>
/// Immutable model extracted from semantic analysis of a [JsExport] or [JsImport] interface.
/// Used by all emitters.
/// </summary>
internal sealed record BridgeInterfaceModel
{
    public string Namespace { get; init; } = "";
    public string InterfaceFullName { get; init; } = "";
    public string InterfaceName { get; init; } = "";
    public string ServiceName { get; init; } = "";
    public BridgeDirection Direction { get; init; }
    public ImmutableArray<BridgeMethodModel> Methods { get; init; } = ImmutableArray<BridgeMethodModel>.Empty;
}

internal enum BridgeDirection
{
    Export, // [JsExport] — C# → JS
    Import, // [JsImport] — JS → C#
}

internal sealed record BridgeMethodModel
{
    public string Name { get; init; } = "";
    public string CamelCaseName { get; init; } = "";
    public string RpcMethodName { get; init; } = "";
    public string ReturnTypeFullName { get; init; } = "";
    public bool IsAsync { get; init; }
    public bool HasReturnValue { get; init; }
    public string? InnerReturnTypeFullName { get; init; }
    public ImmutableArray<BridgeParameterModel> Parameters { get; init; } = ImmutableArray<BridgeParameterModel>.Empty;
}

internal sealed record BridgeParameterModel
{
    public string Name { get; init; } = "";
    public string CamelCaseName { get; init; } = "";
    public string TypeFullName { get; init; } = "";
    public bool IsNullable { get; init; }
    public bool HasDefaultValue { get; init; }
    public string? DefaultValueLiteral { get; init; }
}
