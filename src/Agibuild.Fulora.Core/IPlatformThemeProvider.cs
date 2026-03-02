using System;

namespace Agibuild.Fulora;

/// <summary>
/// Abstracts OS-specific theme detection. Implement per platform for accent color,
/// high-contrast detection, and theme change notifications.
/// </summary>
public interface IPlatformThemeProvider
{
    /// <summary>
    /// Returns the current theme mode: <c>"light"</c>, <c>"dark"</c>, or <c>"system"</c>.
    /// </summary>
    string GetThemeMode();

    /// <summary>
    /// Returns the OS accent color as hex <c>"#RRGGBB"</c>, or <c>null</c> on unsupported platforms.
    /// </summary>
    string? GetAccentColor();

    /// <summary>
    /// Returns <c>true</c> if the OS is in high-contrast mode.
    /// </summary>
    bool GetIsHighContrast();

    /// <summary>
    /// Raised when the OS theme changes. Subscribers should re-query theme state.
    /// </summary>
    event EventHandler? ThemeChanged;
}
