using Agibuild.Fulora;
using AvaloniVue.Bridge.Models;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Provides native file system access through the Bridge.
/// Demonstrates file I/O capabilities inaccessible to web content.
/// </summary>
[JsExport]
public interface IFileService
{
    Task<List<FileEntry>> ListFiles(string? path = null);
    Task<string> ReadTextFile(string path);
    Task<string> GetUserDocumentsPath();
}
