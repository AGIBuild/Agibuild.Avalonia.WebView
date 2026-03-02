namespace Agibuild.Fulora;

/// <summary>
/// Provides OS theme information and real-time theme change notifications.
/// Registered via <c>Bridge.Expose&lt;IThemeService&gt;(impl)</c>.
/// </summary>
[JsExport]
public interface IThemeService
{
    /// <summary>
    /// Returns the current OS theme info (mode, accent color, high-contrast state).
    /// </summary>
    Task<ThemeInfo> GetCurrentTheme();

    /// <summary>
    /// Returns the OS accent color as a hex string (<c>"#RRGGBB"</c>), or <c>null</c> on unsupported platforms.
    /// </summary>
    Task<string?> GetAccentColor();

    /// <summary>
    /// Returns <c>true</c> if the OS is in high-contrast mode.
    /// </summary>
    Task<bool> GetHighContrastMode();

    /// <summary>
    /// Push event fired when the OS theme changes (dark/light, accent color, high-contrast).
    /// </summary>
    IBridgeEvent<ThemeChangedEvent> ThemeChanged { get; }
}
