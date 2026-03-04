namespace Agibuild.Fulora.Plugin.AuthToken;

/// <summary>
/// Optional metadata for stored tokens (expiry, scope).
/// </summary>
public sealed class TokenOptions
{
    /// <summary>Optional expiry time for the token. Expired tokens are removed on Get.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Optional scope identifier for the token.</summary>
    public string? Scope { get; init; }
}
