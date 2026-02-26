namespace Agibuild.Fulora;

/// <summary>
/// Helpers for matching web authentication callback URIs.
/// </summary>
public static class WebAuthCallbackMatcher
{
    /// <summary>
    /// Returns true when the URIs match exactly (scheme/host/port/path) using strict semantics.
    /// </summary>
    /// <param name="expectedCallbackUri">The expected callback URI configured for the auth flow.</param>
    /// <param name="actualUri">The actual navigated URI.</param>
    public static bool IsStrictMatch(Uri expectedCallbackUri, Uri actualUri)
    {
        ArgumentNullException.ThrowIfNull(expectedCallbackUri);
        ArgumentNullException.ThrowIfNull(actualUri);

        if (!expectedCallbackUri.IsAbsoluteUri || !actualUri.IsAbsoluteUri)
        {
            return false;
        }

        return string.Equals(expectedCallbackUri.Scheme, actualUri.Scheme, StringComparison.OrdinalIgnoreCase)
               && string.Equals(expectedCallbackUri.Host, actualUri.Host, StringComparison.OrdinalIgnoreCase)
               && expectedCallbackUri.Port == actualUri.Port
               && string.Equals(expectedCallbackUri.AbsolutePath, actualUri.AbsolutePath, StringComparison.Ordinal);
    }
}

