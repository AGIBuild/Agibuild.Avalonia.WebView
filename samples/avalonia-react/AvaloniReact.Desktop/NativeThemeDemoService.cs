using System;
using System.Threading.Tasks;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

/// <summary>
/// Desktop implementation of <see cref="INativeThemeDemoService"/> using
/// <see cref="AvaloniaThemeProvider"/> for native OS theme detection.
/// </summary>
internal sealed class NativeThemeDemoService : INativeThemeDemoService, IDisposable
{
    private readonly IPlatformThemeProvider _provider;
    private readonly BridgeEvent<NativeThemeChangeEvent> _onThemeChanged = new();
    private string _lastMode;

    public NativeThemeDemoService(IPlatformThemeProvider provider)
    {
        _provider = provider;
        _lastMode = _provider.GetThemeMode();
        _provider.ThemeChanged += OnProviderThemeChanged;
    }

    public IBridgeEvent<NativeThemeChangeEvent> OnThemeChanged => _onThemeChanged;

    public Task<NativeThemeSnapshot> GetCurrentTheme()
        => Task.FromResult(BuildSnapshot());

    public Task<string?> GetAccentColor()
        => Task.FromResult(_provider.GetAccentColor());

    public Task<bool> GetHighContrastMode()
        => Task.FromResult(_provider.GetIsHighContrast());

    private void OnProviderThemeChanged(object? sender, EventArgs e)
    {
        var newMode = _provider.GetThemeMode();
        var previousMode = _lastMode;
        if (string.Equals(newMode, previousMode, StringComparison.Ordinal))
            return;
        _lastMode = newMode;
        _onThemeChanged.Emit(new NativeThemeChangeEvent
        {
            CurrentTheme = BuildSnapshot(),
            PreviousMode = previousMode
        });
    }

    private NativeThemeSnapshot BuildSnapshot() => new()
    {
        Mode = _provider.GetThemeMode(),
        AccentColor = _provider.GetAccentColor(),
        IsHighContrast = _provider.GetIsHighContrast()
    };

    public void Dispose()
    {
        _provider.ThemeChanged -= OnProviderThemeChanged;
    }
}
