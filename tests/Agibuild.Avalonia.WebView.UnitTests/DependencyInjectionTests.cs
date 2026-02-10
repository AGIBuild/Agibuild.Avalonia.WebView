using Agibuild.Avalonia.WebView.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

[Collection("WebViewEnvironmentState")]
public sealed class DependencyInjectionTests : IDisposable
{
    public DependencyInjectionTests()
    {
        WebViewEnvironment.LoggerFactory = null;
    }

    public void Dispose()
    {
        WebViewEnvironment.LoggerFactory = null;
    }

    [Fact]
    public void AddWebView_registers_IWebView_factory()
    {
        var services = new ServiceCollection();
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetService<Func<IWebViewDispatcher, IWebView>>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void AddWebView_registers_IWebViewDispatcher()
    {
        var services = new ServiceCollection();
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IWebViewDispatcher>();

        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void AddWebView_IWebViewDispatcher_is_transient()
    {
        var services = new ServiceCollection();
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        var d1 = provider.GetService<IWebViewDispatcher>();
        var d2 = provider.GetService<IWebViewDispatcher>();

        Assert.NotNull(d1);
        Assert.NotNull(d2);
        Assert.NotSame(d1, d2);
    }

    [Fact]
    public void UseAgibuildWebView_initializes_WebViewEnvironment_LoggerFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        provider.UseAgibuildWebView();

        Assert.NotNull(WebViewEnvironment.LoggerFactory);
    }

    [Fact]
    public void Factory_resolves_without_error()
    {
        // Note: CreateDefault requires a registered platform adapter.
        // In unit tests we only verify that the factory itself resolves.
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetService<Func<IWebViewDispatcher, IWebView>>();

        Assert.NotNull(factory);
    }
}
