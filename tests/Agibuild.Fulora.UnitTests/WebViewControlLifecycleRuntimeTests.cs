using Avalonia.Platform;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewControlLifecycleRuntimeTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void AttachToNativeControl_creates_core_attaches_and_replays_pending_source()
    {
        var adapter = MockWebViewAdapter.Create();
        WebViewCore? attachedCore = null;
        var controlRuntime = new WebViewControlRuntime();
        var eventRuntime = new WebViewControlEventRuntime(
            _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, () => { }, _ => { },
            () => null, () => null, () => null, () => null, () => null,
            _ => Task.CompletedTask, () => 1.0, _ => { });

        var lifecycle = new WebViewControlLifecycleRuntime(
            controlRuntime,
            eventRuntime,
            getLoggerFactory: () => NullLoggerFactory.Instance,
            getEnvironmentOptions: () => null,
            getPendingSource: () => new Uri("https://example.test/replayed"),
            setCore: core => attachedCore = core,
            setCoreAttached: _ => { },
            setAdapterUnavailable: _ => { },
            createDispatcher: () => _dispatcher,
            createCore: (_, _, _) => new WebViewCore(adapter, _dispatcher),
            wrapPlatformHandle: handle => new TestNativeHandle(handle.Handle, handle.HandleDescriptor ?? string.Empty));

        lifecycle.AttachToNativeControl(new TestAvaloniaPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.NotNull(attachedCore);
        Assert.Equal(1, adapter.AttachCallCount);
        Assert.Equal(new Uri("https://example.test/replayed"), adapter.LastNavigationUri);
    }

    [Fact]
    public void AttachToNativeControl_marks_adapter_unavailable_when_platform_not_supported()
    {
        var controlRuntime = new WebViewControlRuntime();
        var eventRuntime = new WebViewControlEventRuntime(
            _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, () => { }, _ => { },
            () => null, () => null, () => null, () => null, () => null,
            _ => Task.CompletedTask, () => 1.0, _ => { });
        var unavailable = false;
        WebViewCore? core = null;
        var lifecycle = new WebViewControlLifecycleRuntime(
            controlRuntime,
            eventRuntime,
            () => NullLoggerFactory.Instance,
            () => null,
            () => null,
            value => core = value,
            _ => { },
            value => unavailable = value,
            () => _dispatcher,
            createCore: (_, _, _) => throw new PlatformNotSupportedException());

        lifecycle.AttachToNativeControl(new TestAvaloniaPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.True(unavailable);
        Assert.Null(core);
        Assert.Throws<PlatformNotSupportedException>(() => { _ = controlRuntime.Bridge; });
    }

    [Fact]
    public void AttachToNativeControl_rethrows_non_platform_failures_and_clears_runtime_state()
    {
        var controlRuntime = new WebViewControlRuntime();
        var eventRuntime = new WebViewControlEventRuntime(
            _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, () => { }, _ => { },
            () => null, () => null, () => null, () => null, () => null,
            _ => Task.CompletedTask, () => 1.0, _ => { });
        WebViewCore? core = null;
        var lifecycle = new WebViewControlLifecycleRuntime(
            controlRuntime,
            eventRuntime,
            () => NullLoggerFactory.Instance,
            () => null,
            () => null,
            value => core = value,
            _ => { },
            _ => { },
            () => _dispatcher,
            createCore: (_, _, _) => throw new InvalidOperationException("boom"));

        var error = Assert.Throws<InvalidOperationException>(
            () => lifecycle.AttachToNativeControl(new TestAvaloniaPlatformHandle(IntPtr.Zero, "test-parent")));

        Assert.Equal("boom", error.Message);
        Assert.Null(core);
        Assert.Throws<InvalidOperationException>(() => { _ = controlRuntime.Bridge; });
    }

    [Fact]
    public void DestroyAttachedCore_detaches_events_and_disposes_core()
    {
        var adapter = MockWebViewAdapter.Create();
        var controlRuntime = new WebViewControlRuntime();
        var eventRuntime = new WebViewControlEventRuntime(
            _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, _ => { }, () => { }, _ => { },
            () => null, () => null, () => null, () => null, () => null,
            _ => Task.CompletedTask, () => 1.0, _ => { });
        var lifecycle = new WebViewControlLifecycleRuntime(
            controlRuntime, eventRuntime,
            () => NullLoggerFactory.Instance, () => null, () => null, _ => { }, _ => { }, _ => { }, () => _dispatcher,
            createCore: (_, _, _) => new WebViewCore(adapter, _dispatcher),
            wrapPlatformHandle: handle => new TestNativeHandle(handle.Handle, handle.HandleDescriptor ?? string.Empty));
        var core = new WebViewCore(adapter, _dispatcher);
        controlRuntime.AttachCore(core);
        eventRuntime.Attach(core);
        core.Attach(new TestNativeHandle(IntPtr.Zero, "test-parent"));

        lifecycle.DestroyAttachedCore(core, coreAttached: true);

        Assert.Equal(1, adapter.DetachCallCount);
    }

    private sealed class TestNativeHandle(nint handle, string descriptor) : INativeHandle
    {
        public nint Handle => handle;
        public string HandleDescriptor => descriptor;
    }

    private sealed class TestAvaloniaPlatformHandle(nint handle, string descriptor) : IPlatformHandle
    {
        public nint Handle => handle;
        public string HandleDescriptor => descriptor;
    }
}
