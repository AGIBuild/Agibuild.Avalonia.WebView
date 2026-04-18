using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Fulora;

/// <summary>
/// Public entry-point for constructing <see cref="IWebView"/> instances outside of a DI container.
/// <para>
/// In DI scenarios prefer calling <c>services.AddFulora()</c> (or <c>services.AddWebView()</c>)
/// and resolving <c>Func&lt;IWebViewDispatcher, IWebView&gt;</c> from the container; this static
/// factory exists for non-DI consumers and bootstrap code.
/// </para>
/// <para>
/// The underlying runtime type is intentionally internal; callers only interact with
/// <see cref="IWebView"/> (and optional capability interfaces such as
/// <see cref="ISpaHostingWebView"/>).
/// </para>
/// </summary>
public static class WebViewFactory
{
    /// <summary>
    /// Creates a new <see cref="IWebView"/> using the default platform adapter for the current OS.
    /// </summary>
    /// <param name="dispatcher">UI thread dispatcher used to marshal adapter calls.</param>
    public static IWebView CreateDefault(IWebViewDispatcher dispatcher)
        => CreateDefault(dispatcher, loggerFactory: null);

    /// <summary>
    /// Creates a new <see cref="IWebView"/> using the default platform adapter for the current OS,
    /// routing diagnostics through the supplied <paramref name="loggerFactory"/>.
    /// </summary>
    /// <param name="dispatcher">UI thread dispatcher used to marshal adapter calls.</param>
    /// <param name="loggerFactory">Optional logger factory; when <see langword="null"/>, a no-op logger is used.</param>
    public static IWebView CreateDefault(IWebViewDispatcher dispatcher, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var logger = loggerFactory?.CreateLogger<WebViewCore>()
                     ?? (ILogger<WebViewCore>)NullLogger<WebViewCore>.Instance;

        return WebViewCore.CreateDefault(dispatcher, logger);
    }
}
