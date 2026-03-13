using System;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Fallback <see cref="IPlatformThemeProvider"/> that returns safe defaults.
/// Used on platforms where OS theme APIs are unavailable (e.g., headless, unsupported Linux variants).
/// </summary>
public sealed class FallbackThemeProvider : IPlatformThemeProvider
{
    /// <inheritdoc />
    public event EventHandler? ThemeChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public string GetThemeMode() => "system";

    /// <inheritdoc />
    public string? GetAccentColor() => null;

    /// <inheritdoc />
    public bool GetIsHighContrast() => false;
}
