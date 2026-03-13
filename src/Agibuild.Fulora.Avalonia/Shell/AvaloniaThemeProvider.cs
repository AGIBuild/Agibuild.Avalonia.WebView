using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Styling;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Avalonia-based <see cref="IPlatformThemeProvider"/> that detects dark/light mode
/// via <see cref="Application.ActualThemeVariant"/> and dispatches change events.
/// Accent color and high-contrast are delegated to platform-specific detection.
/// </summary>
public sealed class AvaloniaThemeProvider : IPlatformThemeProvider, IDisposable
{
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="AvaloniaThemeProvider"/> class.</summary>
    public AvaloniaThemeProvider()
    {
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnThemeVariantChanged;
    }

    /// <inheritdoc />
    public event EventHandler? ThemeChanged;

    /// <inheritdoc />
    public string GetThemeMode()
    {
        var variant = Application.Current?.ActualThemeVariant;
        if (variant == ThemeVariant.Dark) return "dark";
        if (variant == ThemeVariant.Light) return "light";
        return "system";
    }

    /// <inheritdoc />
    public string? GetAccentColor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsAccentColor();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacOSAccentColor();
        return null;
    }

    /// <inheritdoc />
    public bool GetIsHighContrast()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsHighContrast();
        return false;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        if (!_disposed)
            ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Application.Current is { } app)
            app.ActualThemeVariantChanged -= OnThemeVariantChanged;
    }

    // ─── Platform-specific accent color ────────────────────────────────────

    private static string? GetWindowsAccentColor()
    {
        try
        {
            return WindowsAccentColorReader.Read();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetMacOSAccentColor()
    {
        try
        {
            return MacOSAccentColorReader.Read();
        }
        catch
        {
            return null;
        }
    }

    private static bool GetWindowsHighContrast()
    {
        try
        {
            return WindowsHighContrastReader.IsEnabled();
        }
        catch
        {
            return false;
        }
    }
}
