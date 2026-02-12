using System.Reflection;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Extension methods for configuring SPA hosting.
/// </summary>
public static class SpaHostingExtensions
{
    /// <summary>
    /// Configures the WebView environment for embedded SPA hosting (production mode).
    /// Call this at startup; all WebView controls that enable SPA hosting will serve from embedded resources.
    /// </summary>
    /// <param name="options">The environment options to configure.</param>
    /// <param name="schemeName">The custom scheme name (e.g. "app"). Default: "app".</param>
    /// <param name="resourceAssembly">Assembly containing embedded resources.</param>
    /// <param name="resourcePrefix">Embedded resource path prefix (e.g. "wwwroot").</param>
    public static WebViewEnvironmentOptions AddEmbeddedFileProvider(
        this WebViewEnvironmentOptions options,
        string schemeName,
        Assembly resourceAssembly,
        string resourcePrefix)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(schemeName);
        ArgumentNullException.ThrowIfNull(resourceAssembly);
        ArgumentException.ThrowIfNullOrEmpty(resourcePrefix);

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
