namespace AvaloniVue.Bridge.Models;

/// <summary>Represents a file or directory entry.</summary>
public record FileEntry(
    string Name,
    string Path,
    bool IsDirectory,
    long Size,
    DateTime LastModified);
