using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Integration tests for the Zoom Control feature.
///
/// HOW IT WORKS (for newcomers):
///   1. We create a MockWebViewAdapterWithZoom — it stores a zoom factor in memory.
///   2. We wrap it in a WebDialog (same as a real app would).
///   3. We set/get zoom via async APIs and verify clamping behavior.
///   4. We verify multiple async updates are observable.
///   5. A basic adapter (no zoom support) silently ignores zoom set.
/// </summary>
public sealed class ZoomIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ──────────────────── Test 1: Default zoom is 1.0 ────────────────────

    [AvaloniaFact]
    public async Task Default_zoom_is_1()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        Assert.Equal(1.0, await dialog.GetZoomFactorAsync());
    }

    // ──────────────────── Test 2: Set and get zoom ────────────────────

    [AvaloniaFact]
    public async Task Set_and_get_zoom()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(2.0);
        Assert.Equal(2.0, await dialog.GetZoomFactorAsync());
    }

    // ──────────────────── Test 3: Zoom is clamped ────────────────────

    [AvaloniaFact]
    public async Task Zoom_is_clamped()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(0.01);
        Assert.Equal(0.25, await dialog.GetZoomFactorAsync(), 2);

        await dialog.SetZoomFactorAsync(99.0);
        Assert.Equal(5.0, await dialog.GetZoomFactorAsync(), 2);
    }

    // ──────────────────── Test 4: Repeated updates are observable ────────────────────

    [AvaloniaFact]
    public async Task Repeated_zoom_updates_are_observable()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(1.5);
        Assert.Equal(1.5, await dialog.GetZoomFactorAsync());
        await dialog.SetZoomFactorAsync(2.0);
        Assert.Equal(2.0, await dialog.GetZoomFactorAsync());
    }

    // ──────────────────── Test 5: Without adapter, zoom is no-op ────────────────────

    [AvaloniaFact]
    public async Task Without_adapter_zoom_is_noop()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create(); // basic — no zoom
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(2.0); // should not throw
        Assert.Equal(1.0, await dialog.GetZoomFactorAsync());
    }

    // ──────────────────── Test 6: Zoom set after dispose throws ────────────────────

    [AvaloniaFact]
    public async Task Zoom_set_after_dispose_throws_ObjectDisposedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        dialog.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => dialog.SetZoomFactorAsync(2.0));
    }

    // ──────────────────── Test 7: Multiple zoom changes accumulate ────────────────────

    [AvaloniaFact]
    public async Task Multiple_zoom_changes_accumulate()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(1.5);
        await dialog.SetZoomFactorAsync(2.0);
        await dialog.SetZoomFactorAsync(0.5);
        Assert.Equal(0.5, await dialog.GetZoomFactorAsync());
    }
}
