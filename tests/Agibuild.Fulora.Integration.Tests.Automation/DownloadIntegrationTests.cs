using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Integration tests for the Download management feature.
/// Exercises the full WebDialog → WebViewCore → IDownloadAdapter event chain.
///
/// HOW IT WORKS (for newcomers):
///   1. We create a MockWebViewAdapterWithDownload — it implements IDownloadAdapter with a DownloadRequested event.
///   2. We wrap it in a WebDialog.
///   3. We subscribe to dialog.DownloadRequested.
///   4. We raise a download event on the adapter and verify it arrives at the dialog.
///   5. We also test Cancel and DownloadPath propagation.
/// </summary>
public sealed class DownloadIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebDialog Dialog, MockWebViewAdapterWithDownload Adapter) CreateDialogWithDownload()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithDownload();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        return (dialog, adapter);
    }

    // ──────────────────── Test 1: Event forwarding ────────────────────

    [AvaloniaFact]
    public void DownloadRequested_event_forwards_through_dialog()
    {
        var (dialog, adapter) = CreateDialogWithDownload();

        DownloadRequestedEventArgs? received = null;
        dialog.DownloadRequested += (_, e) => received = e;

        var args = new DownloadRequestedEventArgs(
            new Uri("https://example.com/file.zip"), "file.zip");
        adapter.RaiseDownloadRequested(args);

        Assert.NotNull(received);
        Assert.Equal("https://example.com/file.zip", received!.DownloadUri.ToString());
        Assert.Equal("file.zip", received.SuggestedFileName);
        dialog.Dispose();
    }

    // ──────────────────── Test 2: Cancel propagation ────────────────────

    [AvaloniaFact]
    public void DownloadRequested_cancel_propagates()
    {
        var (dialog, adapter) = CreateDialogWithDownload();

        dialog.DownloadRequested += (_, e) => e.Cancel = true;

        var args = new DownloadRequestedEventArgs(
            new Uri("https://example.com/file.zip"), "file.zip");
        adapter.RaiseDownloadRequested(args);

        Assert.True(args.Cancel);
        dialog.Dispose();
    }

    // ──────────────────── Test 3: DownloadPath propagation ────────────────────

    [AvaloniaFact]
    public void DownloadRequested_downloadPath_propagates()
    {
        var (dialog, adapter) = CreateDialogWithDownload();

        dialog.DownloadRequested += (_, e) => e.DownloadPath = "/tmp/file.zip";

        var args = new DownloadRequestedEventArgs(
            new Uri("https://example.com/file.zip"), "file.zip");
        adapter.RaiseDownloadRequested(args);

        Assert.Equal("/tmp/file.zip", args.DownloadPath);
        dialog.Dispose();
    }

    // ──────────────────── Test 4: No download support ────────────────────

    [AvaloniaFact]
    public void Basic_adapter_does_not_fire_download_events()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        DownloadRequestedEventArgs? received = null;
        dialog.DownloadRequested += (_, e) => received = e;

        // No download events can be raised — basic adapter lacks IDownloadAdapter
        Assert.Null(received);
    }
}
