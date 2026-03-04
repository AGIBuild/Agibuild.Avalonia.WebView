namespace Agibuild.Fulora.Plugin.FileSystem;

/// <summary>
/// Represents a file or directory entry returned by <see cref="IFileSystemService.List"/>.
/// </summary>
public sealed class FileEntry
{
    /// <summary>File or directory name.</summary>
    public string Name { get; init; } = "";
    /// <summary>Relative path within the sandbox root.</summary>
    public string Path { get; init; } = "";
    /// <summary>Whether this entry is a directory.</summary>
    public bool IsDirectory { get; init; }
    /// <summary>File size in bytes; 0 for directories.</summary>
    public long Size { get; init; }
    /// <summary>Last modification time in UTC.</summary>
    public DateTimeOffset LastModified { get; init; }
}
