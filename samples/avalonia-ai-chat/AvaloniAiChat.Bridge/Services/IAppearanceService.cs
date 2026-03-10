using Agibuild.Fulora;

namespace AvaloniAiChat.Bridge.Services;

public sealed class AppearanceSettings
{
    public string ThemePreference { get; init; } = "system";
    public bool EnableTransparency { get; init; } = true;
    public int GlassOpacityPercent { get; init; } = 78;
}

public sealed class AppearanceCapabilities
{
    public string Platform { get; init; } = "unknown";
    public bool SupportsTransparency { get; init; }
    public bool IsTransparencyEnabled { get; init; }
    public bool IsTransparencyEffective { get; init; }
    public string EffectiveTransparencyLevel { get; init; } = "unknown";
    public string ValidationMessage { get; init; } = "";
}

public sealed class AppearanceState
{
    public AppearanceSettings Settings { get; init; } = new();
    public string EffectiveThemeMode { get; init; } = "liquid";
    public AppearanceCapabilities Capabilities { get; init; } = new();
}

[JsExport]
public interface IAppearanceService
{
    Task<AppearanceState> GetAppearanceState();
    Task<AppearanceState> UpdateAppearanceSettings(AppearanceSettings settings);
    IAsyncEnumerable<AppearanceState> StreamAppearanceState(CancellationToken cancellationToken = default);
}
