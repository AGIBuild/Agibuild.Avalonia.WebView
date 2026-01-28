using Agibuild.Avalonia.WebView.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace Agibuild.Avalonia.WebView.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgibuildAvaloniaWebView(
        this IServiceCollection services,
        Func<IServiceProvider, IWebViewAdapter> adapterFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(adapterFactory);

        services.AddSingleton<Func<IServiceProvider, IWebViewAdapter>>(_ => adapterFactory);
        return services;
    }
}
