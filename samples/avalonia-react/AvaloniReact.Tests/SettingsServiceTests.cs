using AvaloniReact.Bridge.Models;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Tests;

public class SettingsServiceTests
{
    private SettingsService CreateService()
    {
        // Use temp file to avoid polluting real settings
        var tmpFile = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}.json");
        return new SettingsService(tmpFile);
    }

    [Fact]
    public async Task GetSettings_returns_defaults()
    {
        var service = CreateService();
        var settings = await service.GetSettings();

        Assert.Equal("system", settings.Theme);
        Assert.Equal("en", settings.Language);
        Assert.Equal(14, settings.FontSize);
        Assert.False(settings.SidebarCollapsed);
    }

    [Fact]
    public async Task UpdateSettings_persists_and_returns_updated()
    {
        var service = CreateService();
        var updated = new AppSettings
        {
            Theme = "dark",
            Language = "zh",
            FontSize = 16,
            SidebarCollapsed = true,
        };

        var result = await service.UpdateSettings(updated);

        Assert.Equal("dark", result.Theme);
        Assert.Equal("zh", result.Language);
        Assert.Equal(16, result.FontSize);
        Assert.True(result.SidebarCollapsed);
    }

    [Fact]
    public async Task UpdateSettings_survives_read_after_write()
    {
        var service = CreateService();
        var updated = new AppSettings { Theme = "dark" };

        await service.UpdateSettings(updated);
        var current = await service.GetSettings();

        Assert.Equal("dark", current.Theme);
    }
}
