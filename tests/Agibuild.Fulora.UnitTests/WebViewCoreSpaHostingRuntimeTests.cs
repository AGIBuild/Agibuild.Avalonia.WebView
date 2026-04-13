using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewCoreSpaHostingRuntimeTests
{
    [Fact]
    public void EnableSpaHosting_registers_scheme_and_auto_enables_bridge_when_requested()
    {
        var host = new StubSpaHostingHost();
        var runtime = new WebViewCoreSpaHostingRuntime(host, NullLogger.Instance);

        runtime.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
            AutoInjectBridgeScript = true
        });

        Assert.NotNull(host.RegisteredScheme);
        Assert.Equal("app", host.RegisteredScheme!.SchemeName);
        Assert.Equal(1, host.EnableBridgeCallCount);
        Assert.True(host.HandlerRegistered);
    }

    [Fact]
    public void EnableSpaHosting_does_not_auto_enable_bridge_when_already_enabled()
    {
        var host = new StubSpaHostingHost { IsBridgeEnabled = true };
        var runtime = new WebViewCoreSpaHostingRuntime(host, NullLogger.Instance);

        runtime.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
            AutoInjectBridgeScript = true
        });

        Assert.Equal(0, host.EnableBridgeCallCount);
    }

    [Fact]
    public void EnableSpaHosting_twice_throws()
    {
        var host = new StubSpaHostingHost();
        var runtime = new WebViewCoreSpaHostingRuntime(host, NullLogger.Instance);
        var options = new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly
        };

        runtime.EnableSpaHosting(options);

        Assert.Throws<InvalidOperationException>(() => runtime.EnableSpaHosting(options));
    }

    [Fact]
    public void Dispose_unhooks_web_resource_handler()
    {
        var host = new StubSpaHostingHost();
        var runtime = new WebViewCoreSpaHostingRuntime(host, NullLogger.Instance);

        runtime.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly
        });
        runtime.Dispose();

        Assert.False(host.HandlerRegistered);
    }

    private sealed class StubSpaHostingHost : IWebViewCoreSpaHostingHost
    {
        public bool IsBridgeEnabled { get; set; }
        public int EnableBridgeCallCount { get; private set; }
        public CustomSchemeRegistration? RegisteredScheme { get; private set; }
        public bool HandlerRegistered { get; private set; }

        public void ThrowIfDisposed() { }

        public void EnableWebMessageBridge(WebMessageBridgeOptions options) => EnableBridgeCallCount++;

        public void RegisterCustomScheme(CustomSchemeRegistration registration) => RegisteredScheme = registration;

        public void AddWebResourceRequestedHandler(EventHandler<WebResourceRequestedEventArgs> handler) => HandlerRegistered = true;

        public void RemoveWebResourceRequestedHandler(EventHandler<WebResourceRequestedEventArgs> handler) => HandlerRegistered = false;
    }
}
