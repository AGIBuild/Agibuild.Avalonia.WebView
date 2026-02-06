namespace Agibuild.Avalonia.WebView;

public static class WebAuthCallbackMatcher
{
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

