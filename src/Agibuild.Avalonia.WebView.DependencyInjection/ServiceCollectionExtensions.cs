using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Avalonia.WebView.DependencyInjection;

/// <summary>
/// Dependency injection registrations for Agibuild WebView services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Agibuild WebView services in the DI container.
    /// <para>
    /// Registers:
    /// <list type="bullet">
    ///   <item><c>Func&lt;IWebViewDispatcher, IWebView&gt;</c> — factory for creating <see cref="IWebView"/> instances programmatically.</item>
    /// </list>
    /// </para>
    /// <para>
    /// After building the <see cref="IServiceProvider"/>, call
    /// <c>provider.UseAgibuildWebView()</c> (or
    /// <see cref="WebViewEnvironment.Initialize(ILoggerFactory?)"/> directly) so that XAML
    /// <c>&lt;agw:WebView /&gt;</c> controls automatically pick up <see cref="ILoggerFactory"/>
    /// and other shared services from DI.
    /// </para>
    /// </summary>
    public static IServiceCollection AddWebView(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the Avalonia UI-thread dispatcher as IWebViewDispatcher (transient — one per resolve).
        services.AddTransient<IWebViewDispatcher>(_ => new AvaloniaWebViewDispatcher());

        services.AddSingleton<Func<IWebViewDispatcher, IWebView>>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<WebViewCore>()
                         ?? (ILogger<WebViewCore>)NullLogger<WebViewCore>.Instance;

            return dispatcher => WebViewCore.CreateDefault(dispatcher, logger);
        });

        return services;
    }
}
