using Agibuild.Fulora;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniAiChat.Bridge.Services;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AvaloniAiChat.Desktop;

public sealed class AppearanceService : IAppearanceService, IWindowShellService, IDisposable
{
    private const double DefaultDragRegionHeight = 44d;

    private readonly Window _window;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _appearanceStateSignal = new(0, 256);
    private bool _disposed;
    private AppearanceSettings _settings;

    public AppearanceService(Window window)
    {
        _window = window;
        _settings = new AppearanceSettings();
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _ = ApplySettingsToWindowAsync();
    }

    public Task<AppearanceState> GetAppearanceState()
        => Task.FromResult(BuildAppearanceStateSnapshot());

    public Task<WindowShellState> GetWindowShellState()
        => Task.FromResult(BuildWindowShellStateSnapshot());

    public async Task<AppearanceState> UpdateAppearanceSettings(AppearanceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
            _settings = Normalize(settings);

        await ApplySettingsToWindowAsync();
        NotifyAppearanceStateChanged();
        return BuildAppearanceStateSnapshot();
    }

    public async Task<WindowShellState> UpdateWindowShellSettings(WindowShellSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
            _settings = Normalize(ToAppearanceSettings(settings));

        await ApplySettingsToWindowAsync();
        NotifyAppearanceStateChanged();
        return BuildWindowShellStateSnapshot();
    }

