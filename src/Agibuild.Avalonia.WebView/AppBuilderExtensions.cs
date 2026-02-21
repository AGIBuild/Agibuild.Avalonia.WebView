using Avalonia;
using Microsoft.Extensions.Logging;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Avalonia <see cref="AppBuilder"/> extensions for the Agibuild WebView.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Initializes the Agibuild WebView environment with default settings (no logging).
    /// </summary>
    public static AppBuilder UseAgibuildWebView(this AppBuilder builder)
        => UseAgibuildWebView(builder, loggerFactory: null);

    /// <summary>
    /// Initializes the Agibuild WebView environment with an optional logger factory.
    /// </summary>
    /// <param name="builder">The Avalonia app builder.</param>
    /// <param name="loggerFactory">Optional logger factory used by WebView internals.</param>
    /// <returns>The same <see cref="AppBuilder"/> for fluent chaining.</returns>
    public static AppBuilder UseAgibuildWebView(this AppBuilder builder, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        WebViewEnvironment.Initialize(loggerFactory);
        return builder;
    }

}
