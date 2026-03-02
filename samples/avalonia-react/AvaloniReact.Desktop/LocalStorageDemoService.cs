using System.Text.Json;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

/// <summary>
/// Sample implementation demonstrating local storage.
/// In production, use the <c>Agibuild.Fulora.Plugin.LocalStorage</c> NuGet package:
/// <code>
/// bridge.UsePlugin&lt;LocalStoragePlugin&gt;();
/// </code>
/// </summary>
public sealed class LocalStorageDemoService : ILocalStorageDemoService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private Dictionary<string, string> _store;

    public LocalStorageDemoService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AvaloniReact",
            "local-storage.json");
        _store = LoadFromDisk();
    }

    public Task<string?> Get(string key)
    {
        lock (_lock) return Task.FromResult(_store.GetValueOrDefault(key));
    }

    public Task Set(string key, string value)
    {
        lock (_lock) { _store[key] = value; SaveToDisk(); }
        return Task.CompletedTask;
    }

    public Task Remove(string key)
    {
        lock (_lock) { _store.Remove(key); SaveToDisk(); }
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        lock (_lock) { _store.Clear(); SaveToDisk(); }
        return Task.CompletedTask;
    }

    public Task<string[]> GetKeys()
    {
        lock (_lock) return Task.FromResult(_store.Keys.ToArray());
    }

    private Dictionary<string, string> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(_filePath)) ?? new();
        }
        catch { return new(); }
    }

    private void SaveToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath,
                JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort */ }
    }
}
