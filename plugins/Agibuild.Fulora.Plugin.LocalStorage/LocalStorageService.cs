using System.Text.Json;

namespace Agibuild.Fulora.Plugin.LocalStorage;

/// <summary>
/// JSON file-backed implementation of <see cref="ILocalStorageService"/>.
/// Stores data in a single JSON file in the application data directory.
/// Thread-safe via a lock on all read/write operations.
/// </summary>
public sealed class LocalStorageService : ILocalStorageService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private Dictionary<string, string> _store;

    /// <summary>
    /// Creates a new local storage service.
    /// </summary>
    /// <param name="storagePath">
    /// Full path to the JSON storage file. When null, defaults to
    /// <c>{AppData}/Fulora/local-storage.json</c>.
    /// </param>
    public LocalStorageService(string? storagePath = null)
    {
        _filePath = storagePath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Fulora",
                "local-storage.json");

        _store = LoadFromDisk();
    }

    public Task<string?> Get(string key)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.GetValueOrDefault(key));
        }
    }

    public Task Set(string key, string value)
    {
        lock (_lock)
        {
            _store[key] = value;
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    public Task Remove(string key)
    {
        lock (_lock)
        {
            _store.Remove(key);
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        lock (_lock)
        {
            _store.Clear();
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    public Task<string[]> GetKeys()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Keys.ToArray());
        }
    }

    private Dictionary<string, string> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, string>();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best-effort persistence — log in production
        }
    }
}
