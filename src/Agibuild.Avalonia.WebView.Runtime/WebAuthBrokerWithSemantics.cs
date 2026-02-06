namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Decorates an <see cref="IWebAuthBroker"/> to enforce v1 baseline contract semantics.
/// </summary>
public sealed class WebAuthBrokerWithSemantics : IWebAuthBroker
{
    private readonly IWebAuthBroker _inner;

    public WebAuthBrokerWithSemantics(IWebAuthBroker inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<WebAuthResult> AuthenticateAsync(ITopLevelWindow owner, AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(options);

        if (options.CallbackUri is null)
        {
            throw new ArgumentException("CallbackUri is required.", nameof(options));
        }

        // Clone to avoid mutating the caller's input; enforce ephemeral session.
        var effectiveOptions = new AuthOptions
        {
            CallbackUri = options.CallbackUri,
            UseEphemeralSession = true
        };

        var result = await _inner.AuthenticateAsync(owner, effectiveOptions).ConfigureAwait(false);

        if (result.Status == WebAuthStatus.Success)
        {
            if (result.CallbackUri is null || !WebAuthCallbackMatcher.IsStrictMatch(options.CallbackUri, result.CallbackUri))
            {
                return new WebAuthResult
                {
                    Status = WebAuthStatus.Error,
                    CallbackUri = result.CallbackUri,
                    Error = "CallbackUri did not match expected strict callback rules."
                };
            }
        }

        return result;
    }
}

