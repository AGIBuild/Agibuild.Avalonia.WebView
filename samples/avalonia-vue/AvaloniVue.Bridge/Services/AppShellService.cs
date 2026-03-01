using AvaloniVue.Bridge.Models;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Configurable page registry and app metadata provider.
/// Pages are injected at construction — adding a page requires only updating the list.
/// </summary>
public class AppShellService : IAppShellService
{
    private readonly List<PageDefinition> _pages;
    private readonly AppInfo _appInfo;

    public AppShellService()
        : this(DefaultPages(), DefaultAppInfo())
    {
    }

    public AppShellService(List<PageDefinition> pages, AppInfo appInfo)
    {
        _pages = pages;
        _appInfo = appInfo;
    }

    public Task<List<PageDefinition>> GetPages() => Task.FromResult(_pages);

    public Task<AppInfo> GetAppInfo() => Task.FromResult(_appInfo);

    private static List<PageDefinition> DefaultPages() =>
    [
        new("dashboard", "Dashboard", "LayoutDashboard", "/dashboard"),
        new("chat", "Chat", "MessageSquare", "/chat"),
        new("files", "Files", "FolderOpen", "/files"),
        new("settings", "Settings", "Settings", "/settings"),
    ];

    private static AppInfo DefaultAppInfo() =>
        new("Avalonia + Vue Demo", "1.0.0", "Production-grade hybrid app sample powered by Agibuild.Fulora");
}
