namespace Agibuild.Fulora.Plugin.AuthToken;

/// <summary>
/// Abstraction for platform-secure storage. Implementations may use Keychain,
/// Credential Manager, Keystore, or Secret Service per platform.
/// </summary>
public interface ISecureStorageProvider
{
    /// <summary>Retrieves a stored value by key.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Stores a key-value pair.</summary>
    Task SetAsync(string key, string value);

    /// <summary>Removes a key-value pair.</summary>
    Task RemoveAsync(string key);

    /// <summary>Returns all stored keys.</summary>
    Task<string[]> ListKeysAsync();
}
