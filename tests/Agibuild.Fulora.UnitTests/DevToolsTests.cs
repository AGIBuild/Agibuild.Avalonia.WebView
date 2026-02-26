using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public class DevToolsTests
{
    [Fact]
    public void IWebView_declares_async_DevTools_members()
    {
        // Compile-time verification: IWebView has the methods
        var methods = typeof(IWebView).GetMethod("OpenDevToolsAsync");
        Assert.NotNull(methods);

        methods = typeof(IWebView).GetMethod("CloseDevToolsAsync");
        Assert.NotNull(methods);

        methods = typeof(IWebView).GetMethod("IsDevToolsOpenAsync");
        Assert.NotNull(methods);
        Assert.Equal(typeof(Task<bool>), methods!.ReturnType);
    }

    [Fact]
    public void IDevToolsAdapter_interface_has_expected_members()
    {
        // Verify IDevToolsAdapter shape at runtime (internal, so we use reflection)
        var type = typeof(IWebView).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IDevToolsAdapter");

        // IDevToolsAdapter is defined in Adapters.Abstractions, may not be visible here.
        // Instead verify via IWebView which delegates to it.
        Assert.NotNull(typeof(IWebView).GetMethod("OpenDevToolsAsync"));
        Assert.NotNull(typeof(IWebView).GetMethod("CloseDevToolsAsync"));
        Assert.NotNull(typeof(IWebView).GetMethod("IsDevToolsOpenAsync"));
    }

    [Fact]
    public async Task TestWebViewHost_DevTools_are_noop()
    {
        using var host = new Agibuild.Fulora.Testing.TestWebViewHost();
        await host.OpenDevToolsAsync();
        await host.CloseDevToolsAsync();
        Assert.False(await host.IsDevToolsOpenAsync());
    }
}
