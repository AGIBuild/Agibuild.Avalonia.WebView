using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>Application metadata exposed to the frontend.</summary>
public record AppInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string Description);
