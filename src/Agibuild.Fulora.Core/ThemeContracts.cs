namespace Agibuild.Fulora;

/// <summary>
/// Snapshot of the current OS theme state.
/// </summary>
public sealed class ThemeInfo
{
    /// <summary>
    /// Theme mode: <c>"light"</c>, <c>"dark"</c>, or <c>"system"</c>.
    /// Maps to CSS <c>prefers-color-scheme</c> values.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// OS accent color as hex <c>"#RRGGBB"</c>, or <c>null</c> on unsupported platforms.
    /// </summary>
    public string? AccentColor { get; init; }

    /// <summary>
    /// Whether the OS is in high-contrast mode.
    /// </summary>
    public bool IsHighContrast { get; init; }
}

/// <summary>
/// Event payload pushed to JS when the OS theme changes.
/// </summary>
public sealed class ThemeChangedEvent
{
    /// <summary>
    /// The new theme state after the change.
    /// </summary>
    public required ThemeInfo CurrentTheme { get; init; }

    /// <summary>
    /// The mode before the change (<c>"light"</c>, <c>"dark"</c>, or <c>"system"</c>).
    /// </summary>
    public required string PreviousMode { get; init; }
}
