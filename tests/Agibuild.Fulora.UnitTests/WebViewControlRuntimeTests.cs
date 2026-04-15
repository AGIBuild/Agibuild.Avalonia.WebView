using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewControlRuntimeTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void BridgeTracer_set_before_core_attach_is_applied_when_core_is_attached()
    {
        var runtime = new WebViewControlRuntime();
        var tracer = NullBridgeTracer.Instance;
        var core = new WebViewCore(MockWebViewAdapter.Create(), _dispatcher);

        runtime.BridgeTracer = tracer;
        runtime.AttachCore(core);

        Assert.Same(tracer, runtime.BridgeTracer);
        Assert.Same(tracer, core.BridgeTracer);
    }

    [Fact]
    public void Status_properties_reflect_attached_core()
    {
        var runtime = new WebViewControlRuntime();
        var adapter = new MockWebViewAdapter
        {
            CanGoBack = true,
            CanGoForward = false
        };
        var core = new WebViewCore(adapter, _dispatcher);

        runtime.AttachCore(core);
        runtime.SetCoreAttached(true);

        Assert.True(runtime.IsAvailable);
        Assert.True(runtime.IsCoreAttached);
        Assert.True(runtime.CanGoBack);
        Assert.False(runtime.CanGoForward);
        Assert.Equal(core.ChannelId, runtime.ChannelId);
    }

    [Fact]
    public async Task CaptureScreenshotAsync_delegates_to_attached_core()
    {
        var runtime = new WebViewControlRuntime();
        var core = new WebViewCore(MockWebViewAdapter.CreateWithScreenshot(), _dispatcher);

        runtime.AttachCore(core);

        var bytes = await runtime.CaptureScreenshotAsync();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetZoomFactorAsync_delegates_to_attached_core()
    {
        var runtime = new WebViewControlRuntime();
        var core = new WebViewCore(MockWebViewAdapter.CreateWithZoom(), _dispatcher);

        runtime.AttachCore(core);
        await runtime.SetZoomFactorAsync(1.5);

        var zoom = await runtime.GetZoomFactorAsync();

        Assert.Equal(1.5, zoom);
    }

    [Fact]
    public void SetCustomUserAgent_delegates_to_attached_core()
    {
        var runtime = new WebViewControlRuntime();
        var adapter = MockWebViewAdapter.CreateWithOptions();
        var core = new WebViewCore(adapter, _dispatcher);

        runtime.AttachCore(core);
        runtime.SetCustomUserAgent("Fulora/Test");

        Assert.Equal("Fulora/Test", adapter.AppliedUserAgent);
    }

    [Fact]
    public void Bridge_access_before_core_attach_throws_with_control_ready_guidance()
    {
        var runtime = new WebViewControlRuntime();

        var ex = Assert.Throws<InvalidOperationException>(() => { _ = runtime.Bridge; });

        Assert.Contains("WebView is not yet attached", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bridge_access_after_platform_unavailable_throws_platform_not_supported()
    {
        var runtime = new WebViewControlRuntime();
        runtime.MarkAdapterUnavailable();

        Assert.Throws<PlatformNotSupportedException>(() => { _ = runtime.Bridge; });
        Assert.False(runtime.IsAvailable);
        Assert.False(runtime.IsCoreAttached);
        Assert.Equal(Guid.Empty, runtime.ChannelId);
    }

    [Fact]
    public void ClearCore_resets_IsCoreAttached_preventing_zombie_attached_state()
    {
        // ClearCore() enforces the invariant: _core == null → IsCoreAttached == false.
        // Without this, callers that call ClearCore() directly (without prior SetCoreAttached(false))
        // would produce a zombie state where IsCoreAttached is true but Core is null.
        var runtime = new WebViewControlRuntime();
        var core = new WebViewCore(MockWebViewAdapter.Create(), _dispatcher);

        runtime.AttachCore(core);
        runtime.SetCoreAttached(true);

        Assert.True(runtime.IsCoreAttached);
        Assert.NotNull(runtime.Core);

        runtime.ClearCore();

        Assert.Null(runtime.Core);
        Assert.False(runtime.IsCoreAttached);
        Assert.False(runtime.IsAvailable);
    }

    [Fact]
    public void SetCoreAttached_false_retains_core_reference_as_intended_for_deferred_disposal()
    {
        // After early host-close detach, the control marks itself as not attached but intentionally
        // keeps the Core reference alive so DestroyAttachedCore() can later call core.Dispose().
        // ClearCore() is the operation that both clears the reference and resets attachment state.
        var runtime = new WebViewControlRuntime();
        var core = new WebViewCore(MockWebViewAdapter.Create(), _dispatcher);

        runtime.AttachCore(core);
        runtime.SetCoreAttached(true);
        runtime.SetCoreAttached(false);

        Assert.False(runtime.IsCoreAttached);
        Assert.NotNull(runtime.Core);
        Assert.False(runtime.IsAvailable);
    }
}
