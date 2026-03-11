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
}
