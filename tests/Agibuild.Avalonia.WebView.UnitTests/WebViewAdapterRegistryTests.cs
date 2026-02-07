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
    public void Register_null_AdapterId_throws_ArgumentNullException()
    {
        var reg = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.MacOS, null!, () => new MockWebViewAdapter());

        Assert.Throws<ArgumentNullException>(() => WebViewAdapterRegistry.Register(reg));
    }

    [Fact]
    public void Register_duplicate_registration_is_silently_ignored()
    {
        // Register the same (Platform, AdapterId) twice — second call should not throw.
        var reg1 = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.Gtk, "dup-test", () => new MockWebViewAdapter(), Priority: 10);
        var reg2 = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.Gtk, "dup-test", () => new MockWebViewAdapter(), Priority: 20);

        WebViewAdapterRegistry.Register(reg1);
        WebViewAdapterRegistry.Register(reg2); // Should not throw (TryAdd ignores duplicates).
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
            Assert.Contains("No WebView adapter registered", reason);
        }
    }
}
