using AvaloniVue.Bridge.Models;
using AvaloniVue.Bridge.Services;

namespace AvaloniVue.Tests;

public class AppShellServiceTests
{
    [Fact]
    public async Task GetPages_returns_default_four_pages()
    {
        var service = new AppShellService();
        var pages = await service.GetPages();

        Assert.Equal(4, pages.Count);
        Assert.Contains(pages, p => p.Id == "dashboard");
        Assert.Contains(pages, p => p.Id == "chat");
        Assert.Contains(pages, p => p.Id == "files");
        Assert.Contains(pages, p => p.Id == "settings");
    }

    [Fact]
    public async Task GetPages_returns_custom_pages_when_configured()
    {
        var custom = new List<PageDefinition>
        {
            new("home", "Home", "Home", "/"),
        };
        var service = new AppShellService(custom, new AppInfo("Test", "1.0", "desc"));

        var pages = await service.GetPages();
        Assert.Single(pages);
        Assert.Equal("home", pages[0].Id);
    }

    [Fact]
    public async Task GetAppInfo_returns_application_metadata()
    {
        var service = new AppShellService();
        var info = await service.GetAppInfo();

        Assert.NotNull(info.Name);
        Assert.NotNull(info.Version);
        Assert.NotNull(info.Description);
        Assert.NotEmpty(info.Name);
    }

    [Fact]
    public async Task GetPages_each_page_has_all_fields()
    {
        var service = new AppShellService();
        var pages = await service.GetPages();

        foreach (var page in pages)
        {
            Assert.NotEmpty(page.Id);
            Assert.NotEmpty(page.Title);
            Assert.NotEmpty(page.Icon);
            Assert.NotEmpty(page.Route);
            Assert.StartsWith("/", page.Route);
        }
    }
}
