namespace Agibuild.Fulora;

/// <summary>
/// Marks an assembly as validated for NativeAOT compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AotCompatibilityAttribute : Attribute
{
    /// <summary>Gets whether the assembly is AOT compatible.</summary>
    public bool IsAotCompatible { get; }

    /// <summary>Initializes a new instance with the specified AOT compatibility flag.</summary>
    public AotCompatibilityAttribute(bool isAotCompatible = true) => IsAotCompatible = isAotCompatible;
}