    public async IAsyncEnumerable<AppearanceState> StreamAppearanceState(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = BuildAppearanceStateSnapshot();
        var lastSignature = BuildAppearanceSignature(current);
        yield return current;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _ = await _appearanceStateSignal.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            current = BuildAppearanceStateSnapshot();
            var signature = BuildAppearanceSignature(current);
            if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
            {
                lastSignature = signature;
                yield return current;
            }
        }
    }

    public async IAsyncEnumerable<WindowShellState> StreamWindowShellState(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = BuildWindowShellStateSnapshot();
        var lastSignature = BuildWindowShellSignature(current);
        yield return current;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _ = await _appearanceStateSignal.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            current = BuildWindowShellStateSnapshot();
            var signature = BuildWindowShellSignature(current);
            if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
            {
                lastSignature = signature;
                yield return current;
            }
        }
    }

    private AppearanceState BuildAppearanceStateSnapshot()
    {
        AppearanceSettings snapshot;
        lock (_gate)
            snapshot = _settings;

        var effectiveTheme = ResolveEffectiveTheme(snapshot.ThemePreference);
        var effectiveTransparencyLevel = TryGetActualTransparencyLevel();
        var isTransparencyEnabled = snapshot.EnableTransparency;
        var supportsTransparency = IsEffectiveTransparency(effectiveTransparencyLevel);
        var isTransparencyEffective = isTransparencyEnabled && supportsTransparency;
        var platform = DetectPlatform();
        var message = BuildTransparencyValidationMessage(
            isTransparencyEnabled,
            supportsTransparency,
            effectiveTransparencyLevel);

        return new AppearanceState
        {
            Settings = snapshot,
            EffectiveThemeMode = effectiveTheme,
            Capabilities = new AppearanceCapabilities
            {
                Platform = platform,
                SupportsTransparency = supportsTransparency,
                IsTransparencyEnabled = isTransparencyEnabled,
                IsTransparencyEffective = isTransparencyEffective,
                EffectiveTransparencyLevel = effectiveTransparencyLevel,
                ValidationMessage = message
            }
        };
    }

    private async Task ApplySettingsToWindowAsync()
    {
        AppearanceSettings snapshot;
        lock (_gate)
            snapshot = _settings;

        var effectiveTheme = ResolveEffectiveTheme(snapshot.ThemePreference);
        var pct = snapshot.GlassOpacityPercent / 100d;
        var windowAlpha = (byte)Math.Clamp((int)(30 + pct * 210), 30, 240);
        var tintedBackground = effectiveTheme == "liquid"
            ? new SolidColorBrush(Color.FromArgb(windowAlpha, 9, 18, 35))
            : new SolidColorBrush(Color.FromArgb(windowAlpha, 248, 250, 252));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // 始终扩展客户区以获得沉浸式体验，不随透明度开关而改变
            _window.ExtendClientAreaToDecorationsHint = true;
            _window.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.PreferSystemChrome;

            if (snapshot.EnableTransparency)
            {
                _window.TransparencyLevelHint = BuildTransparencyLevelHint();
                _window.Background = tintedBackground;
            }
            else
            {
                _window.TransparencyLevelHint = [WindowTransparencyLevel.None];
                _window.Background = effectiveTheme == "liquid"
                    ? new SolidColorBrush(Color.FromRgb(5, 9, 20))
                    : new SolidColorBrush(Color.FromRgb(248, 250, 252));
            }
        });
    }

    private WindowShellState BuildWindowShellStateSnapshot()
    {
        AppearanceSettings snapshot;
        lock (_gate)
            snapshot = _settings;

        var effectiveTheme = ResolveEffectiveTheme(snapshot.ThemePreference);
        var effectiveTransparencyLevel = TryGetActualTransparencyLevel();
        var isTransparencyEnabled = snapshot.EnableTransparency;
        var supportsTransparency = IsEffectiveTransparency(effectiveTransparencyLevel);
        var isTransparencyEffective = isTransparencyEnabled && supportsTransparency;
        var platform = DetectPlatform();
        var message = BuildTransparencyValidationMessage(
            isTransparencyEnabled,
            supportsTransparency,
            effectiveTransparencyLevel);

        return new WindowShellState
        {
            Settings = ToWindowShellSettings(snapshot),
            EffectiveThemeMode = effectiveTheme,
            Capabilities = new WindowShellCapabilities
            {
                Platform = platform,
                SupportsTransparency = supportsTransparency,
                IsTransparencyEnabled = isTransparencyEnabled,
                IsTransparencyEffective = isTransparencyEffective,
                EffectiveTransparencyLevel = effectiveTransparencyLevel,
                ValidationMessage = message,
                AppliedOpacityPercent = snapshot.GlassOpacityPercent
            },
            ChromeMetrics = BuildWindowChromeMetricsSnapshot()
        };
    }

    private static AppearanceSettings ToAppearanceSettings(WindowShellSettings settings)
        => new()
        {
            ThemePreference = settings.ThemePreference,
            EnableTransparency = settings.EnableTransparency,
            GlassOpacityPercent = settings.GlassOpacityPercent
        };

    private static WindowShellSettings ToWindowShellSettings(AppearanceSettings settings)
        => new()
        {
            ThemePreference = settings.ThemePreference,
            EnableTransparency = settings.EnableTransparency,
            GlassOpacityPercent = settings.GlassOpacityPercent
        };

    private static WindowChromeMetrics BuildWindowChromeMetricsSnapshot()
        => new()
        {
            TitleBarHeight = DefaultDragRegionHeight,
            DragRegionHeight = DefaultDragRegionHeight,
            SafeInsets = new WindowSafeInsets()
        };

    private static AppearanceSettings Normalize(AppearanceSettings settings)
    {
        var preference = settings.ThemePreference?.Trim().ToLowerInvariant() switch
        {
            "liquid" => "liquid",
            "classic" => "classic",
            _ => "system"
        };

        var opacity = Math.Clamp(settings.GlassOpacityPercent, 20, 95);

        return new AppearanceSettings
        {
            ThemePreference = preference,
            EnableTransparency = settings.EnableTransparency,
            GlassOpacityPercent = opacity
        };
    }

    private static string ResolveEffectiveTheme(string? themePreference)
    {
        var normalized = themePreference?.Trim().ToLowerInvariant();
        if (normalized == "liquid")
            return "liquid";
        if (normalized == "classic")
            return "classic";

        var variant = Application.Current?.ActualThemeVariant?.ToString() ?? string.Empty;
        return variant.Contains("dark", StringComparison.OrdinalIgnoreCase) ? "liquid" : "classic";
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        bool followSystem;
        lock (_gate)
            followSystem = string.Equals(_settings.ThemePreference, "system", StringComparison.OrdinalIgnoreCase);

        if (!followSystem)
            return;

        _ = ApplySettingsToWindowAsync();
        NotifyAppearanceStateChanged();
    }

    private string TryGetActualTransparencyLevel()
    {
        var property = _window.GetType().GetProperty("TransparencyLevel")
            ?? _window.GetType().GetProperty("ActualTransparencyLevel");
        var value = property?.GetValue(_window);
        return NormalizeTransparencyLevel(value?.ToString());
    }

    private static bool IsEffectiveTransparency(string level)
        => level is "blur" or "transparent" or "acrylicblur" or "mica";

    private static WindowTransparencyLevel[] BuildTransparencyLevelHint()
    {
        string[] preferenceOrder =
            OperatingSystem.IsWindows()
                ? ["Mica", "AcrylicBlur", "Blur", "Transparent"]
                : OperatingSystem.IsMacOS()
                    ? ["Blur", "Transparent"]
                    : ["AcrylicBlur", "Blur", "Transparent"];

        var result = new List<WindowTransparencyLevel>(4);
        foreach (var name in preferenceOrder)
        {
            if (!TryResolveTransparencyLevelByName(name, out var level))
                continue;
            if (level == WindowTransparencyLevel.None || result.Contains(level))
                continue;
            result.Add(level);
        }

        if (result.Count == 0)
        {
            result.Add(WindowTransparencyLevel.Blur);
            result.Add(WindowTransparencyLevel.Transparent);
        }

        return [.. result];
    }

    private static bool TryResolveTransparencyLevelByName(string memberName, out WindowTransparencyLevel level)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;
        var type = typeof(WindowTransparencyLevel);

        if (type.GetProperty(memberName, Flags)?.GetValue(null) is WindowTransparencyLevel propertyValue)
        {
            level = propertyValue;
            return true;
        }

        if (type.GetField(memberName, Flags)?.GetValue(null) is WindowTransparencyLevel fieldValue)
        {
            level = fieldValue;
            return true;
        }

        level = WindowTransparencyLevel.None;
        return false;
    }

    private static string NormalizeTransparencyLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "unknown";

        foreach (var token in raw.Split([',', '|', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = token.Trim().ToLowerInvariant();
            if (normalized.Contains("acrylic", StringComparison.Ordinal))
                return "acrylicblur";
            if (normalized.Contains("mica", StringComparison.Ordinal))
                return "mica";
            if (normalized.Contains("blur", StringComparison.Ordinal))
                return "blur";
            if (normalized.Contains("transparent", StringComparison.Ordinal) || normalized.Contains("translucent", StringComparison.Ordinal))
                return "transparent";
            if (normalized == "none")
                return "none";
        }

        return "unknown";
    }

    private static string DetectPlatform()
    {
        if (OperatingSystem.IsMacOS())
            return "macOS";
        if (OperatingSystem.IsWindows())
            return "Windows";
        if (OperatingSystem.IsLinux())
            return "Linux";
        return "Unknown";
    }

    private static string BuildTransparencyValidationMessage(
        bool enabled,
        bool supportsTransparency,
        string effectiveLevel)
    {
        if (!enabled)
            return "Transparency is disabled in appearance settings.";
        if (supportsTransparency && IsEffectiveTransparency(effectiveLevel))
            return $"Transparency is active. Effective level: {effectiveLevel}.";
        if (string.Equals(effectiveLevel, "none", StringComparison.OrdinalIgnoreCase))
            return "Platform reported 'none' after applying transparency hint.";
        return "Transparency validation is inconclusive on this platform runtime.";
    }

    private static string BuildAppearanceSignature(AppearanceState state)
        => $"{state.Settings.ThemePreference}|{state.Settings.EnableTransparency}|{state.Settings.GlassOpacityPercent}|{state.EffectiveThemeMode}|{state.Capabilities.EffectiveTransparencyLevel}|{state.Capabilities.IsTransparencyEffective}";

    private static string BuildWindowShellSignature(WindowShellState state)
        => $"{state.Settings.ThemePreference}|{state.Settings.EnableTransparency}|{state.Settings.GlassOpacityPercent}|{state.EffectiveThemeMode}|{state.Capabilities.EffectiveTransparencyLevel}|{state.Capabilities.IsTransparencyEffective}|{state.ChromeMetrics.TitleBarHeight}|{state.ChromeMetrics.DragRegionHeight}|{state.ChromeMetrics.SafeInsets.Top}|{state.ChromeMetrics.SafeInsets.Right}|{state.ChromeMetrics.SafeInsets.Bottom}|{state.ChromeMetrics.SafeInsets.Left}";

    private void NotifyAppearanceStateChanged()
    {
        try
        {
            _appearanceStateSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Burst updates can exceed buffer; latest state is still available via GetAppearanceState().
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (Application.Current is { } app)
            app.ActualThemeVariantChanged -= OnActualThemeVariantChanged;

        _appearanceStateSignal.Dispose();
    }
}
