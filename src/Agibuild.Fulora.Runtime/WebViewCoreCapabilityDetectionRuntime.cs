using Agibuild.Fulora.Adapters.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

internal sealed class WebViewCoreCapabilityDetectionRuntime
{
    private readonly IWebViewAdapter _adapter;
    private readonly IWebViewEnvironmentOptions _environmentOptions;
    private readonly ILogger _logger;

    public WebViewCoreCapabilityDetectionRuntime(
        IWebViewAdapter adapter,
        IWebViewEnvironmentOptions environmentOptions,
        ILogger logger)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _environmentOptions = environmentOptions ?? throw new ArgumentNullException(nameof(environmentOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ApplyEnvironmentOptions()
    {
        if (_adapter is not IWebViewAdapterOptions adapterOptions)
            return;

        adapterOptions.ApplyEnvironmentOptions(_environmentOptions);
        _logger.LogDebug("Environment options applied: DevTools={DevTools}, Ephemeral={Ephemeral}, UA={UA}",
            _environmentOptions.EnableDevTools,
            _environmentOptions.UseEphemeralSession,
            _environmentOptions.CustomUserAgent ?? "(default)");
    }

    public void RegisterConfiguredCustomSchemes()
    {
        if (_adapter is not ICustomSchemeAdapter customSchemeAdapter)
            return;

        var schemes = _environmentOptions.CustomSchemes;
        if (schemes.Count == 0)
            return;

        customSchemeAdapter.RegisterCustomSchemes(schemes);
        _logger.LogDebug("Custom schemes registered: {Count}", schemes.Count);
    }

    public ICookieManager? CreateCookieManager(WebViewCore owner)
    {
        if (_adapter is not ICookieAdapter cookieAdapter)
            return null;

        var manager = new WebViewCore.RuntimeCookieManager(cookieAdapter, owner, _logger);
        _logger.LogDebug("Cookie support: enabled");
        return manager;
    }

    public ICommandManager? CreateCommandManager(WebViewCore owner)
    {
        if (_adapter is not ICommandAdapter commandAdapter)
            return null;

        var manager = new WebViewCore.RuntimeCommandManager(commandAdapter, owner);
        _logger.LogDebug("Command support: enabled");
        return manager;
    }
}
