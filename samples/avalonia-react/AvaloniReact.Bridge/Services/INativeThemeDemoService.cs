using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Demonstrates native OS theme detection via the Fulora bridge.
/// Exposes the framework's theme provider to JS as a [JsExport] service.
/// </summary>
[JsExport]
public interface INativeThemeDemoService
{
    Task<NativeThemeSnapshot> GetCurrentTheme();
    Task<string?> GetAccentColor();
    Task<bool> GetHighContrastMode();
    IBridgeEvent<NativeThemeChangeEvent> OnThemeChanged { get; }
}

public sealed class NativeThemeSnapshot
{
    public required string Mode { get; init; }
    public string? AccentColor { get; init; }
    public bool IsHighContrast { get; init; }
}

public sealed class NativeThemeChangeEvent
{
    public required NativeThemeSnapshot CurrentTheme { get; init; }
    public required string PreviousMode { get; init; }
}
