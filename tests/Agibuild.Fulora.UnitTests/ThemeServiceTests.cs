using System;
using System.Collections.Generic;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ThemeServiceTests
{
    // ─── IPlatformThemeProvider Contract Tests ──────────────────────────────

    [Fact]
    public void FallbackProvider_ReturnsSystemMode()
    {
        var provider = new FallbackThemeProvider();
        Assert.Equal("system", provider.GetThemeMode());
    }

    [Fact]
    public void FallbackProvider_ReturnsNullAccentColor()
    {
        var provider = new FallbackThemeProvider();
        Assert.Null(provider.GetAccentColor());
    }

    [Fact]
    public void FallbackProvider_ReturnsNotHighContrast()
    {
        var provider = new FallbackThemeProvider();
        Assert.False(provider.GetIsHighContrast());
    }

    // ─── ThemeService Contract Tests ────────────────────────────────────────

    [Fact]
    public async Task GetCurrentTheme_ReturnsDarkMode_WhenProviderReturnsDark()
    {
        var mock = new MockThemeProvider { Mode = "dark" };
        using var service = new ThemeService(mock);

        var theme = await service.GetCurrentTheme();

        Assert.Equal("dark", theme.Mode);
    }

    [Fact]
    public async Task GetCurrentTheme_ReturnsLightMode_WhenProviderReturnsLight()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        using var service = new ThemeService(mock);

        var theme = await service.GetCurrentTheme();

        Assert.Equal("light", theme.Mode);
    }

    [Fact]
    public async Task GetCurrentTheme_IncludesAccentColor()
    {
        var mock = new MockThemeProvider { Mode = "dark", AccentColor = "#007AFF" };
        using var service = new ThemeService(mock);

        var theme = await service.GetCurrentTheme();

        Assert.Equal("#007AFF", theme.AccentColor);
    }

    [Fact]
    public async Task GetCurrentTheme_IncludesHighContrast()
    {
        var mock = new MockThemeProvider { Mode = "light", HighContrast = true };
        using var service = new ThemeService(mock);

        var theme = await service.GetCurrentTheme();

        Assert.True(theme.IsHighContrast);
    }

    [Fact]
    public async Task GetAccentColor_ReturnsNull_WhenUnsupported()
    {
        var mock = new MockThemeProvider { Mode = "light", AccentColor = null };
        using var service = new ThemeService(mock);

        var accent = await service.GetAccentColor();

        Assert.Null(accent);
    }

    [Fact]
    public async Task GetAccentColor_ReturnsHexColor()
    {
        var mock = new MockThemeProvider { Mode = "dark", AccentColor = "#FF5257" };
        using var service = new ThemeService(mock);

        var accent = await service.GetAccentColor();

        Assert.Equal("#FF5257", accent);
    }

    [Fact]
    public async Task GetHighContrastMode_ReturnsFalse_WhenNotHighContrast()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        using var service = new ThemeService(mock);

        Assert.False(await service.GetHighContrastMode());
    }

    [Fact]
    public async Task GetHighContrastMode_ReturnsTrue_WhenHighContrast()
    {
        var mock = new MockThemeProvider { Mode = "light", HighContrast = true };
        using var service = new ThemeService(mock);

        Assert.True(await service.GetHighContrastMode());
    }

    // ─── Event Deduplication Tests ──────────────────────────────────────────

    [Fact]
    public void ThemeChanged_FiresEvent_WhenModeChanges()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        using var service = new ThemeService(mock);

        var events = new List<ThemeChangedEvent>();
        ((BridgeEvent<ThemeChangedEvent>)service.ThemeChanged).Connect(e => events.Add(e));

        mock.Mode = "dark";
        mock.RaiseThemeChanged();

        Assert.Single(events);
        Assert.Equal("dark", events[0].CurrentTheme.Mode);
        Assert.Equal("light", events[0].PreviousMode);
    }

    [Fact]
    public void ThemeChanged_DoesNotFire_WhenModeIsSame()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        using var service = new ThemeService(mock);

        var events = new List<ThemeChangedEvent>();
        ((BridgeEvent<ThemeChangedEvent>)service.ThemeChanged).Connect(e => events.Add(e));

        // Fire without changing mode — deduplication should suppress
        mock.RaiseThemeChanged();

        Assert.Empty(events);
    }

    [Fact]
    public void ThemeChanged_FiresMultiple_WhenModeChangesMultipleTimes()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        using var service = new ThemeService(mock);

        var events = new List<ThemeChangedEvent>();
        ((BridgeEvent<ThemeChangedEvent>)service.ThemeChanged).Connect(e => events.Add(e));

        mock.Mode = "dark";
        mock.RaiseThemeChanged();
        mock.Mode = "light";
        mock.RaiseThemeChanged();

        Assert.Equal(2, events.Count);
        Assert.Equal("dark", events[0].CurrentTheme.Mode);
        Assert.Equal("light", events[0].PreviousMode);
        Assert.Equal("light", events[1].CurrentTheme.Mode);
        Assert.Equal("dark", events[1].PreviousMode);
    }

    [Fact]
    public void ThemeChanged_DoesNotFire_AfterDispose()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        var service = new ThemeService(mock);

        var events = new List<ThemeChangedEvent>();
        ((BridgeEvent<ThemeChangedEvent>)service.ThemeChanged).Connect(e => events.Add(e));

        service.Dispose();

        mock.Mode = "dark";
        mock.RaiseThemeChanged();

        Assert.Empty(events);
    }

    [Fact]
    public void ThemeChanged_IncludesAccentColor_InEvent()
    {
        var mock = new MockThemeProvider { Mode = "light", AccentColor = "#007AFF" };
        using var service = new ThemeService(mock);

        var events = new List<ThemeChangedEvent>();
        ((BridgeEvent<ThemeChangedEvent>)service.ThemeChanged).Connect(e => events.Add(e));

        mock.Mode = "dark";
        mock.AccentColor = "#FF5257";
        mock.RaiseThemeChanged();

        Assert.Single(events);
        Assert.Equal("#FF5257", events[0].CurrentTheme.AccentColor);
    }

    // ─── Constructor / Dispose Tests ────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsOnNullProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new ThemeService(null!));
    }

    [Fact]
    public void Dispose_DoubleDispose_DoesNotThrow()
    {
        var mock = new MockThemeProvider { Mode = "light" };
        var service = new ThemeService(mock);
        service.Dispose();
        service.Dispose();
    }

    // ─── TypeScript declaration generation contract test ────────────────────

    [Fact]
    public void IThemeService_HasJsExportAttribute()
    {
        var attr = typeof(IThemeService).GetCustomAttributes(typeof(JsExportAttribute), false);
        Assert.Single(attr);
    }

    [Fact]
    public void ThemeInfo_HasExpectedProperties()
    {
        var props = typeof(ThemeInfo).GetProperties();
        Assert.Contains(props, p => p.Name == "Mode" && p.PropertyType == typeof(string));
        Assert.Contains(props, p => p.Name == "AccentColor" && p.PropertyType == typeof(string));
        Assert.Contains(props, p => p.Name == "IsHighContrast" && p.PropertyType == typeof(bool));
    }

    [Fact]
    public void ThemeChangedEvent_HasExpectedProperties()
    {
        var props = typeof(ThemeChangedEvent).GetProperties();
        Assert.Contains(props, p => p.Name == "CurrentTheme" && p.PropertyType == typeof(ThemeInfo));
        Assert.Contains(props, p => p.Name == "PreviousMode" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void IThemeService_HasThemeChangedEvent()
    {
        var prop = typeof(IThemeService).GetProperty("ThemeChanged");
        Assert.NotNull(prop);
        Assert.Equal(typeof(IBridgeEvent<ThemeChangedEvent>), prop!.PropertyType);
    }

    // ─── Mock Provider ──────────────────────────────────────────────────────

    private sealed class MockThemeProvider : IPlatformThemeProvider
    {
        public string Mode { get; set; } = "system";
        public string? AccentColor { get; set; }
        public bool HighContrast { get; set; }

        public event EventHandler? ThemeChanged;

        public string GetThemeMode() => Mode;
        public string? GetAccentColor() => AccentColor;
        public bool GetIsHighContrast() => HighContrast;

        public void RaiseThemeChanged() => ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
