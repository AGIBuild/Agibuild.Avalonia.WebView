using System.Collections.Immutable;

namespace Agibuild.Fulora.Bridge.Generator;

/// <summary>
/// Root of the Bridge Contract IR. Contains all services and the complete DTO type graph.
/// All bridge artifact emitters should consume this model as their single source of truth.
/// </summary>
internal sealed record BridgeContractModel
{
    public ImmutableArray<BridgeInterfaceModel> Services { get; init; } = ImmutableArray<BridgeInterfaceModel>.Empty;
    public ImmutableArray<BridgeDtoModel> Dtos { get; init; } = ImmutableArray<BridgeDtoModel>.Empty;
}

/// <summary>
/// Structured type reference in the Bridge Contract IR.
/// Replaces string-based type names with a richly typed model for deterministic multi-target emission.
/// </summary>
internal sealed record BridgeTypeRef
{
    public static readonly BridgeTypeRef UnknownRef = new() { Kind = BridgeTypeKind.Unknown, FullName = "object", Name = "object" };

    public BridgeTypeKind Kind { get; init; }

    /// <summary>Full CLR type name (e.g. "System.Int32", "MyNamespace.PageDefinition").</summary>
    public string FullName { get; init; } = "";

    /// <summary>Short type name without namespace.</summary>
    public string Name { get; init; } = "";

    /// <summary>Element type for Array, AsyncEnumerable; inner type for Nullable.</summary>
    public BridgeTypeRef? ElementType { get; init; }

    /// <summary>Type arguments for Dictionary (key, value) or BridgeEvent (payload).</summary>
    public ImmutableArray<BridgeTypeRef> TypeArguments { get; init; } = ImmutableArray<BridgeTypeRef>.Empty;

    public bool IsNullable { get; init; }
}

internal enum BridgeTypeKind
{
    String,
    Number,
    Boolean,
    Void,
    Binary,
    DateTime,
    Guid,
    Array,
    Dictionary,
    Dto,
    Enum,
    BridgeEvent,
    AsyncEnumerable,
    Unknown,
}

/// <summary>
/// DTO type definition discovered from bridge service contracts.
/// </summary>
internal sealed record BridgeDtoModel
{
    public string FullName { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsEnum { get; init; }
    public ImmutableArray<BridgeDtoPropertyModel> Properties { get; init; } = ImmutableArray<BridgeDtoPropertyModel>.Empty;
    public ImmutableArray<BridgeEnumMemberModel> EnumMembers { get; init; } = ImmutableArray<BridgeEnumMemberModel>.Empty;
}

/// <summary>
/// Property within a DTO type.
/// </summary>
internal sealed record BridgeDtoPropertyModel
{
    public string Name { get; init; } = "";
    public string CamelCaseName { get; init; } = "";
    public BridgeTypeRef TypeRef { get; init; } = BridgeTypeRef.UnknownRef;
    public bool IsNullable { get; init; }
}

/// <summary>
/// Member of an enum type used in bridge contracts.
/// </summary>
internal sealed record BridgeEnumMemberModel
{
    public string Name { get; init; } = "";
    public string CamelCaseName { get; init; } = "";
    public string ValueLiteral { get; init; } = "0";
}
