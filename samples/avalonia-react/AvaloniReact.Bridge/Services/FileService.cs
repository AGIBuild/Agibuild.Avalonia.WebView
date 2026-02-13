using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Provides native file system access through the Bridge.
/// Demonstrates reading directories and files — capabilities unavailable in web sandboxes.
/// </summary>
public class FileService : IFileService
{
    public Task<List<FileEntry>> ListFiles(string? path = null)
    {
        var targetPath = path ?? GetUserDocumentsPathSync();

        if (!Directory.Exists(targetPath))
            return Task.FromResult(new List<FileEntry>());

        var entries = new List<FileEntry>();

        foreach (var dir in Directory.GetDirectories(targetPath))
        {
            var info = new DirectoryInfo(dir);
            entries.Add(new FileEntry(
                info.Name,
                info.FullName,
                IsDirectory: true,
                Size: 0,
                LastModified: info.LastWriteTimeUtc));
        }

        foreach (var file in Directory.GetFiles(targetPath))
        {
            var info = new FileInfo(file);
            entries.Add(new FileEntry(
                info.Name,
                info.FullName,
                IsDirectory: false,
                Size: info.Length,
                LastModified: info.LastWriteTimeUtc));
        }

        return Task.FromResult(entries.OrderByDescending(e => e.IsDirectory).ThenBy(e => e.Name).ToList());
    }

    public async Task<string> ReadTextFile(string path)
    {
        if (!File.Exists(path))
            return $"Error: File not found — {path}";

        try
        {
            // Limit read size for safety
            var info = new FileInfo(path);
            if (info.Length > 1024 * 1024) // 1 MB limit
                return $"Error: File too large ({info.Length / 1024} KB). Max 1 MB for preview.";

            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public Task<string> GetUserDocumentsPath() =>
        Task.FromResult(GetUserDocumentsPathSync());

    private static string GetUserDocumentsPathSync() =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}
