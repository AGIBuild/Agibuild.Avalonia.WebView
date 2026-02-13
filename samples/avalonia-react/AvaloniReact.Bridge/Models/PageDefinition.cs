using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>Defines a page available in the app shell navigation.</summary>
public record PageDefinition(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("route")] string Route);
