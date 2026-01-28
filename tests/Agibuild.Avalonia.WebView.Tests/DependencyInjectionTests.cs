using Agibuild.Avalonia.WebView.Adapters;
using Agibuild.Avalonia.WebView.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Agibuild.Avalonia.WebView.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Adapter_factory_resolves_new_instance_each_time()
    {
        var services = new ServiceCollection();
        services.AddAgibuildAvaloniaWebView(_ => new MockWebViewAdapter());

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<IServiceProvider, IWebViewAdapter>>();

        var first = factory(provider);
        var second = factory(provider);

        Assert.NotSame(first, second);
    }
}
