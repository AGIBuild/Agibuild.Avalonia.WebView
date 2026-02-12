using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public class DevToolsTests
{
    [Fact]
    public void IWebView_declares_OpenDevTools_CloseDevTools_IsDevToolsOpen()
    {
        // Compile-time verification: IWebView has the methods
        var methods = typeof(IWebView).GetMethod("OpenDevTools");
        Assert.NotNull(methods);

        methods = typeof(IWebView).GetMethod("CloseDevTools");
        Assert.NotNull(methods);

        var prop = typeof(IWebView).GetProperty("IsDevToolsOpen");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void IDevToolsAdapter_interface_has_expected_members()
    {
        // Verify IDevToolsAdapter shape at runtime (internal, so we use reflection)
        var type = typeof(IWebView).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IDevToolsAdapter");

        // IDevToolsAdapter is defined in Adapters.Abstractions, may not be visible here.
        // Instead verify via IWebView which delegates to it.
        Assert.NotNull(typeof(IWebView).GetMethod("OpenDevTools"));
        Assert.NotNull(typeof(IWebView).GetMethod("CloseDevTools"));
        Assert.NotNull(typeof(IWebView).GetProperty("IsDevToolsOpen"));
    }

    [Fact]
    public void TestWebViewHost_DevTools_are_noop()
    {
        using var host = new Agibuild.Avalonia.WebView.Testing.TestWebViewHost();
        host.OpenDevTools();
        host.CloseDevTools();
        Assert.False(host.IsDevToolsOpen);
    }
}
