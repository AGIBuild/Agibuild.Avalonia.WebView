using Agibuild.Fulora;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Convenience DI extensions for Agibuild WebView.
/// </summary>
public static class WebViewServiceCollectionExtensions
{
    /// <inheritdoc cref="Agibuild.Fulora.DependencyInjection.ServiceCollectionExtensions.AddWebView"/>
    public static IServiceCollection AddWebView(this IServiceCollection services)
        => Agibuild.Fulora.DependencyInjection.ServiceCollectionExtensions.AddWebView(services);

    /// <summary>
    /// Initializes the Agibuild WebView environment from the built <see cref="IServiceProvider"/>.
    /// <para>
    /// Extracts <see cref="ILoggerFactory"/> from the provider and stores it in
    /// <see cref="WebViewEnvironment"/> so all XAML <c>&lt;agw:WebView /&gt;</c> controls
    /// automatically receive logging.
    /// </para>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddLogging(b =&gt; b.AddConsole());
    /// services.AddWebView();
    /// var provider = services.BuildServiceProvider();
    /// provider.UseAgibuildWebView(); // ← initializes global WebView config
    /// </code>
    /// </example>
    /// </summary>
    public static IServiceProvider UseAgibuildWebView(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        WebViewEnvironment.Initialize(provider.GetService<ILoggerFactory>());
        return provider;
    }
}
