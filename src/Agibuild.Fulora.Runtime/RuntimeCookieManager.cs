using Agibuild.Fulora.Adapters.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

/// <summary>
/// Runtime wrapper around <see cref="ICookieAdapter"/> that adds lifecycle guards and dispatcher marshaling.
/// </summary>
internal sealed class RuntimeCookieManager : ICookieManager
{
    private readonly ICookieAdapter _cookieAdapter;
    private readonly IWebViewCoreOperationHost _host;
    private readonly ILogger _logger;

    public RuntimeCookieManager(ICookieAdapter cookieAdapter, IWebViewCoreOperationHost host, ILogger logger)
    {
        _cookieAdapter = cookieAdapter ?? throw new ArgumentNullException(nameof(cookieAdapter));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        _host.ThrowIfDisposed();
        _logger.LogDebug("CookieManager.GetCookiesAsync: {Uri}", uri);
        return _host.EnqueueOperationAsync("Cookie.GetCookiesAsync", () => _cookieAdapter.GetCookiesAsync(uri));
    }

    public Task SetCookieAsync(WebViewCookie cookie)
    {
        ArgumentNullException.ThrowIfNull(cookie);
        _host.ThrowIfDisposed();
        _logger.LogDebug("CookieManager.SetCookieAsync: {Name}@{Domain}", cookie.Name, cookie.Domain);
        return _host.EnqueueOperationAsync("Cookie.SetCookieAsync", () => _cookieAdapter.SetCookieAsync(cookie));
    }

    public Task DeleteCookieAsync(WebViewCookie cookie)
    {
        ArgumentNullException.ThrowIfNull(cookie);
        _host.ThrowIfDisposed();
        _logger.LogDebug("CookieManager.DeleteCookieAsync: {Name}@{Domain}", cookie.Name, cookie.Domain);
        return _host.EnqueueOperationAsync("Cookie.DeleteCookieAsync", () => _cookieAdapter.DeleteCookieAsync(cookie));
    }

    public Task ClearAllCookiesAsync()
    {
        _host.ThrowIfDisposed();
        _logger.LogDebug("CookieManager.ClearAllCookiesAsync");
        return _host.EnqueueOperationAsync("Cookie.ClearAllCookiesAsync", () => _cookieAdapter.ClearAllCookiesAsync());
    }
}
