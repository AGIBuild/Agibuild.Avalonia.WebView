using System.Reflection;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Configures SPA hosting within the WebView. Supports serving embedded resources
/// (production) or proxying to a dev server (development) via a custom <c>app://</c> scheme.
/// </summary>
public sealed class SpaHostingOptions
{
    /// <summary>
    /// The custom scheme name. Default: "app".
    /// The WebView navigates to <c>{Scheme}://localhost/index.html</c>.
    /// </summary>
    public string Scheme { get; init; } = "app";

    /// <summary>
    /// The host component of the custom scheme URL. Default: "localhost".
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// The root document served for SPA router fallback. Default: "index.html".
    /// When a request path has no file extension, this document is served.
    /// </summary>
    public string FallbackDocument { get; init; } = "index.html";

    /// <summary>
    /// The embedded resource prefix (e.g. "wwwroot").
    /// Files in this folder are served as <c>app://localhost/{relative-path}</c>.
    /// Must be set when <see cref="DevServerUrl"/> is null.
    /// </summary>
    public string? EmbeddedResourcePrefix { get; init; }

    /// <summary>
    /// The assembly containing embedded resources. Required when <see cref="EmbeddedResourcePrefix"/> is set.
    /// </summary>
    public Assembly? ResourceAssembly { get; init; }

    /// <summary>
    /// When set, proxies all <c>app://</c> requests to this URL (e.g. "http://localhost:5173").
    /// Used for development with HMR (Vite, webpack, etc.).
    /// </summary>
    public string? DevServerUrl { get; init; }

    /// <summary>
    /// When true, the Bridge client script is auto-injected into pages served via this scheme.
    /// Default: true.
    /// </summary>
    public bool AutoInjectBridgeScript { get; init; } = true;

    /// <summary>
    /// Additional response headers to include with every served resource.
    /// Useful for CSP headers, CORS, etc.
    /// </summary>
    public IDictionary<string, string>? DefaultHeaders { get; init; }

    /// <summary>
    /// Builds the base URL for this scheme. Example: "app://localhost".
    /// </summary>
    internal string BaseUrl => $"{Scheme}://{Host}";

    /// <summary>
    /// Builds the entry point URL. Example: "app://localhost/index.html".
    /// </summary>
    public Uri EntryPointUri => new($"{BaseUrl}/{FallbackDocument}");
}
