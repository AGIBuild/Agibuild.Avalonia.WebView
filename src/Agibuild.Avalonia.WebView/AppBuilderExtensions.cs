using Avalonia;
using Microsoft.Extensions.Logging;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Avalonia <see cref="AppBuilder"/> extensions for the Agibuild WebView.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Initializes the Agibuild WebView environment from a DI <see cref="IServiceProvider"/>.
    /// <para>
    /// Call this in your <c>AppBuilder</c> chain so that all <c>&lt;agw:WebView /&gt;</c>
    /// controls automatically receive logging and other shared services.
    /// </para>
    /// <example>
    /// <code>
    /// var provider = services.BuildServiceProvider();
    ///
    /// AppBuilder.Configure&lt;App&gt;()
    ///     .UsePlatformDetect()
    ///     .UseAgibuildWebView(provider.GetService&lt;ILoggerFactory&gt;())
    ///     .StartWithClassicDesktopLifetime(args);
    /// </code>
    /// </example>
    /// </summary>
    /// <summary>
    /// Initializes the Agibuild WebView environment with default settings (no logging).
    /// </summary>
    public static AppBuilder UseAgibuildWebView(this AppBuilder builder)
        => UseAgibuildWebView(builder, loggerFactory: null);

    public static AppBuilder UseAgibuildWebView(this AppBuilder builder, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        WebViewEnvironment.Initialize(loggerFactory);
        return builder;
    }

}
