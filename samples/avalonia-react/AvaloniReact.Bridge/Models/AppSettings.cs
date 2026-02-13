namespace AvaloniReact.Bridge.Models;

/// <summary>User-configurable application settings.</summary>
public record AppSettings
{
    public string Theme { get; init; } = "system";
    public string Language { get; init; } = "en";
    public int FontSize { get; init; } = 14;
    public bool SidebarCollapsed { get; init; } = false;
}
