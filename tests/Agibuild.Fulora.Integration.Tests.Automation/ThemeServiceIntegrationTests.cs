using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

public sealed class ThemeServiceIntegrationTests
{
    [AvaloniaFact]
    public async Task ThemeService_GetCurrentTheme_ReturnsValidTheme()
    {
        var mockProvider = new MockThemeProvider("dark", "#007AFF", false);
        using var service = new ThemeService(mockProvider);

        var theme = await service.GetCurrentTheme();

        Assert.Equal("dark", theme.Mode);
        Assert.Equal("#007AFF", theme.AccentColor);
        Assert.False(theme.IsHighContrast);
    }

    [AvaloniaFact]
    public async Task ThemeService_GetAccentColor_ReturnsNullOnUnsupported()
    {
        var mockProvider = new MockThemeProvider("light", null, false);
        using var service = new ThemeService(mockProvider);

        var accent = await service.GetAccentColor();

        Assert.Null(accent);
    }

    [AvaloniaFact]
    public async Task ThemeService_GetHighContrastMode_ReturnsCorrectState()
    {
        var mockProvider = new MockThemeProvider("light", null, true);
        using var service = new ThemeService(mockProvider);

        var isHighContrast = await service.GetHighContrastMode();

        Assert.True(isHighContrast);
    }

    [AvaloniaFact]
    public void ThemeService_ThemeChange_PushesEventToSubscribers()
    {
        var mockProvider = new MockThemeProvider("light", "#007AFF", false);
        using var service = new ThemeService(mockProvider);

        var receivedEvents = new List<ThemeChangedEvent>();
        var bridgeEvent = (BridgeEvent<ThemeChangedEvent>)service.ThemeChanged;
        bridgeEvent.Connect(e => receivedEvents.Add(e));

        // Simulate OS theme change: light → dark
        mockProvider.Mode = "dark";
        mockProvider.AccentColor = "#FF5257";
        mockProvider.RaiseThemeChanged();

        Assert.Single(receivedEvents);
        Assert.Equal("dark", receivedEvents[0].CurrentTheme.Mode);
        Assert.Equal("light", receivedEvents[0].PreviousMode);
        Assert.Equal("#FF5257", receivedEvents[0].CurrentTheme.AccentColor);
    }

    [AvaloniaFact]
    public void ThemeService_DuplicateThemeChange_IsDeduped()
    {
        var mockProvider = new MockThemeProvider("dark", null, false);
        using var service = new ThemeService(mockProvider);

        var receivedEvents = new List<ThemeChangedEvent>();
        var bridgeEvent = (BridgeEvent<ThemeChangedEvent>)service.ThemeChanged;
        bridgeEvent.Connect(e => receivedEvents.Add(e));

        // Fire without changing mode — should be deduped
        mockProvider.RaiseThemeChanged();
        mockProvider.RaiseThemeChanged();

        Assert.Empty(receivedEvents);
    }

    [AvaloniaFact]
    public void ThemeService_MultipleChanges_FiresCorrectEvents()
    {
        var mockProvider = new MockThemeProvider("light", null, false);
        using var service = new ThemeService(mockProvider);

        var receivedEvents = new List<ThemeChangedEvent>();
        var bridgeEvent = (BridgeEvent<ThemeChangedEvent>)service.ThemeChanged;
        bridgeEvent.Connect(e => receivedEvents.Add(e));

        mockProvider.Mode = "dark";
        mockProvider.RaiseThemeChanged();

        mockProvider.Mode = "light";
        mockProvider.RaiseThemeChanged();

        Assert.Equal(2, receivedEvents.Count);
        Assert.Equal("dark", receivedEvents[0].CurrentTheme.Mode);
        Assert.Equal("light", receivedEvents[0].PreviousMode);
        Assert.Equal("light", receivedEvents[1].CurrentTheme.Mode);
        Assert.Equal("dark", receivedEvents[1].PreviousMode);
    }

    [AvaloniaFact]
    public void FallbackProvider_ReturnsSafeDefaults()
    {
        var fallback = new FallbackThemeProvider();

        Assert.Equal("system", fallback.GetThemeMode());
        Assert.Null(fallback.GetAccentColor());
        Assert.False(fallback.GetIsHighContrast());
    }

    [AvaloniaFact]
    public void ThemeService_Dispose_StopsEvents()
    {
        var mockProvider = new MockThemeProvider("light", null, false);
        var service = new ThemeService(mockProvider);

        var receivedEvents = new List<ThemeChangedEvent>();
        var bridgeEvent = (BridgeEvent<ThemeChangedEvent>)service.ThemeChanged;
        bridgeEvent.Connect(e => receivedEvents.Add(e));

        service.Dispose();

        mockProvider.Mode = "dark";
        mockProvider.RaiseThemeChanged();

        Assert.Empty(receivedEvents);
    }

    // ─── Mock ───────────────────────────────────────────────────────────────

    private sealed class MockThemeProvider : IPlatformThemeProvider
    {
        public MockThemeProvider(string mode, string? accentColor, bool isHighContrast)
        {
            Mode = mode;
            AccentColor = accentColor;
            IsHighContrast = isHighContrast;
        }

        public string Mode { get; set; }
        public string? AccentColor { get; set; }
        public bool IsHighContrast { get; set; }

        public event EventHandler? ThemeChanged;

        public string GetThemeMode() => Mode;
        public string? GetAccentColor() => AccentColor;
        public bool GetIsHighContrast() => IsHighContrast;

        public void RaiseThemeChanged() => ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
