namespace MinimalHybrid.Bridge;

public class AppService : IAppService
{
    private static readonly UserProfile CurrentUser = new("Jane Developer", "jane@agibuild.com", "admin");

    private static readonly List<Item> SampleItems =
    [
        new("1", "Avalonia UI", "Cross-platform .NET UI framework"),
        new("2", "Agibuild Fulora", "Hybrid WebView bridge for Avalonia"),
        new("3", "WebView2", "Chromium-based WebView for Windows"),
        new("4", "WKWebView", "WebKit-based WebView for macOS/iOS"),
        new("5", "Vue.js", "Progressive JavaScript framework"),
        new("6", "React", "Library for building user interfaces"),
        new("7", "Vite", "Next-generation frontend build tool"),
        new("8", "JSON-RPC", "Lightweight remote procedure call protocol"),
    ];

    public Task<UserProfile> GetCurrentUser() =>
        Task.FromResult(CurrentUser);

    public Task SaveSettings(AppSettings settings)
    {
        // In a real app, persist to disk. This sample just acknowledges.
        return Task.CompletedTask;
    }

    public Task<List<Item>> SearchItems(string query, int limit)
    {
        var results = SampleItems
            .Where(i => i.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || i.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit > 0 ? limit : 5)
            .ToList();

        return Task.FromResult(results);
    }
}
