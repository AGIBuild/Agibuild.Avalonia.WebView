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
///   3. We set/get ZoomFactor and verify clamping behavior.
///   4. We verify ZoomFactorChanged events fire correctly.
///   5. A basic adapter (no zoom support) silently ignores zoom set.
/// </summary>
public sealed class ZoomIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ──────────────────── Test 1: Default zoom is 1.0 ────────────────────

    [AvaloniaFact]
    public void Default_zoom_is_1()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        Assert.Equal(1.0, dialog.ZoomFactor);
    }

    // ──────────────────── Test 2: Set and get zoom ────────────────────

    [AvaloniaFact]
    public void Set_and_get_zoom()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        dialog.ZoomFactor = 2.0;
        Assert.Equal(2.0, dialog.ZoomFactor);
    }

    // ──────────────────── Test 3: Zoom is clamped ────────────────────

    [AvaloniaFact]
    public void Zoom_is_clamped()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        dialog.ZoomFactor = 0.01;
        Assert.Equal(0.25, dialog.ZoomFactor, 2);

        dialog.ZoomFactor = 99.0;
        Assert.Equal(5.0, dialog.ZoomFactor, 2);
    }

    // ──────────────────── Test 4: ZoomFactorChanged fires ────────────────────

    [AvaloniaFact]
    public void ZoomFactorChanged_fires()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        double? received = null;
        dialog.ZoomFactorChanged += (_, z) => received = z;

        dialog.ZoomFactor = 1.5;
        Assert.Equal(1.5, received);
    }

    // ──────────────────── Test 5: Without adapter, zoom is no-op ────────────────────

    [AvaloniaFact]
    public void Without_adapter_zoom_is_noop()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create(); // basic — no zoom
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        dialog.ZoomFactor = 2.0; // should not throw
        Assert.Equal(1.0, dialog.ZoomFactor);
    }

    // ──────────────────── Test 6: Zoom set after dispose throws ────────────────────

    [AvaloniaFact]
    public void Zoom_set_after_dispose_throws_ObjectDisposedException()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        dialog.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dialog.ZoomFactor = 2.0);
    }

    // ──────────────────── Test 7: Unsubscribe stops events ────────────────────

    [AvaloniaFact]
    public void Unsubscribe_stops_events()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        int callCount = 0;
        void Handler(object? s, double z) => callCount++;
        dialog.ZoomFactorChanged += Handler;
        dialog.ZoomFactor = 1.5;
        Assert.Equal(1, callCount);

        dialog.ZoomFactorChanged -= Handler;
        dialog.ZoomFactor = 2.0;
        Assert.Equal(1, callCount); // should not increment
    }

    // ──────────────────── Test 8: Multiple zoom changes accumulate ────────────────────

    [AvaloniaFact]
    public void Multiple_zoom_changes_accumulate()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        var values = new List<double>();
        dialog.ZoomFactorChanged += (_, z) => values.Add(z);

        dialog.ZoomFactor = 1.5;
        dialog.ZoomFactor = 2.0;
        dialog.ZoomFactor = 0.5;

        Assert.Equal(3, values.Count);
        Assert.Equal(1.5, values[0]);
        Assert.Equal(2.0, values[1]);
        Assert.Equal(0.5, values[2]);
    }
}
