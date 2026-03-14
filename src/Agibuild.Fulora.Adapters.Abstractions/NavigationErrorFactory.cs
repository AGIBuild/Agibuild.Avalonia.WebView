namespace Agibuild.Fulora.Adapters.Abstractions;

/// <summary>
/// Categories of navigation errors that platform adapters classify before
/// delegating to the shared factory.
/// </summary>
internal enum NavigationErrorCategory
{
    Timeout,
    Network,
    Ssl,
    Other
}

/// <summary>
/// Creates typed <see cref="WebViewNavigationException"/> subclasses from a
/// platform-neutral error category. Eliminates duplicated switch expressions
/// across platform adapters.
/// </summary>
internal static class NavigationErrorFactory
{
    /// <summary>
    /// Creates the appropriate navigation exception for the given error category.
    /// </summary>
    internal static Exception Create(
        NavigationErrorCategory category,
        string message,
        Guid navigationId,
        Uri requestUri) => category switch
        {
            NavigationErrorCategory.Timeout => new WebViewTimeoutException(message, navigationId, requestUri),
            NavigationErrorCategory.Network => new WebViewNetworkException(message, navigationId, requestUri),
            NavigationErrorCategory.Ssl => new WebViewSslException(message, navigationId, requestUri),
            _ => new WebViewNavigationException(message, navigationId, requestUri),
        };
}
