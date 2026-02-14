namespace AvaloniReact.Bridge.Models;

/// <summary>Defines a page available in the app shell navigation.</summary>
public record PageDefinition(
    string Id,
    string Title,
    string Icon,
    string Route);
