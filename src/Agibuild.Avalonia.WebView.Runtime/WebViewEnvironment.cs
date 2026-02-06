using Microsoft.Extensions.Logging;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Default implementation of <see cref="IWebViewEnvironmentOptions"/>.
/// </summary>
public sealed class WebViewEnvironmentOptions : IWebViewEnvironmentOptions
{
    public bool EnableDevTools { get; set; }
    public string? CustomUserAgent { get; set; }
    public bool UseEphemeralSession { get; set; }
}

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
    /// Global environment options applied to all WebView instances unless overridden per-instance.
    /// </summary>
    public static IWebViewEnvironmentOptions Options { get; set; } = new WebViewEnvironmentOptions();

    /// <summary>
    /// Initializes the WebView environment with the given <see cref="ILoggerFactory"/>.
    /// </summary>
    public static void Initialize(ILoggerFactory? loggerFactory)
    {
        LoggerFactory ??= loggerFactory;
    }

    /// <summary>
    /// Initializes the WebView environment with the given <see cref="ILoggerFactory"/> and <see cref="IWebViewEnvironmentOptions"/>.
    /// </summary>
    public static void Initialize(ILoggerFactory? loggerFactory, IWebViewEnvironmentOptions? options)
    {
        LoggerFactory ??= loggerFactory;
        if (options is not null)
        {
            Options = options;
        }
    }
}
