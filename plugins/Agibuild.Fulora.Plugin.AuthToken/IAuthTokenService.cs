using Agibuild.Fulora;

namespace Agibuild.Fulora.Plugin.AuthToken;

/// <summary>
/// Bridge service for secure token storage with expiry and scope metadata.
/// </summary>
[JsExport]
public interface IAuthTokenService
{
    /// <summary>Retrieves a stored token by key. Returns null if not found or expired.</summary>
    Task<string?> GetToken(string key);

    /// <summary>Stores a token with optional expiry and scope metadata.</summary>
    Task SetToken(string key, string value, TokenOptions? options = null);

    /// <summary>Removes a stored token and its metadata.</summary>
    Task RemoveToken(string key);

    /// <summary>Returns all stored token keys.</summary>
    Task<string[]> ListKeys();
}
