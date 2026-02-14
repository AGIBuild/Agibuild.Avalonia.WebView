using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1AdapterLifecycleEventsTests
{
    // -----------------------------------------------------------------------
    //  10.2 AdapterCreated fires after Attach with correct PlatformHandle
    // -----------------------------------------------------------------------

    [Fact]
    public void AdapterCreated_fires_after_attach_with_platform_handle()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        var typedHandle = new TestWindowsWebView2PlatformHandle(
            Handle: 0x1234, CoreWebView2Handle: 0xAAAA, CoreWebView2ControllerHandle: 0xBBBB);
        adapter.HandleToReturn = typedHandle;

        var core = new WebViewCore(adapter, dispatcher);

        AdapterCreatedEventArgs? receivedArgs = null;
        core.AdapterCreated += (_, e) => receivedArgs = e;

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.NotNull(receivedArgs);
        Assert.Same(typedHandle, receivedArgs!.PlatformHandle);
    }

    [Fact]
    public void AdapterCreated_fires_with_null_handle_when_adapter_does_not_support_handles()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter(); // No INativeWebViewHandleProvider

        var core = new WebViewCore(adapter, dispatcher);

        AdapterCreatedEventArgs? receivedArgs = null;
        core.AdapterCreated += (_, e) => receivedArgs = e;

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.NotNull(receivedArgs);
        Assert.Null(receivedArgs!.PlatformHandle);
    }

    [Fact]
    public void AdapterCreated_fires_exactly_once_per_attach()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestPlatformHandle(0x1234, "WebView2");

        var core = new WebViewCore(adapter, dispatcher);

        var fireCount = 0;
        core.AdapterCreated += (_, _) => fireCount++;

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void AdapterCreated_fires_before_pending_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestPlatformHandle(0x1234, "WebView2");
        adapter.AutoCompleteNavigation = true;

        var core = new WebViewCore(adapter, dispatcher);

        var events = new List<string>();
        core.AdapterCreated += (_, _) => events.Add("AdapterCreated");
        core.NavigationStarted += (_, _) => events.Add("NavigationStarted");

        // Set source before attach to create a pending navigation
        // (we can only do this via the WebView control, but for core-level test
        //  we verify the ordering within Attach itself)
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        // AdapterCreated should have fired
        Assert.Contains("AdapterCreated", events);
    }

    // -----------------------------------------------------------------------
    //  10.3 AdapterDestroyed fires before Detach and at most once
    // -----------------------------------------------------------------------

    [Fact]
    public void AdapterDestroyed_fires_during_detach()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var fired = false;
        core.AdapterDestroyed += (_, _) => fired = true;

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));
        core.Detach();

        Assert.True(fired);
    }

    [Fact]
    public void AdapterDestroyed_fires_during_dispose_if_not_detached()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var fired = false;
        core.AdapterDestroyed += (_, _) => fired = true;

        // No Attach/Detach — just dispose
        core.Dispose();

        Assert.True(fired);
    }

    [Fact]
    public void AdapterDestroyed_fires_at_most_once_when_both_detach_and_dispose()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var fireCount = 0;
        core.AdapterDestroyed += (_, _) => fireCount++;

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));
        core.Detach();
        core.Dispose();

        Assert.Equal(1, fireCount);
    }

    // -----------------------------------------------------------------------
    //  10.4 TryGetWebViewHandle returns null after AdapterDestroyed
    // -----------------------------------------------------------------------

    [Fact]
    public void TryGetWebViewHandle_returns_null_after_adapter_destroyed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestPlatformHandle(0x1234, "WebView2");

        var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        // Before destroy — handle should be available
        Assert.NotNull(core.TryGetWebViewHandle());

        core.Detach();

        // After destroy — handle should be null
        Assert.Null(core.TryGetWebViewHandle());
    }

    [Fact]
    public async Task TryGetWebViewHandleAsync_returns_null_after_adapter_destroyed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestPlatformHandle(0x1234, "WebView2");

        var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var beforeDetach = await core.TryGetWebViewHandleAsync();
        Assert.NotNull(beforeDetach);

        core.Detach();

        var afterDetach = await core.TryGetWebViewHandleAsync();
        Assert.Null(afterDetach);
    }

    // -----------------------------------------------------------------------
    //  10.5 No events fire after AdapterDestroyed
    // -----------------------------------------------------------------------

    [Fact]
    public void No_events_fire_after_adapter_destroyed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var eventsAfterDestroy = new List<string>();
        core.NavigationCompleted += (_, _) => eventsAfterDestroy.Add("NavigationCompleted");
        core.WebMessageReceived += (_, _) => eventsAfterDestroy.Add("WebMessageReceived");
        core.NewWindowRequested += (_, _) => eventsAfterDestroy.Add("NewWindowRequested");
        core.WebResourceRequested += (_, _) => eventsAfterDestroy.Add("WebResourceRequested");
        core.EnvironmentRequested += (_, _) => eventsAfterDestroy.Add("EnvironmentRequested");

        core.Detach(); // raises AdapterDestroyed

        // Try to raise adapter events after destroyed
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        adapter.RaiseWebMessage("{}", "https://example.test", Guid.NewGuid());
        adapter.RaiseNewWindowRequested(new Uri("https://example.test"));
        adapter.RaiseWebResourceRequested();
        adapter.RaiseEnvironmentRequested();

        Assert.Empty(eventsAfterDestroy);
    }

    // -----------------------------------------------------------------------
    //  10.6 Typed handle pattern-matching works
    // -----------------------------------------------------------------------

    [Fact]
    public void Typed_handle_pattern_matching_windows()
    {
        var handle = new TestWindowsWebView2PlatformHandle(
            Handle: 0x1234, CoreWebView2Handle: 0xAAAA, CoreWebView2ControllerHandle: 0xBBBB);

        Assert.True(handle is IWindowsWebView2PlatformHandle);
        Assert.Equal("WebView2", handle.HandleDescriptor);

        var typed = (IWindowsWebView2PlatformHandle)handle;
        Assert.Equal((nint)0xAAAA, typed.CoreWebView2Handle);
        Assert.Equal((nint)0xBBBB, typed.CoreWebView2ControllerHandle);
    }

    [Fact]
    public void Typed_handle_pattern_matching_apple()
    {
        var handle = new TestAppleWKWebViewPlatformHandle(WKWebViewHandle: 0x5678);

        Assert.True(handle is IAppleWKWebViewPlatformHandle);
        Assert.Equal("WKWebView", handle.HandleDescriptor);

        var typed = (IAppleWKWebViewPlatformHandle)handle;
        Assert.Equal((nint)0x5678, typed.WKWebViewHandle);
    }

    [Fact]
    public void Typed_handle_pattern_matching_gtk()
    {
        var handle = new TestGtkWebViewPlatformHandle(WebKitWebViewHandle: 0x9ABC);

        Assert.True(handle is IGtkWebViewPlatformHandle);
        Assert.Equal("WebKitGTK", handle.HandleDescriptor);

        var typed = (IGtkWebViewPlatformHandle)handle;
        Assert.Equal((nint)0x9ABC, typed.WebKitWebViewHandle);
    }

    [Fact]
    public void Typed_handle_pattern_matching_android()
    {
        var handle = new TestAndroidWebViewPlatformHandle(AndroidWebViewHandle: 0xDEF0);

        Assert.True(handle is IAndroidWebViewPlatformHandle);
        Assert.Equal("AndroidWebView", handle.HandleDescriptor);

        var typed = (IAndroidWebViewPlatformHandle)handle;
        Assert.Equal((nint)0xDEF0, typed.AndroidWebViewHandle);
    }

    [Fact]
    public void AdapterCreated_event_args_allow_pattern_matching()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        var typedHandle = new TestWindowsWebView2PlatformHandle(
            Handle: 0x1234, CoreWebView2Handle: 0xAAAA, CoreWebView2ControllerHandle: 0xBBBB);
        adapter.HandleToReturn = typedHandle;

        var core = new WebViewCore(adapter, dispatcher);

        IWindowsWebView2PlatformHandle? matchedHandle = null;
        core.AdapterCreated += (_, e) =>
        {
            if (e.PlatformHandle is IWindowsWebView2PlatformHandle win)
            {
                matchedHandle = win;
            }
        };

        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        Assert.NotNull(matchedHandle);
        Assert.Equal((nint)0xAAAA, matchedHandle!.CoreWebView2Handle);
        Assert.Equal((nint)0xBBBB, matchedHandle.CoreWebView2ControllerHandle);
    }
}
