using System.Text.Json;

namespace Agibuild.Fulora.Plugin.AuthToken;

/// <summary>
/// Implementation of <see cref="IAuthTokenService"/> that delegates to
/// <see cref="ISecureStorageProvider"/> for storage and tracks token metadata
/// (expiry, scope). Performs lazy cleanup of expired tokens on Get.
/// </summary>
public sealed class AuthTokenService : IAuthTokenService
{
    private const string MetaPrefix = "__meta:";
    private readonly ISecureStorageProvider _storage;

    /// <summary>Initializes a new instance with the specified storage provider.</summary>
    /// <param name="storage">The secure storage provider for persisting tokens.</param>
    public AuthTokenService(ISecureStorageProvider storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    /// <summary>Retrieves a stored token by key. Returns null if not found or expired.</summary>
    public async Task<string?> GetToken(string key)
    {
        var value = await _storage.GetAsync(key);
        if (value is null)
            return null;

        var meta = await GetMetadataAsync(key);
        if (meta?.ExpiresAt is { } expiresAt && DateTimeOffset.UtcNow >= expiresAt)
        {
            await RemoveToken(key);
            return null;
        }

        return value;
    }

    /// <summary>Stores a token with optional expiry and scope metadata.</summary>
    public async Task SetToken(string key, string value, TokenOptions? options = null)
    {
        await _storage.SetAsync(key, value);
        await SetMetadataAsync(key, options);
    }

    /// <summary>Removes a stored token and its metadata.</summary>
    public async Task RemoveToken(string key)
    {
        await _storage.RemoveAsync(key);
        await _storage.RemoveAsync(MetaPrefix + key);
    }

    /// <summary>Returns all stored token keys (excluding metadata keys).</summary>
    public async Task<string[]> ListKeys()
    {
        var keys = await _storage.ListKeysAsync();
        return keys
            .Where(k => !k.StartsWith(MetaPrefix, StringComparison.Ordinal))
            .ToArray();
    }

    private async Task<TokenMetadata?> GetMetadataAsync(string key)
    {
        var json = await _storage.GetAsync(MetaPrefix + key);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TokenMetadata>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task SetMetadataAsync(string key, TokenOptions? options)
    {
        if (options is null || (options.ExpiresAt is null && options.Scope is null))
        {
            await _storage.RemoveAsync(MetaPrefix + key);
            return;
        }

        var meta = new TokenMetadata
        {
            ExpiresAt = options.ExpiresAt,
            Scope = options.Scope
        };
        var json = JsonSerializer.Serialize(meta);
        await _storage.SetAsync(MetaPrefix + key, json);
    }

    private sealed class TokenMetadata
    {
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? Scope { get; set; }
    }
}
