using System.Reflection;
using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class BranchCoverageRound3Tests
{
    #region Medium: WebViewCore adapterDestroyed paths

    [Fact]
    public void Events_ignored_when_adapterDestroyed_but_not_disposed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var events = new List<string>();
        core.NavigationCompleted += (_, _) => events.Add("NavigationCompleted");
        core.NewWindowRequested += (_, _) => events.Add("NewWindowRequested");
        core.WebMessageReceived += (_, _) => events.Add("WebMessageReceived");
        core.WebResourceRequested += (_, _) => events.Add("WebResourceRequested");
        core.EnvironmentRequested += (_, _) => events.Add("EnvironmentRequested");
        core.DownloadRequested += (_, _) => events.Add("DownloadRequested");
        core.PermissionRequested += (_, _) => events.Add("PermissionRequested");

        // Flip the at-most-once "adapter destroyed" latch on the lifecycle machine directly so the
        // simulation bypasses Detach()/Dispose() (which would also unsubscribe adapter events and
        // defeat the point of this test).
        SetLifecycleFlag(core, "_adapterDestroyed", true);

        // Fire all event types — each should hit the _adapterDestroyed path
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        adapter.RaiseNewWindowRequested(new Uri("https://test.example"));
        adapter.RaiseWebMessage("{}", "https://test.example", Guid.NewGuid());
        adapter.RaiseWebResourceRequested();
        adapter.RaiseEnvironmentRequested();
        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
            new Uri("https://dl.example/file.zip"), "file.zip", "application/zip"));
        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://test.example")));

        Assert.Empty(events);
    }

    #endregion

    #region Medium: WebViewCore InvokeAsyncOnUiThread disposed

    [Fact]
    public async Task InvokeAsyncOnUiThread_disposed_returns_failure()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithCommands();
        var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        core.Dispose();

        var ex = await Assert.ThrowsAsync<ObjectDisposedException>(() => core.NavigateAsync(new Uri("https://test.example")));
        Assert.NotNull(ex);
    }

    #endregion

    #region Medium: WebViewCore WebMessageReceived with rpc null + no subscriber

    [Fact]
    public void WebMessageReceived_allowed_without_subscriber_does_not_throw()
    {
        // Line 1350: WebMessageReceived?.Invoke — null-conditional with no subscriber
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string>(StringComparer.Ordinal) { "https://test.example" }
        });

        // Send a web message that is allowed by policy but has no subscriber
        // This covers the null-conditional ?.Invoke path
        adapter.RaiseWebMessage(
            """{"data":"test"}""",
            "https://test.example",
            core.ChannelId,
            protocolVersion: 1);
    }

    #endregion

    #region Medium: WebViewCore RedoAsync

    [Fact]
    public async Task RedoAsync_executes_via_command_manager()
    {
        // Line 1604: RedoAsync coverage
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithCommands();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var cmdMgr = core.TryGetCommandManager();
        Assert.NotNull(cmdMgr);

        await cmdMgr!.RedoAsync();
        Assert.Contains(WebViewCommand.Redo, ((MockWebViewAdapterWithCommands)adapter).ExecutedCommands);
    }

    #endregion

    #region Medium: WebViewCore EnableWebMessageBridge with null AllowedOrigins

    [Fact]
    public void EnableWebMessageBridge_null_origins_covers_coalesce()
    {
        // Line 900: options.AllowedOrigins?.Count ?? 0
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var options = new WebMessageBridgeOptions { AllowedOrigins = null! };
        core.EnableWebMessageBridge(options);
    }

    #endregion

    #region Round 4: WebViewCore _disposed=true via reflection for event handlers

    [Fact]
    public void Events_ignored_when_disposed_but_not_detached_from_adapter()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var events = new List<string>();
        core.NavigationCompleted += (_, _) => events.Add("NavigationCompleted");
        core.NewWindowRequested += (_, _) => events.Add("NewWindowRequested");
        core.WebMessageReceived += (_, _) => events.Add("WebMessageReceived");
        core.WebResourceRequested += (_, _) => events.Add("WebResourceRequested");
        core.EnvironmentRequested += (_, _) => events.Add("EnvironmentRequested");
        core.DownloadRequested += (_, _) => events.Add("DownloadRequested");
        core.PermissionRequested += (_, _) => events.Add("PermissionRequested");

        // Flip the lifecycle machine's disposed latch directly so Dispose() is bypassed (otherwise the
        // adapter event subscription would be torn down before we can exercise the "disposed but still
        // receiving events" branch).
        SetLifecycleFlag(core, "_disposed", true);

        // Raise all events — each should be silently ignored due to _disposed=true
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        adapter.RaiseNewWindowRequested(new Uri("https://test.example"));
        adapter.RaiseWebMessage("{}", "https://test.example", Guid.NewGuid());
        adapter.RaiseWebResourceRequested();
        adapter.RaiseEnvironmentRequested();
        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
            new Uri("https://dl.example/file.zip"), "file.zip", "application/zip"));
        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://test.example")));

        Assert.Empty(events);
    }

    #endregion

    #region Round 4: WebResourceRequested/EnvironmentRequested with subscriber

    [Fact]
    public void WebResourceRequested_with_subscriber_invokes_handler()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var invoked = false;
        core.WebResourceRequested += (_, _) => invoked = true;
        adapter.RaiseWebResourceRequested();
        Assert.True(invoked);
    }

    [Fact]
    public void EnvironmentRequested_with_subscriber_invokes_handler()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var invoked = false;
        core.EnvironmentRequested += (_, _) => invoked = true;
        adapter.RaiseEnvironmentRequested();
        Assert.True(invoked);
    }

    [Fact]
    public void WebResourceRequested_without_subscriber_does_not_throw()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        // No subscriber attached — covers the null-delegate branch of ?.Invoke
        adapter.RaiseWebResourceRequested();
    }

    #endregion

    #region Round 4: NormalizeProfileHash char < '0'

    [Fact]
    public void NormalizeProfileHash_char_below_zero_covers_false_branch()
    {
        // Line 126: c is >= '0' → false branch (never tested because all hex chars are >= '0')
        // Use a char that is < '0' in ASCII (e.g., '/' = 0x2F, space = 0x20)
        var method = typeof(WebViewSessionPermissionProfile).GetMethod(
            "NormalizeProfileHash", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Hash with '/' character (ASCII 47, < '0' which is ASCII 48)
        var invalidHash = "sha256:" + "/" + new string('0', 63);
        var result = (string?)method!.Invoke(null, [invalidHash]);
        Assert.Null(result);
    }

    #endregion

    #region Round 4: Navigate to about:blank

    [Fact]
    public async Task NavigationStarting_with_about_blank_uri_covers_ternary()
    {
        // Line 541: info.RequestUri.AbsoluteUri != AboutBlank.AbsoluteUri ? info.RequestUri : AboutBlank
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { AutoCompleteNavigation = true };
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        await core.NavigateAsync(new Uri("about:blank"));
    }

    #endregion

    #region Round 5 Tier 1: WebViewCore remaining branches

    [Fact]
    public void EnvironmentRequested_without_subscriber_does_not_throw()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        adapter.RaiseEnvironmentRequested();
    }

    [Fact]
    public void WebMessageReceived_non_rpc_body_falls_through_to_event()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        core.EnableWebMessageBridge(new WebMessageBridgeOptions());
        var received = false;
        core.WebMessageReceived += (_, _) => received = true;

        adapter.RaiseWebMessage(
            body: """{"type":"not-rpc"}""",
            origin: "",
            channelId: core.ChannelId,
            protocolVersion: 1);

        Assert.True(received);
    }

    [Fact]
    public void ThrowIfNotOnUiThread_from_background_thread_throws()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        InvalidOperationException? caught = null;
        var thread = new Thread(() =>
        {
            try { core.EnableWebMessageBridge(new WebMessageBridgeOptions()); }
            catch (InvalidOperationException ex) { caught = ex; }
        });
        thread.Start();
        thread.Join();

        Assert.NotNull(caught);
        Assert.Contains("must be called on the UI thread", caught!.Message);
    }

    #endregion

    #region Round 5 Tier 2: WebViewCore OnNativeNavigationStarting about:blank ternary

    [Fact]
    public async Task NativeNavigationStarting_about_blank_covers_ternary()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var decision = await adapter.SimulateNativeNavigationStartingAsync(new Uri("about:blank"));
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task NativeNavigationStarting_normal_uri_covers_ternary()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        var decision = await adapter.SimulateNativeNavigationStartingAsync(new Uri("https://example.com"));
        Assert.True(decision.IsAllowed);
    }

    #endregion
}
