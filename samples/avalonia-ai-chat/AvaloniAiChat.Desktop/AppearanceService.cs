using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Avalonia;
using Avalonia.Controls;
using AvaloniAiChat.Bridge.Services;
using System.Runtime.CompilerServices;

namespace AvaloniAiChat.Desktop;

/// <summary>
/// Sample appearance service that wraps the framework <see cref="IWindowShellService"/>
/// and provides the sample-specific <see cref="IAppearanceService"/> contract.
/// </summary>
public sealed class AppearanceService : IAppearanceService, IDisposable
{
    private readonly IWindowShellService _shellService;
    private readonly IDisposable? _shellServiceDisposable;

    public AppearanceService(IWindowShellService shellService)
    {
        _shellService = shellService ?? throw new ArgumentNullException(nameof(shellService));
        _shellServiceDisposable = shellService as IDisposable;
    }

    public async Task<AppearanceState> GetAppearanceState()
    {
        var state = await _shellService.GetWindowShellState();
        return ToAppearanceState(state);
    }

    public async Task<AppearanceState> UpdateAppearanceSettings(AppearanceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var shellSettings = new WindowShellSettings
        {
            ThemePreference = settings.ThemePreference,
            EnableTransparency = settings.EnableTransparency,
            GlassOpacityPercent = settings.GlassOpacityPercent
        };
        var state = await _shellService.UpdateWindowShellSettings(shellSettings);
        return ToAppearanceState(state);
    }

    public async IAsyncEnumerable<AppearanceState> StreamAppearanceState(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var state in _shellService.StreamWindowShellState(cancellationToken))
        {
            yield return ToAppearanceState(state);
        }
    }

    private static AppearanceState ToAppearanceState(WindowShellState state)
    {
        var levelStr = state.Capabilities.EffectiveTransparencyLevel.ToString().ToLowerInvariant();
        return new AppearanceState
        {
            Settings = new AppearanceSettings
            {
                ThemePreference = state.Settings.ThemePreference,
                EnableTransparency = state.Settings.EnableTransparency,
                GlassOpacityPercent = state.Settings.GlassOpacityPercent
            },
            EffectiveThemeMode = state.EffectiveThemeMode,
            Capabilities = new AppearanceCapabilities
            {
                Platform = state.Capabilities.Platform,
                SupportsTransparency = state.Capabilities.SupportsTransparency,
                IsTransparencyEnabled = state.Capabilities.IsTransparencyEnabled,
                IsTransparencyEffective = state.Capabilities.IsTransparencyEffective,
                EffectiveTransparencyLevel = levelStr,
                ValidationMessage = state.Capabilities.ValidationMessage
            }
        };
    }

    public void Dispose()
    {
        _shellServiceDisposable?.Dispose();
    }
}
