namespace Agibuild.Fulora;

/// <summary>
/// Extension methods for bootstrapping SPA navigation and bridge registration on <see cref="IWebView"/>.
/// </summary>
public static class WebViewBootstrapExtensions
{
    private const string ReadyEventScript =
        "(function(){window.__agWebViewReady=true;window.dispatchEvent(new Event('agWebViewReady'));})()";

    private const string DefaultErrorHtml =
        "<html><body style='font-family:system-ui;padding:2em;color:#333'>" +
        "<h2>Navigation failed</h2><p>{0}</p></body></html>";

    /// <summary>
    /// Bootstraps the WebView with SPA navigation and bridge service registration in one deterministic call.
    /// <para>
    /// When <see cref="SpaBootstrapOptions.DevServerUrl"/> is set, navigates directly to the dev server.
    /// Otherwise, navigates to the SPA entry point (caller must enable SPA hosting beforehand for production).
    /// </para>
    /// <para>
    /// After successful navigation, configures bridge services via <see cref="SpaBootstrapOptions.ConfigureBridge"/>
    /// and dispatches the <c>agWebViewReady</c> event to the page.
    /// </para>
    /// </summary>
    public static async Task BootstrapSpaAsync(
        this IWebView webView,
        SpaBootstrapOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webView);
        ArgumentNullException.ThrowIfNull(options);

        EnsureSpaHostingConfigured(webView, options);
        var targetUri = ResolveTargetUri(options);

        try
        {
            await webView.NavigateAsync(targetUri).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var errorHtml = options.ErrorPageFactory?.Invoke(ex)
                ?? string.Format(DefaultErrorHtml, System.Net.WebUtility.HtmlEncode(ex.Message));
            await webView.NavigateToStringAsync(errorHtml).ConfigureAwait(false);
            return;
        }

        options.ConfigureBridge?.Invoke(webView.Bridge, options.ServiceProvider);

        await webView.InvokeScriptAsync(ReadyEventScript).ConfigureAwait(false);
    }

    private static Uri ResolveTargetUri(SpaBootstrapOptions options)
    {
        if (!string.IsNullOrEmpty(options.DevServerUrl))
            return new Uri(options.DevServerUrl);

        return new Uri($"{options.Scheme}://localhost/{options.FallbackDocument}");
    }

    private static void EnsureSpaHostingConfigured(IWebView webView, SpaBootstrapOptions options)
    {
        var hasEmbeddedPrefix = !string.IsNullOrWhiteSpace(options.EmbeddedResourcePrefix);
        var hasEmbeddedAssembly = options.ResourceAssembly is not null;

        if (hasEmbeddedPrefix != hasEmbeddedAssembly)
        {
            throw new InvalidOperationException(
                "SpaBootstrapOptions.EmbeddedResourcePrefix and ResourceAssembly must be configured together.");
        }

        if (!string.IsNullOrEmpty(options.DevServerUrl) || !hasEmbeddedPrefix)
            return;

        if (webView is not ISpaHostingWebView spaHostingWebView)
        {
            throw new InvalidOperationException(
                $"WebView type '{webView.GetType().Name}' does not support SPA hosting bootstrap.");
        }

        spaHostingWebView.EnableSpaHosting(new SpaHostingOptions
        {
            Scheme = options.Scheme,
            FallbackDocument = options.FallbackDocument,
            EmbeddedResourcePrefix = options.EmbeddedResourcePrefix,
            ResourceAssembly = options.ResourceAssembly,
            AutoInjectBridgeScript = true
        });
    }
}
