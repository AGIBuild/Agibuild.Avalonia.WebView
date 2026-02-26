using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Verifies that WebViewCore correctly detects and integrates with optional adapter facets:
/// ICustomSchemeAdapter, IDownloadAdapter, IPermissionAdapter.
/// </summary>
[Collection("WebViewEnvironmentState")]
public sealed class ContractSemanticsV1FacetDetectionTests
{
    // ==================== ICustomSchemeAdapter ====================

    [Fact]
    public void CustomSchemeAdapter_receives_schemes_during_construction()
    {
        var schemes = new[]
        {
            new CustomSchemeRegistration { SchemeName = "app", HasAuthorityComponent = true, TreatAsSecure = true },
            new CustomSchemeRegistration { SchemeName = "local" }
        };

        var originalOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions { CustomSchemes = schemes };
            var adapter = MockWebViewAdapter.CreateWithCustomSchemes();
            using var core = new WebViewCore(adapter, new TestDispatcher());

            Assert.Equal(1, adapter.RegisterCallCount);
            Assert.NotNull(adapter.RegisteredSchemes);
            Assert.Equal(2, adapter.RegisteredSchemes!.Count);
            Assert.Equal("app", adapter.RegisteredSchemes[0].SchemeName);
        }
        finally
        {
            WebViewEnvironment.Options = originalOptions;
        }
    }

    [Fact]
    public void CustomSchemeAdapter_not_called_when_no_schemes_configured()
    {
        var originalOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions(); // empty CustomSchemes
            var adapter = MockWebViewAdapter.CreateWithCustomSchemes();
            using var core = new WebViewCore(adapter, new TestDispatcher());

            Assert.Equal(0, adapter.RegisterCallCount);
        }
        finally
        {
            WebViewEnvironment.Options = originalOptions;
        }
    }

    [Fact]
    public void Adapter_without_ICustomSchemeAdapter_does_not_throw()
    {
        var originalOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions
            {
                CustomSchemes = [new CustomSchemeRegistration { SchemeName = "test" }]
            };
            var adapter = MockWebViewAdapter.Create(); // plain adapter, no ICustomSchemeAdapter
            using var core = new WebViewCore(adapter, new TestDispatcher());
            // Should not throw — facet detection skips gracefully.
        }
        finally
        {
            WebViewEnvironment.Options = originalOptions;
        }
    }

    // ==================== IDownloadAdapter ====================

    [Fact]
    public void DownloadAdapter_events_forwarded_to_consumer()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        var dispatcher = new TestDispatcher();
        using var core = new WebViewCore(adapter, dispatcher);

        DownloadRequestedEventArgs? received = null;
        core.DownloadRequested += (_, e) => received = e;

        var args = new DownloadRequestedEventArgs(
            new Uri("https://example.test/file.zip"),
            "file.zip", "application/zip", 1024);

        adapter.RaiseDownloadRequested(args);

        Assert.NotNull(received);
        Assert.Equal("file.zip", received!.SuggestedFileName);
        Assert.Equal("application/zip", received.ContentType);
        Assert.Equal(1024, received.ContentLength);
    }

    [Fact]
    public void DownloadAdapter_consumer_can_cancel_via_event()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        core.DownloadRequested += (_, e) => e.Cancel = true;

        var args = new DownloadRequestedEventArgs(new Uri("https://example.test/f.zip"));
        adapter.RaiseDownloadRequested(args);

        Assert.True(args.Cancel);
    }

    [Fact]
    public void DownloadAdapter_consumer_can_set_download_path()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        core.DownloadRequested += (_, e) => e.DownloadPath = "/tmp/saved.zip";

        var args = new DownloadRequestedEventArgs(new Uri("https://example.test/f.zip"));
        adapter.RaiseDownloadRequested(args);

        Assert.Equal("/tmp/saved.zip", args.DownloadPath);
    }

    [Fact]
    public void Adapter_without_IDownloadAdapter_does_not_raise_download_events()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        bool raised = false;
        core.DownloadRequested += (_, _) => raised = true;

        // No way to raise on plain adapter — just verify no exception and event not raised.
        Assert.False(raised);
    }

    [Fact]
    public void DownloadRequested_not_raised_after_dispose()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        bool raised = false;
        core.DownloadRequested += (_, _) => raised = true;

        core.Dispose();

        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.test/f.zip")));
        Assert.False(raised);
    }

    // ==================== IPermissionAdapter ====================

    [Fact]
    public void PermissionAdapter_events_forwarded_to_consumer()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        PermissionRequestedEventArgs? received = null;
        core.PermissionRequested += (_, e) => received = e;

        var args = new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera,
            new Uri("https://example.test"));

        adapter.RaisePermissionRequested(args);

        Assert.NotNull(received);
        Assert.Equal(WebViewPermissionKind.Camera, received!.PermissionKind);
        Assert.Equal(new Uri("https://example.test"), received.Origin);
        Assert.Equal(PermissionState.Default, received.State);
    }

    [Fact]
    public void PermissionAdapter_consumer_can_allow()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        core.PermissionRequested += (_, e) => e.State = PermissionState.Allow;

        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Microphone);
        adapter.RaisePermissionRequested(args);

        Assert.Equal(PermissionState.Allow, args.State);
    }

    [Fact]
    public void PermissionAdapter_consumer_can_deny()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        core.PermissionRequested += (_, e) => e.State = PermissionState.Deny;

        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Geolocation);
        adapter.RaisePermissionRequested(args);

        Assert.Equal(PermissionState.Deny, args.State);
    }

    [Fact]
    public void PermissionRequested_not_raised_after_dispose()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, new TestDispatcher());

        bool raised = false;
        core.PermissionRequested += (_, _) => raised = true;

        core.Dispose();

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera));
        Assert.False(raised);
    }

    // ==================== Full adapter (all facets) ====================

    [Fact]
    public void Full_adapter_all_facets_detected()
    {
        var originalOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions
            {
                CustomSchemes = [new CustomSchemeRegistration { SchemeName = "app" }]
            };
            var adapter = MockWebViewAdapter.CreateFull();
            var dispatcher = new TestDispatcher();
            using var core = new WebViewCore(adapter, dispatcher);

            // Custom schemes registered
            Assert.Equal(1, adapter.RegisterCallCount);
            Assert.Equal("app", adapter.RegisteredSchemes![0].SchemeName);

            // Download events forwarded
            DownloadRequestedEventArgs? downloadArgs = null;
            core.DownloadRequested += (_, e) => downloadArgs = e;
            adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.test/f.zip")));
            Assert.NotNull(downloadArgs);

            // Permission events forwarded
            PermissionRequestedEventArgs? permArgs = null;
            core.PermissionRequested += (_, e) => permArgs = e;
            adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera));
            Assert.NotNull(permArgs);
        }
        finally
        {
            WebViewEnvironment.Options = originalOptions;
        }
    }
}
