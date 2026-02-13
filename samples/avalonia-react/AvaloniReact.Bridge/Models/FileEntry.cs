using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>Represents a file or directory entry.</summary>
public record FileEntry(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("isDirectory")] bool IsDirectory,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("lastModified")] DateTime LastModified);
