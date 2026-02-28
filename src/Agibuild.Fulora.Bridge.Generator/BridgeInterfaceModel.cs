using System.Collections.Immutable;
using System.Linq;

namespace Agibuild.Fulora.Bridge.Generator;

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
    public ImmutableArray<BridgeDiagnosticInfo> ValidationErrors { get; init; } = ImmutableArray<BridgeDiagnosticInfo>.Empty;
    public bool IsValid => ValidationErrors.IsDefaultOrEmpty;
}

/// <summary>
/// Serializable diagnostic info that can safely be cached by the incremental generator pipeline.
/// </summary>
internal sealed record BridgeDiagnosticInfo(string DiagnosticId, string Arg0, string Arg1 = "", string Arg2 = "");

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
    public bool HasCancellationToken => Parameters.Any(p => p.IsCancellationToken);
    public bool IsAsyncEnumerable { get; init; }
    public string? AsyncEnumerableInnerType { get; init; }
    public int VisibleParameterCount => Parameters.Count(p => !p.IsCancellationToken);
    public bool IsOverload { get; init; }
}

internal sealed record BridgeParameterModel
{
    public string Name { get; init; } = "";
    public string CamelCaseName { get; init; } = "";
    public string TypeFullName { get; init; } = "";
    public bool IsNullable { get; init; }
    public bool HasDefaultValue { get; init; }
    public string? DefaultValueLiteral { get; init; }
    public bool IsCancellationToken { get; init; }
}
