using System;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Fallback <see cref="IPlatformThemeProvider"/> that returns safe defaults.
/// Used on platforms where OS theme APIs are unavailable (e.g., headless, unsupported Linux variants).
/// </summary>
public sealed class FallbackThemeProvider : IPlatformThemeProvider
{
    public event EventHandler? ThemeChanged
    {
        add { }
        remove { }
    }

    public string GetThemeMode() => "system";
    public string? GetAccentColor() => null;
    public bool GetIsHighContrast() => false;
}
