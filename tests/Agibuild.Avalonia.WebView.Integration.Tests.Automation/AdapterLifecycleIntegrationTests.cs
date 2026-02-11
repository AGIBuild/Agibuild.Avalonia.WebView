using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Integration tests for adapter lifecycle events and platform handles.
/// Exercises AdapterCreated, AdapterDestroyed, and TryGetWebViewHandle.
///
/// HOW IT WORKS (for newcomers):
///   1. We create a MockWebViewAdapterWithHandle — it implements INativeWebViewHandleProvider.
///   2. We wrap it in a WebDialog.
///   3. We verify AdapterCreated fires with the correct PlatformHandle.
///   4. We verify AdapterDestroyed fires on Dispose.
///   5. We verify TryGetWebViewHandle() returns null after destroy.
///   6. We test typed platform handle pattern matching (e.g., IWindowsWebView2PlatformHandle).
/// </summary>
public sealed class AdapterLifecycleIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ──────────────────── Test 1: AdapterCreated fires with handle ────────────────────

    [AvaloniaFact]
    public void AdapterCreated_fires_with_platform_handle()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestWindowsWebView2PlatformHandle(1, 2, 3);

        AdapterCreatedEventArgs? createdArgs = null;
        // Must subscribe before WebDialog ctor (which calls Attach → AdapterCreated)
        // Actually, WebDialog wires events in ctor, so subscribe on dialog after creation
        var dialog = new WebDialog(host, adapter, _dispatcher);
        // AdapterCreated already fired during Attach. Let's use WebViewCore directly.
        dialog.Dispose();

        // Use WebViewCore directly for precise lifecycle control
        adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestWindowsWebView2PlatformHandle(1, 2, 3);

        using var core = new WebViewCore(adapter, _dispatcher);
        core.AdapterCreated += (_, e) => createdArgs = e;

        // Attach triggers AdapterCreated
        core.Attach(new TestPlatformHandle(nint.Zero, "test"));

        Assert.NotNull(createdArgs);
        Assert.NotNull(createdArgs!.PlatformHandle);
    }

    // ──────────────────── Test 2: TryGetWebViewHandle returns handle ────────────────────

    [AvaloniaFact]
    public void TryGetWebViewHandle_returns_handle_from_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithHandle();
        var expected = new TestWindowsWebView2PlatformHandle(100, 200, 300);
        adapter.HandleToReturn = expected;

        using var core = new WebViewCore(adapter, _dispatcher);

        var handle = core.TryGetWebViewHandle();
        Assert.NotNull(handle);
        Assert.Equal("WebView2", handle!.HandleDescriptor);
    }

    // ──────────────────── Test 3: TryGetWebViewHandle null for basic adapter ────────────────────

    [AvaloniaFact]
    public void TryGetWebViewHandle_null_for_basic_adapter()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        Assert.Null(core.TryGetWebViewHandle());
    }

    // ──────────────────── Test 4: Typed handle pattern matching ────────────────────

    [AvaloniaFact]
    public void Typed_handle_pattern_matching_works()
    {
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestWindowsWebView2PlatformHandle(1, 2, 3);

        using var core = new WebViewCore(adapter, _dispatcher);

        var handle = core.TryGetWebViewHandle();

        // Pattern match like a consumer would
        if (handle is IWindowsWebView2PlatformHandle wv2)
        {
            Assert.Equal(1, (int)wv2.Handle);
            Assert.Equal(2, (int)wv2.CoreWebView2Handle);
            Assert.Equal(3, (int)wv2.CoreWebView2ControllerHandle);
        }
        else
        {
            Assert.Fail("Expected IWindowsWebView2PlatformHandle");
        }
    }

    [AvaloniaFact]
    public void Apple_typed_handle_pattern_matching_works()
    {
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestAppleWKWebViewPlatformHandle(42);

        using var core = new WebViewCore(adapter, _dispatcher);

        var handle = core.TryGetWebViewHandle();

        if (handle is IAppleWKWebViewPlatformHandle wk)
        {
            Assert.Equal(42, (int)wk.WKWebViewHandle);
        }
        else
        {
            Assert.Fail("Expected IAppleWKWebViewPlatformHandle");
        }
    }

    // ──────────────────── Test 5: AdapterDestroyed fires on Dispose ────────────────────

    [AvaloniaFact]
    public void AdapterDestroyed_fires_on_dispose()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        bool destroyed = false;
        core.AdapterDestroyed += (_, _) => destroyed = true;

        core.Dispose();

        Assert.True(destroyed);
    }

    // ──────────────────── Test 6: TryGetWebViewHandle null after destroy ────────────────────

    [AvaloniaFact]
    public void TryGetWebViewHandle_null_after_dispose()
    {
        var adapter = MockWebViewAdapter.CreateWithHandle();
        adapter.HandleToReturn = new TestWindowsWebView2PlatformHandle(1, 2, 3);

        var core = new WebViewCore(adapter, _dispatcher);

        // Before dispose — has handle
        Assert.NotNull(core.TryGetWebViewHandle());

        core.Dispose();

        // After dispose — null
        Assert.Null(core.TryGetWebViewHandle());
    }
}
