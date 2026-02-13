using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>User-configurable application settings.</summary>
public record AppSettings
{
    [JsonPropertyName("theme")]
    public string Theme { get; init; } = "system";

    [JsonPropertyName("language")]
    public string Language { get; init; } = "en";

    [JsonPropertyName("fontSize")]
    public int FontSize { get; init; } = 14;

    [JsonPropertyName("sidebarCollapsed")]
    public bool SidebarCollapsed { get; init; } = false;
}
