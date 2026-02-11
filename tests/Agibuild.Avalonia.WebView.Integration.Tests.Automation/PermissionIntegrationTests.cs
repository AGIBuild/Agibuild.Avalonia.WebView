using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Integration tests for the Permission request handling feature.
/// Exercises the full WebDialog → WebViewCore → IPermissionAdapter event chain.
///
/// HOW IT WORKS (for newcomers):
///   1. We create a MockWebViewAdapterWithPermission — it implements IPermissionAdapter.
///   2. We wrap it in a WebDialog.
///   3. We subscribe to dialog.PermissionRequested.
///   4. We raise a permission event on the adapter and verify it arrives at the dialog.
///   5. We also test Allow/Deny state propagation.
/// </summary>
public sealed class PermissionIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebDialog Dialog, MockWebViewAdapterWithPermission Adapter) CreateDialogWithPermission()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithPermission();
        var dialog = new WebDialog(host, adapter, _dispatcher);
        return (dialog, adapter);
    }

    // ──────────────────── Test 1: Event forwarding ────────────────────

    [AvaloniaFact]
    public void PermissionRequested_event_forwards_through_dialog()
    {
        var (dialog, adapter) = CreateDialogWithPermission();

        PermissionRequestedEventArgs? received = null;
        dialog.PermissionRequested += (_, e) => received = e;

        var args = new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(args);

        Assert.NotNull(received);
        Assert.Equal(WebViewPermissionKind.Camera, received!.PermissionKind);
        dialog.Dispose();
    }

    // ──────────────────── Test 2: Allow propagation ────────────────────

    [AvaloniaFact]
    public void PermissionRequested_allow_propagates()
    {
        var (dialog, adapter) = CreateDialogWithPermission();

        dialog.PermissionRequested += (_, e) => e.State = PermissionState.Allow;

        var args = new PermissionRequestedEventArgs(
            WebViewPermissionKind.Microphone, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(args);

        Assert.Equal(PermissionState.Allow, args.State);
        dialog.Dispose();
    }

    // ──────────────────── Test 3: Deny propagation ────────────────────

    [AvaloniaFact]
    public void PermissionRequested_deny_propagates()
    {
        var (dialog, adapter) = CreateDialogWithPermission();

        dialog.PermissionRequested += (_, e) => e.State = PermissionState.Deny;

        var args = new PermissionRequestedEventArgs(
            WebViewPermissionKind.Geolocation, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(args);

        Assert.Equal(PermissionState.Deny, args.State);
        dialog.Dispose();
    }

    // ──────────────────── Test 4: Default state ────────────────────

    [AvaloniaFact]
    public void PermissionRequested_default_state_is_default()
    {
        var (dialog, adapter) = CreateDialogWithPermission();

        PermissionRequestedEventArgs? received = null;
        dialog.PermissionRequested += (_, e) => received = e;

        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera);
        adapter.RaisePermissionRequested(args);

        Assert.NotNull(received);
        Assert.Equal(PermissionState.Default, received!.State);
        dialog.Dispose();
    }
}
