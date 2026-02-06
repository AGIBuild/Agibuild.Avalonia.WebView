using Microsoft.Extensions.DependencyInjection;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Extension methods for registering Avalonia WebDialog and WebAuthBroker services.
/// <para>
/// Call <c>services.AddWebViewDialogServices()</c> to register:
/// <list type="bullet">
///   <item><see cref="IWebDialogFactory"/> → <see cref="AvaloniaWebDialogFactory"/> (singleton)</item>
///   <item><see cref="IWebAuthBroker"/> → <see cref="WebAuthBroker"/> (transient)</item>
/// </list>
/// </para>
/// <para>
/// Note: Call <c>services.AddWebView()</c> first if you also need
/// the base <see cref="IWebView"/> factory and dispatcher registrations.
/// </para>
/// </summary>
public static class WebViewServiceCollectionExtensions
{
    /// <summary>
    /// Registers the production <see cref="IWebDialogFactory"/> and <see cref="IWebAuthBroker"/>
    /// in the DI container.
    /// </summary>
    public static IServiceCollection AddWebViewDialogServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWebDialogFactory, AvaloniaWebDialogFactory>();

        services.AddTransient<IWebAuthBroker>(sp =>
            new WebAuthBroker(sp.GetRequiredService<IWebDialogFactory>()));

        return services;
    }
}
