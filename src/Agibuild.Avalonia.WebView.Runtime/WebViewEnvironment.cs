using Microsoft.Extensions.Logging;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Global configuration for the Agibuild WebView components.
/// <para>
/// Call <see cref="Initialize(ILoggerFactory?)"/> once at application startup
/// so that all <see cref="WebView"/> controls automatically receive shared services.
/// </para>
/// </summary>
public static class WebViewEnvironment
{
    /// <summary>
    /// The <see cref="ILoggerFactory"/> used by all WebView controls
    /// that do not have an explicit <c>LoggerFactory</c> property set.
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Initializes the WebView environment with the given <see cref="ILoggerFactory"/>.
    /// </summary>
    public static void Initialize(ILoggerFactory? loggerFactory)
    {
        LoggerFactory ??= loggerFactory;
    }
}
