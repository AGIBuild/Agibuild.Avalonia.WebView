namespace Agibuild.Fulora;

/// <summary>
/// Extension methods for configuring SPA hosting.
/// </summary>
public static class SpaHostingExtensions
{
    /// <summary>
    /// Registers a custom scheme with the WebView environment for SPA hosting (production mode).
    /// Per-control resource serving is configured separately via <see cref="ISpaHostingWebView.EnableSpaHosting"/>.
    /// </summary>
    /// <param name="options">The environment options to configure.</param>
    /// <param name="schemeName">The custom scheme name (e.g. "app").</param>
    public static WebViewEnvironmentOptions AddEmbeddedFileProvider(
        this WebViewEnvironmentOptions options,
        string schemeName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(schemeName);

        var existing = options.CustomSchemes.ToList();
        if (!existing.Any(s => s.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase)))
        {
            existing.Add(new CustomSchemeRegistration
            {
                SchemeName = schemeName,
                HasAuthorityComponent = true,
                TreatAsSecure = true,
            });
            options.CustomSchemes = existing.AsReadOnly();
        }

        return options;
    }

    /// <summary>
    /// Configures the WebView environment for dev server proxy mode.
    /// All requests to the custom scheme are proxied to the given URL.
    /// </summary>
    /// <param name="options">The environment options to configure.</param>
    /// <param name="schemeName">The custom scheme name (e.g. "app").</param>
    /// <param name="devServerUrl">The dev server URL (e.g. "http://localhost:5173").</param>
    public static WebViewEnvironmentOptions AddDevServerProxy(
        this WebViewEnvironmentOptions options,
        string schemeName,
        string devServerUrl)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(schemeName);
        ArgumentException.ThrowIfNullOrEmpty(devServerUrl);

        // Ensure the custom scheme is registered.
        var existing = options.CustomSchemes.ToList();
        if (!existing.Any(s => s.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase)))
        {
            existing.Add(new CustomSchemeRegistration
            {
                SchemeName = schemeName,
                HasAuthorityComponent = true,
                TreatAsSecure = true,
            });
            options.CustomSchemes = existing.AsReadOnly();
        }

        return options;
    }
}
