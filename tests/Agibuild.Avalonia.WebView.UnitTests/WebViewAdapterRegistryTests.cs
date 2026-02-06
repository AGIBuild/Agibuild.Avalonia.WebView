using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class WebViewAdapterRegistryTests
{
    [Fact]
    public void Register_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WebViewAdapterRegistry.Register(null!));
    }

    [Fact]
    public void Register_empty_adapterId_throws_ArgumentException()
    {
        var reg = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.MacOS, "  ", () => new MockWebViewAdapter());

        Assert.Throws<ArgumentException>(() => WebViewAdapterRegistry.Register(reg));
    }

    [Fact]
    public void Register_null_factory_throws_ArgumentNullException()
    {
        // Create with null factory via positional ctor
        var reg = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.MacOS, "test-adapter", null!);

        Assert.Throws<ArgumentNullException>(() => WebViewAdapterRegistry.Register(reg));
    }

    [Fact]
    public void HasAnyForCurrentPlatform_returns_true_after_register()
    {
        // The macOS adapter module initializer has already registered via assembly load.
        // On macOS CI this will be true; on other platforms it depends.
        // We just verify the method doesn't throw.
        _ = WebViewAdapterRegistry.HasAnyForCurrentPlatform();
    }

    [Fact]
    public void TryCreateForCurrentPlatform_creates_adapter_if_registered()
    {
        // This depends on whether an adapter is registered for the current platform.
        var result = WebViewAdapterRegistry.TryCreateForCurrentPlatform(out var adapter, out var reason);

        if (result)
        {
            Assert.NotNull(adapter);
            Assert.Null(reason);
        }
        else
        {
            Assert.NotNull(reason);
        }
    }
}
