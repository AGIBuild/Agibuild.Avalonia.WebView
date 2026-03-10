using System;
using System.Threading.Tasks;

namespace Agibuild.Fulora;

/// <summary>
/// Request payload for applying window appearance changes.
/// </summary>
public sealed class WindowAppearanceRequest
{
    public bool EnableTransparency { get; init; }
    public int OpacityPercent { get; init; }
    public string EffectiveThemeMode { get; init; } = "liquid";
}

/// <summary>
/// Effective transparency state as resolved by the platform provider.
/// </summary>
public sealed class TransparencyEffectiveState
{
    public bool IsEnabled { get; init; }
    public bool IsEffective { get; init; }
    public TransparencyLevel Level { get; init; }
    public int AppliedOpacityPercent { get; init; }
    public string? ValidationMessage { get; init; }
}

/// <summary>
/// Abstracts platform-specific window chrome operations (transparency, metrics, appearance).
/// Concrete implementations (e.g. Avalonia) add window tracking on the concrete type.
/// </summary>
public interface IWindowChromeProvider
{
    string Platform { get; }
    bool SupportsTransparency { get; }

    Task ApplyWindowAppearanceAsync(WindowAppearanceRequest request);
    TransparencyEffectiveState GetTransparencyState();
    WindowChromeMetrics GetChromeMetrics();

    event EventHandler? AppearanceChanged;
}
