namespace Agibuild.Fulora.Plugin.AuthToken;

/// <summary>
/// In-memory implementation of <see cref="ISecureStorageProvider"/> for testing
/// and default fallback when platform secure storage is unavailable.
/// </summary>
public sealed class InMemorySecureStorageProvider : ISecureStorageProvider
{
    private readonly Dictionary<string, string> _store = new();
    private readonly object _lock = new();

    /// <summary>Retrieves a stored value by key.</summary>
    public Task<string?> GetAsync(string key)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.GetValueOrDefault(key));
        }
    }

    /// <summary>Stores a key-value pair.</summary>
    public Task SetAsync(string key, string value)
    {
        lock (_lock)
        {
            _store[key] = value;
        }
        return Task.CompletedTask;
    }

    /// <summary>Removes a key-value pair.</summary>
    public Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            _store.Remove(key);
        }
        return Task.CompletedTask;
    }

    /// <summary>Returns all stored keys.</summary>
    public Task<string[]> ListKeysAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Keys.ToArray());
        }
    }
}
