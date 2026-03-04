using Agibuild.Fulora;

namespace Agibuild.Fulora.Plugin.FileSystem;

/// <summary>
/// Bridge service for sandboxed file system access.
/// All paths are resolved relative to a configurable root directory with path traversal prevention.
/// </summary>
[JsExport]
public interface IFileSystemService
{
    /// <summary>Reads text content from the specified path.</summary>
    Task<string> ReadText(string path);
    /// <summary>Writes text content to the specified path.</summary>
    Task WriteText(string path, string content);
    /// <summary>Reads binary content from the specified path.</summary>
    Task<byte[]> ReadBinary(string path);
    /// <summary>Writes binary data to the specified path.</summary>
    Task WriteBinary(string path, byte[] data);
    /// <summary>Lists file and directory entries at the specified path.</summary>
    Task<FileEntry[]> List(string path);
    /// <summary>Deletes the file or directory at the specified path.</summary>
    Task Delete(string path);
    /// <summary>Returns whether the file or directory at the specified path exists.</summary>
    Task<bool> Exists(string path);
    /// <summary>Creates a directory at the specified path.</summary>
    Task CreateDirectory(string path);
}
