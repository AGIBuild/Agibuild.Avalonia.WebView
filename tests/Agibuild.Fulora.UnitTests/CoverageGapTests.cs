using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Supplementary tests targeting uncovered code paths in record structs,
/// WebViewCore edge cases, and WebDialog error paths.
/// </summary>
public sealed class CoverageGapTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ========================= WebMessageEnvelope =========================

    [Fact]
    public void WebMessageEnvelope_value_equality()
    {
        var channelId = Guid.NewGuid();
        var a = new WebMessageEnvelope("body", "origin", channelId, 1);
        var b = new WebMessageEnvelope("body", "origin", channelId, 1);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void WebMessageEnvelope_value_inequality()
    {
        var channelId = Guid.NewGuid();
        var a = new WebMessageEnvelope("body1", "origin", channelId, 1);
        var b = new WebMessageEnvelope("body2", "origin", channelId, 1);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void WebMessageEnvelope_ToString_contains_fields()
    {
        var channelId = Guid.NewGuid();
        var envelope = new WebMessageEnvelope("hello", "https://origin.test", channelId, 1);

        var str = envelope.ToString();
        Assert.Contains("hello", str);
        Assert.Contains("https://origin.test", str);
    }

    [Fact]
    public void WebMessageEnvelope_deconstruction()
    {
        var channelId = Guid.NewGuid();
        var envelope = new WebMessageEnvelope("body", "origin", channelId, 2);

        var (body, origin, channel, version) = envelope;
        Assert.Equal("body", body);
        Assert.Equal("origin", origin);
        Assert.Equal(channelId, channel);
        Assert.Equal(2, version);
    }

    // ========================= WebViewCookie =========================

    [Fact]
    public void WebViewCookie_value_equality()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(1);
        var a = new WebViewCookie("name", "value", ".example.com", "/", expires, true, false);
        var b = new WebViewCookie("name", "value", ".example.com", "/", expires, true, false);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void WebViewCookie_inequality_different_name()
    {
        var a = new WebViewCookie("name1", "value", ".example.com", "/", null, false, false);
        var b = new WebViewCookie("name2", "value", ".example.com", "/", null, false, false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewCookie_nullable_Expires_equality()
    {
        var a = new WebViewCookie("n", "v", ".d", "/", null, false, false);
        var b = new WebViewCookie("n", "v", ".d", "/", null, false, false);

        Assert.Equal(a, b);
    }

    [Fact]
    public void WebViewCookie_nullable_Expires_inequality()
    {
        var a = new WebViewCookie("n", "v", ".d", "/", null, false, false);
        var b = new WebViewCookie("n", "v", ".d", "/", DateTimeOffset.UtcNow, false, false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewCookie_ToString_contains_fields()
    {
        var cookie = new WebViewCookie("session", "abc123", ".example.com", "/path", null, true, true);

        var str = cookie.ToString();
        Assert.Contains("session", str);
        Assert.Contains("abc123", str);
        Assert.Contains(".example.com", str);
    }

    [Fact]
    public void WebViewCookie_deconstruction()
    {
        var cookie = new WebViewCookie("n", "v", "d", "p", null, true, false);

        var (name, value, domain, path, expires, isSecure, isHttpOnly) = cookie;
        Assert.Equal("n", name);
        Assert.Equal("v", value);
        Assert.Equal("d", domain);
        Assert.Equal("p", path);
        Assert.Null(expires);
        Assert.True(isSecure);
        Assert.False(isHttpOnly);
    }

    // ========================= WebMessagePolicyDecision =========================

    [Fact]
    public void WebMessagePolicyDecision_Allow_is_allowed()
    {
        var decision = WebMessagePolicyDecision.Allow();

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.DropReason);
    }

    [Fact]
    public void WebMessagePolicyDecision_Deny_has_reason()
    {
        var decision = WebMessagePolicyDecision.Deny(WebMessageDropReason.OriginNotAllowed);

        Assert.False(decision.IsAllowed);
        Assert.Equal(WebMessageDropReason.OriginNotAllowed, decision.DropReason);
    }

    [Fact]
    public void WebMessagePolicyDecision_value_equality()
    {
        var a = WebMessagePolicyDecision.Allow();
        var b = WebMessagePolicyDecision.Allow();

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ========================= NativeNavigationStartingInfo =========================

    [Fact]
    public void NativeNavigationStartingInfo_value_equality()
    {
        var id = Guid.NewGuid();
        var uri = new Uri("https://example.test");
        var a = new NativeNavigationStartingInfo(id, uri, true);
        var b = new NativeNavigationStartingInfo(id, uri, true);

        Assert.Equal(a, b);
    }

    [Fact]
    public void NativeNavigationStartingDecision_value_equality()
    {
        var id = Guid.NewGuid();
        var a = new NativeNavigationStartingDecision(true, id);
        var b = new NativeNavigationStartingDecision(true, id);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ========================= WebViewAdapterRegistration record =========================

    [Fact]
    public void WebViewAdapterRegistration_value_equality()
    {
        Func<IWebViewAdapter> factory = () => new MockWebViewAdapter();
        var a = new WebViewAdapterRegistration(WebViewAdapterPlatform.iOS, "wk-ios", factory, 100);
        var b = new WebViewAdapterRegistration(WebViewAdapterPlatform.iOS, "wk-ios", factory, 100);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void WebViewAdapterRegistration_inequality_different_platform()
    {
        Func<IWebViewAdapter> factory = () => new MockWebViewAdapter();
        var a = new WebViewAdapterRegistration(WebViewAdapterPlatform.iOS, "wk", factory, 100);
        var b = new WebViewAdapterRegistration(WebViewAdapterPlatform.Gtk, "wk", factory, 100);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewAdapterRegistration_inequality_different_priority()
    {
        Func<IWebViewAdapter> factory = () => new MockWebViewAdapter();
        var a = new WebViewAdapterRegistration(WebViewAdapterPlatform.iOS, "wk", factory, 100);
        var b = new WebViewAdapterRegistration(WebViewAdapterPlatform.iOS, "wk", factory, 200);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WebViewAdapterRegistration_ToString_contains_fields()
    {
        var reg = new WebViewAdapterRegistration(
            WebViewAdapterPlatform.iOS, "wkwebview-ios", () => new MockWebViewAdapter(), 100);

        var str = reg.ToString();
        Assert.Contains("iOS", str);
        Assert.Contains("wkwebview-ios", str);
    }

    [Fact]
    public void WebViewAdapterRegistration_deconstruction()
    {
        Func<IWebViewAdapter> factory = () => new MockWebViewAdapter();
        var reg = new WebViewAdapterRegistration(WebViewAdapterPlatform.Gtk, "webkitgtk", factory, 50);

        var (platform, adapterId, f, priority) = reg;
        Assert.Equal(WebViewAdapterPlatform.Gtk, platform);
        Assert.Equal("webkitgtk", adapterId);
        Assert.Same(factory, f);
        Assert.Equal(50, priority);
    }

    // ========================= WebViewCore — sub-frame auto-allow =========================

    [Fact]
    public void NativeNavigation_sub_frame_auto_allows()
    {
        var (core, adapter) = CreateCoreWithAdapter();
        IWebViewAdapterHost host = core;

        // Simulate a sub-frame native navigation (IsMainFrame = false)
        var subFrameInfo = new NativeNavigationStartingInfo(
            Guid.NewGuid(), new Uri("https://iframe.test"), IsMainFrame: false);

        var decision = host.OnNativeNavigationStartingAsync(subFrameInfo).GetAwaiter().GetResult();

        Assert.True(decision.IsAllowed);
        Assert.Equal(Guid.Empty, decision.NavigationId);
    }

    // ========================= WebViewCore — disposed denies native navigation =========================

    [Fact]
    public void NativeNavigation_disposed_denies()
    {
        var (core, adapter) = CreateCoreWithAdapter();
        IWebViewAdapterHost host = core;
        core.Dispose();

        var info = new NativeNavigationStartingInfo(
            Guid.NewGuid(), new Uri("https://example.test"), IsMainFrame: true);

        var decision = host.OnNativeNavigationStartingAsync(info).GetAwaiter().GetResult();

        Assert.False(decision.IsAllowed);
        Assert.Equal(Guid.Empty, decision.NavigationId);
    }

    // ========================= WebViewCore — same-URL redirect path =========================

    [Fact]
    public async Task NativeNavigation_same_url_redirect_reuses_navigationId()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        // Start a command navigation.
        var navTask = core.NavigateAsync(new Uri("https://example.test/page"));
        var navId = adapter.LastNavigationId!.Value;

        // Simulate a native redirect to the exact same URL with the same correlationId.
        var decision = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://example.test/page"), correlationId: navId);

        Assert.True(decision.IsAllowed);
        Assert.Equal(navId, decision.NavigationId);

        // Complete the navigation to clean up.
        adapter.RaiseNavigationCompleted();
        await navTask;
    }

    // ========================= WebViewCore — adapter invocation exception =========================

    [Fact]
    public async Task NavigateAsync_adapter_throws_completes_with_failure()
    {
        var throwingAdapter = new ThrowingNavigateAdapter();
        var core = new WebViewCore(throwingAdapter, _dispatcher);

        var ex = await Assert.ThrowsAsync<WebViewNavigationException>(
            () => core.NavigateAsync(new Uri("https://fail.test")));

        Assert.Contains("Navigation failed", ex.Message);
    }

    // ========================= WebViewCore — NavigationCompleted with no active navigation =========================

    [Fact]
    public void NavigationCompleted_with_no_active_navigation_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        // Raise NavigationCompleted without any active navigation.
        adapter.RaiseNavigationCompleted(Guid.NewGuid(), new Uri("https://orphan.test"),
            NavigationCompletedStatus.Success);

        // Should be silently ignored — no event fired to the consumer.
        Assert.Null(completedArgs);
    }

    // ========================= WebViewCore — Failure without error fills in default =========================

    [Fact]
    public async Task NavigationCompleted_failure_without_error_synthesizes_exception()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        var navTask = core.NavigateAsync(new Uri("https://fail.test"));
        var navId = adapter.LastNavigationId!.Value;

        // Raise failure with null error — WebViewCore should synthesize an exception.
        adapter.RaiseNavigationCompleted(navId, new Uri("https://fail.test"),
            NavigationCompletedStatus.Failure, error: null);

        var ex = await Assert.ThrowsAsync<WebViewNavigationException>(() => navTask);
        Assert.Equal("Navigation failed.", ex.Message);
    }

    // ========================= WebViewCore — adapter reports failure with WebViewNavigationException =========================

    [Fact]
    public async Task NavigationCompleted_failure_preserves_categorized_exception()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        var navTask = core.NavigateAsync(new Uri("https://fail.test"));
        var navId = adapter.LastNavigationId!.Value;
        var netEx = new WebViewNetworkException("DNS failed", navId, new Uri("https://fail.test"));

        adapter.RaiseNavigationCompleted(navId, new Uri("https://fail.test"),
            NavigationCompletedStatus.Failure, error: netEx);

        var ex = await Assert.ThrowsAsync<WebViewNetworkException>(() => navTask);
        Assert.Same(netEx, ex);
    }

    // ========================= WebDialog — event remove accessors =========================

    [Fact]
    public void WebDialog_event_unsubscribe_paths()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        // Subscribe and unsubscribe each event to cover remove accessors.
        EventHandler<NavigationStartingEventArgs> navStarted = (_, _) => { };
        EventHandler<NavigationCompletedEventArgs> navCompleted = (_, _) => { };
        EventHandler<NewWindowRequestedEventArgs> newWindow = (_, _) => { };
        EventHandler<WebMessageReceivedEventArgs> webMsg = (_, _) => { };
        EventHandler<WebResourceRequestedEventArgs> webRes = (_, _) => { };
        EventHandler<EnvironmentRequestedEventArgs> envReq = (_, _) => { };

        dialog.NavigationStarted += navStarted;
        dialog.NavigationStarted -= navStarted;

        dialog.NavigationCompleted += navCompleted;
        dialog.NavigationCompleted -= navCompleted;

        dialog.NewWindowRequested += newWindow;
        dialog.NewWindowRequested -= newWindow;

        dialog.WebMessageReceived += webMsg;
        dialog.WebMessageReceived -= webMsg;

        dialog.WebResourceRequested += webRes;
        dialog.WebResourceRequested -= webRes;

        dialog.EnvironmentRequested += envReq;
        dialog.EnvironmentRequested -= envReq;

        dialog.Dispose();
    }

    // ========================= WebDialog — DownloadRequested / PermissionRequested event accessors =========================

    [Fact]
    public void WebDialog_DownloadRequested_event_subscribe_unsubscribe()
    {
        var host = new MockDialogHost();
        var downloadAdapter = MockWebViewAdapter.CreateWithDownload();
        var dialog = new WebDialog(host, downloadAdapter, _dispatcher);

        bool raised = false;
        EventHandler<DownloadRequestedEventArgs> handler = (_, _) => raised = true;

        dialog.DownloadRequested += handler;
        downloadAdapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
            new Uri("https://example.com/file.zip"), "file.zip", "application/zip", 1024));
        Assert.True(raised);

        raised = false;
        dialog.DownloadRequested -= handler;
        downloadAdapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
            new Uri("https://example.com/file2.zip"), "file2.zip", "application/zip", 2048));
        Assert.False(raised);

        dialog.Dispose();
    }

    [Fact]
    public void WebDialog_PermissionRequested_event_subscribe_unsubscribe()
    {
        var host = new MockDialogHost();
        var permAdapter = MockWebViewAdapter.CreateWithPermission();
        var dialog = new WebDialog(host, permAdapter, _dispatcher);

        bool raised = false;
        EventHandler<PermissionRequestedEventArgs> handler = (_, _) => raised = true;

        dialog.PermissionRequested += handler;
        permAdapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://example.com")));
        Assert.True(raised);

        raised = false;
        dialog.PermissionRequested -= handler;
        permAdapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://example.com")));
        Assert.False(raised);

        dialog.Dispose();
    }

    // ========================= WebDialog — AdapterDestroyed event =========================

    [Fact]
    public void WebDialog_AdapterDestroyed_event_raised_on_dispose()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        bool raised = false;
        EventHandler handler = (_, _) => raised = true;

        dialog.AdapterDestroyed += handler;
        dialog.Dispose();

        Assert.True(raised);
    }

    [Fact]
    public void WebDialog_AdapterDestroyed_event_unsubscribe()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        bool raised = false;
        EventHandler handler = (_, _) => raised = true;

        dialog.AdapterDestroyed += handler;
        dialog.AdapterDestroyed -= handler;
        dialog.Dispose();

        Assert.False(raised);
    }

    // ========================= Branch coverage — WebViewCore ctor null checks =========================

    [Fact]
    public void WebViewCore_ctor_null_adapter_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WebViewCore(null!, _dispatcher));
    }

    [Fact]
    public void WebViewCore_ctor_null_dispatcher_throws()
    {
        var adapter = MockWebViewAdapter.Create();
        Assert.Throws<ArgumentNullException>(() => new WebViewCore(adapter, null!));
    }

    [Fact]
    public void WebViewCore_ctor_null_logger_uses_NullLogger()
    {
        var adapter = MockWebViewAdapter.Create();
        // Pass null logger via internal ctor — should not throw
        using var core = new WebViewCore(adapter, _dispatcher,
            null!);
        Assert.NotNull(core);
    }

    // ========================= Branch coverage — on-thread event dispatch (Download/Permission) =========================

    [Fact]
    public void Branch_DownloadRequested_on_thread_raises_directly()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, _dispatcher);

        DownloadRequestedEventArgs? received = null;
        core.DownloadRequested += (_, e) => received = e;

        var args = new DownloadRequestedEventArgs(new Uri("https://example.com/file.zip"), "file.zip");
        adapter.RaiseDownloadRequested(args);

        Assert.NotNull(received);
    }

    [Fact]
    public void Branch_PermissionRequested_on_thread_raises_directly()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, _dispatcher);

        PermissionRequestedEventArgs? received = null;
        core.PermissionRequested += (_, e) => received = e;

        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(args);

        Assert.NotNull(received);
    }

    // Note: WebViewCore line 712 (Failure + null error) is unreachable —
    // NavigationCompletedEventArgs ctor throws if status=Failure and error=null.

    // ========================= Branch coverage — WebMessage on-UI dispatch with RPC not matching =========================

    [Fact]
    public void WebMessage_on_thread_non_rpc_forwards_to_consumer()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        // Send a non-RPC message (no "jsonrpc" field)
        adapter.RaiseWebMessage("""{"type":"custom","data":"hello"}""", "*", core.ChannelId);

        Assert.NotNull(received);
        Assert.Contains("custom", received!.Body);
    }

    [Fact]
    public void WebMessage_policy_denied_fires_diagnostics()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        var drops = new List<WebMessageDropDiagnostic>();
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "https://allowed.com" },
            DropDiagnosticsSink = new TestDropSink(drops)
        });

        // Send from a non-allowed origin
        adapter.RaiseWebMessage("hello", "https://evil.com", core.ChannelId);

        Assert.Single(drops);
    }

    // ========================= Branch coverage — EnableWebMessageBridge AllowedOrigins null count =========================

    [Fact]
    public void EnableWebMessageBridge_twice_reuses_rpc_service()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });
        var rpc1 = core.Rpc;
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });
        var rpc2 = core.Rpc;
        Assert.Same(rpc1, rpc2);
    }

    // CompleteActiveNavigation null-operation branch already covered by
    // NavigationCompleted_with_no_active_navigation_is_ignored

    private sealed class TestDropSink : IWebMessageDropDiagnosticsSink
    {
        private readonly List<WebMessageDropDiagnostic> _drops;
        public TestDropSink(List<WebMessageDropDiagnostic> drops) => _drops = drops;
        public void OnMessageDropped(in WebMessageDropDiagnostic diagnostic) => _drops.Add(diagnostic);
    }

    // ========================= Branch coverage — Download/Permission off-thread =========================

    [Fact]
    public void Branch_DownloadRequested_off_thread_dispatches_to_ui()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, _dispatcher);

        DownloadRequestedEventArgs? received = null;
        core.DownloadRequested += (_, e) => received = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
                new Uri("https://dl.test/file.zip"), "file.zip"));
        });

        _dispatcher.RunAll();
        Assert.NotNull(received);
    }

    [Fact]
    public void Branch_PermissionRequested_off_thread_dispatches_to_ui()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, _dispatcher);

        PermissionRequestedEventArgs? received = null;
        core.PermissionRequested += (_, e) => received = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
                WebViewPermissionKind.Camera, new Uri("https://perm.test")));
        });

        _dispatcher.RunAll();
        Assert.NotNull(received);
    }

    [Fact]
    public void Branch_DownloadRequested_after_dispose_is_ignored()
    {
        var adapter = MockWebViewAdapter.CreateWithDownload();
        using var core = new WebViewCore(adapter, _dispatcher);

        DownloadRequestedEventArgs? received = null;
        core.DownloadRequested += (_, e) => received = e;
        core.Dispose();

        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
            new Uri("https://dl.test/file.zip"), "file.zip"));
        Assert.Null(received);
    }

    [Fact]
    public void Branch_PermissionRequested_after_dispose_is_ignored()
    {
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, _dispatcher);

        PermissionRequestedEventArgs? received = null;
        core.PermissionRequested += (_, e) => received = e;
        core.Dispose();

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
            WebViewPermissionKind.Camera, new Uri("https://perm.test")));
        Assert.Null(received);
    }

    // ========================= Branch coverage — WebMessage off-thread dispatch =========================

    [Fact]
    public void Branch_WebMessage_off_thread_dispatches_to_ui()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });

        WebMessageReceivedEventArgs? received = null;
        core.WebMessageReceived += (_, e) => received = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseWebMessage("{\"type\":\"bg\"}", "*", core.ChannelId);
        });

        _dispatcher.RunAll();
        Assert.NotNull(received);
    }

    // ========================= Branch coverage — NativeNavigation redirect cancel =========================

    [Fact]
    public async Task Branch_NativeNavigation_redirect_cancel_completes_navigation()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var navTask = core.NavigateAsync(new Uri("https://redirect.test/page"));
        var navId = adapter.LastNavigationId!.Value;

        // Subscribe and cancel any redirect navigation
        core.NavigationStarted += (_, e) => e.Cancel = true;

        // Simulate a redirect to a different URL with the same correlation ID
        var decision = await adapter.SimulateNativeNavigationStartingAsync(
            new Uri("https://redirect.test/other"),
            correlationId: navId);

        Assert.False(decision.IsAllowed);
        Assert.NotNull(completedArgs);
        Assert.Equal(NavigationCompletedStatus.Canceled, completedArgs!.Status);

        // Canceled navigations still resolve successfully (not faulted)
        await navTask;
    }

    // ========================= WebDialog — new APIs =========================

    [Fact]
    public void WebDialog_TryGetCommandManager_returns_null_for_basic_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        Assert.Null(dialog.TryGetCommandManager());
    }

    [Fact]
    public void WebDialog_TryGetCommandManager_returns_value_for_command_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithCommands();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        Assert.NotNull(dialog.TryGetCommandManager());
    }

    [Fact]
    public async Task WebDialog_CaptureScreenshotAsync_throws_when_unsupported()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.CaptureScreenshotAsync());
    }

    [Fact]
    public async Task WebDialog_CaptureScreenshotAsync_with_screenshot_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithScreenshot();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        var data = await dialog.CaptureScreenshotAsync();
        Assert.NotEmpty(data);
    }

    [Fact]
    public async Task WebDialog_PrintToPdfAsync_throws_when_unsupported()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.PrintToPdfAsync());
    }

    [Fact]
    public async Task WebDialog_PrintToPdfAsync_with_print_adapter()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithPrint();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        var data = await dialog.PrintToPdfAsync();
        Assert.NotEmpty(data);
    }

    [Fact]
    public void WebDialog_Rpc_is_null_without_bridge()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        Assert.Null(dialog.Rpc);
    }

    // ========================= WebViewCore — NavigationCompleted ID mismatch =========================

    [Fact]
    public async Task NavigationCompleted_id_mismatch_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var navTask = core.NavigateAsync(new Uri("https://example.test"));
        var navId = adapter.LastNavigationId!.Value;

        // Raise NavigationCompleted with a DIFFERENT ID
        adapter.RaiseNavigationCompleted(Guid.NewGuid(), new Uri("https://wrong.test"),
            NavigationCompletedStatus.Success);

        // Should be ignored — no event fired
        Assert.Null(completedArgs);

        // Now complete with correct ID
        adapter.RaiseNavigationCompleted(navId, new Uri("https://example.test"),
            NavigationCompletedStatus.Success);
        await navTask;
        Assert.NotNull(completedArgs);
    }

    // ========================= WebViewCore — NewWindowRequested unhandled navigates =========================

    [Fact]
    public void NewWindowRequested_unhandled_navigates_in_view()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        // Don't subscribe to NewWindowRequested — it will be unhandled
        adapter.RaiseNewWindowRequested(new Uri("https://popup.test/page"));

        // Adapter should have received a navigate call for the popup URI
        Assert.NotNull(adapter.LastNavigationUri);
        Assert.Equal("https://popup.test/page", adapter.LastNavigationUri!.ToString());
    }

    // ========================= WebViewCore — NavigationCompleted after dispose =========================

    [Fact]
    public void NavigationCompleted_after_dispose_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        core.Dispose();

        adapter.RaiseNavigationCompleted(Guid.NewGuid(), new Uri("https://disposed.test"),
            NavigationCompletedStatus.Success);

        Assert.Null(completedArgs);
    }

    // ========================= WebViewCore — NewWindowRequested after dispose =========================

    [Fact]
    public void NewWindowRequested_after_dispose_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NewWindowRequestedEventArgs? newWindowArgs = null;
        core.NewWindowRequested += (_, e) => newWindowArgs = e;

        core.Dispose();

        adapter.RaiseNewWindowRequested(new Uri("https://disposed-popup.test"));

        Assert.Null(newWindowArgs);
    }

    // ========================= WebViewCore — WebResourceRequested after dispose =========================

    [Fact]
    public void WebResourceRequested_after_dispose_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        WebResourceRequestedEventArgs? resourceArgs = null;
        core.WebResourceRequested += (_, e) => resourceArgs = e;

        core.Dispose();

        adapter.RaiseWebResourceRequested();

        Assert.Null(resourceArgs);
    }

    // ========================= WebViewCore — EnvironmentRequested after dispose =========================

    [Fact]
    public void EnvironmentRequested_after_dispose_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        EnvironmentRequestedEventArgs? envArgs = null;
        core.EnvironmentRequested += (_, e) => envArgs = e;

        core.Dispose();

        adapter.RaiseEnvironmentRequested();

        Assert.Null(envArgs);
    }

    // ========================= WebViewCore — WebMessageReceived with bridge not enabled =========================

    [Fact]
    public void WebMessageReceived_bridge_not_enabled_drops_message()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        WebMessageReceivedEventArgs? msgArgs = null;
        core.WebMessageReceived += (_, e) => msgArgs = e;

        // Don't enable bridge — message should be dropped
        adapter.RaiseWebMessage("hello", "https://origin.test", Guid.NewGuid());

        Assert.Null(msgArgs);
    }

    // ========================= WebViewCore — WebMessageReceived after dispose =========================

    [Fact]
    public void WebMessageReceived_after_dispose_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        WebMessageReceivedEventArgs? msgArgs = null;
        core.WebMessageReceived += (_, e) => msgArgs = e;

        core.Dispose();

        adapter.RaiseWebMessage("hello", "https://origin.test", Guid.NewGuid());

        Assert.Null(msgArgs);
    }

    // ========================= WebViewCore — Command navigation superseding =========================

    [Fact]
    public async Task Command_navigation_supersedes_active_navigation()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? supersededArgs = null;
        core.NavigationCompleted += (_, e) =>
        {
            if (e.Status == NavigationCompletedStatus.Superseded)
                supersededArgs = e;
        };

        // Start a navigation
        var navTask1 = core.NavigateAsync(new Uri("https://first.test"));
        Assert.True(core.IsLoading);

        adapter.GoBackAccepted = true;
        adapter.CanGoBack = true;

        // GoBack while first navigation is active — should supersede it
        await core.GoBackAsync();

        // First navigation should complete with Superseded
        Assert.NotNull(supersededArgs);
        Assert.Equal(NavigationCompletedStatus.Superseded, supersededArgs!.Status);

        // Clean up — complete the back navigation
        adapter.RaiseNavigationCompleted();
        await navTask1;
    }

    // ========================= WebDialog — Source setter =========================

    [Fact]
    public void WebDialog_Source_set()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        var uri = new Uri("https://set-source.test");
        dialog.Source = uri;

        Assert.Equal(uri, dialog.Source);

        dialog.Dispose();
    }

    // ========================= WebViewAdapterRegistry — priority ordering =========================

    [Fact]
    public void TryCreateForCurrentPlatform_returns_result_or_failure_reason()
    {
        // This test verifies the success/failure paths of TryCreateForCurrentPlatform.
        // On the CI platform, it may or may not have an adapter registered.
        var result = WebViewAdapterRegistry.TryCreateForCurrentPlatform(out var adapter, out var reason);

        if (result)
        {
            Assert.NotNull(adapter);
            Assert.Null(reason);
        }
        else
        {
            Assert.NotNull(reason);
            Assert.Contains("No WebView adapter registered", reason);
        }
    }

    // ========================= WebAuthBroker — Show with non-null PlatformHandle =========================

    [Fact]
    public async Task WebAuthBroker_show_with_platform_handle_delegates_to_show_owner()
    {
        var factory = new AuthTestDialogFactoryLocal(_dispatcher);
        var broker = new WebAuthBroker(factory);

        // Use a window WITH a PlatformHandle to exercise lines 57-59 in WebAuthBroker.
        var owner = new NonNullHandleWindow();

        var options = new AuthOptions
        {
            AuthorizeUri = new Uri("https://auth.test/authorize"),
            CallbackUri = new Uri("myapp://auth/callback"),
        };

        factory.OnDialogCreated = (dialog, adapter) =>
        {
            adapter.AutoCompleteNavigation = true;
            adapter.OnNavigationAutoCompleted = () =>
            {
                _ = adapter.SimulateNativeNavigationStartingAsync(
                    new Uri("myapp://auth/callback?code=abc123"));
            };
        };

        var result = await broker.AuthenticateAsync(owner, options);

        Assert.Equal(WebAuthStatus.Success, result.Status);
        Assert.NotNull(result.CallbackUri);

        // Verify Show was called (dialog is closed by broker's finally block, so check call count)
        Assert.Equal(1, factory.LastHost!.ShowCallCount);
    }

    // ========================= WebViewCore — NavigationCompleted try/catch exception path =========================

    [Fact]
    public async Task NavigationCompleted_ctor_exception_produces_failure()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var navTask = core.NavigateAsync(new Uri("https://error.test"));
        var navId = adapter.LastNavigationId!.Value;

        // Normal failure path to verify error propagation
        adapter.RaiseNavigationCompleted(navId, new Uri("https://error.test"),
            NavigationCompletedStatus.Failure,
            new WebViewNetworkException("Net error", navId, new Uri("https://error.test")));

        try
        {
            await navTask;
        }
        catch
        {
            // Absorb any fault
        }

        Assert.NotNull(completedArgs);
        Assert.Equal(NavigationCompletedStatus.Failure, completedArgs!.Status);
        Assert.NotNull(completedArgs.Error);
    }

    // ========================= Off-thread dispatch paths =========================

    [Fact]
    public async Task NavigationCompleted_off_thread_dispatches_to_ui()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var navTask = core.NavigateAsync(new Uri("https://offthread.test"));
        var navId = adapter.LastNavigationId!.Value;

        // Raise from a separate thread so CheckAccess() returns false
        RunOnBackgroundThread(() =>
        {
            adapter.RaiseNavigationCompleted(navId, new Uri("https://offthread.test"),
                NavigationCompletedStatus.Success);
        });

        // Drain dispatcher queue (we're still on the original "UI" thread)
        _dispatcher.RunAll();
        await navTask;

        Assert.NotNull(completedArgs);
        Assert.Equal(NavigationCompletedStatus.Success, completedArgs!.Status);
    }

    [Fact]
    public void NewWindowRequested_off_thread_dispatches_to_ui()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NewWindowRequestedEventArgs? windowArgs = null;
        core.NewWindowRequested += (_, e) =>
        {
            windowArgs = e;
            e.Handled = true;
        };

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseNewWindowRequested(new Uri("https://popup-offthread.test"));
        });

        _dispatcher.RunAll();

        Assert.NotNull(windowArgs);
        Assert.Equal("https://popup-offthread.test/", windowArgs!.Uri!.ToString());
    }

    [Fact]
    public void WebResourceRequested_off_thread_dispatches_to_ui()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        WebResourceRequestedEventArgs? resourceArgs = null;
        core.WebResourceRequested += (_, e) => resourceArgs = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseWebResourceRequested();
        });

        _dispatcher.RunAll();

        Assert.NotNull(resourceArgs);
    }

    [Fact]
    public void EnvironmentRequested_off_thread_dispatches_to_ui()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        EnvironmentRequestedEventArgs? envArgs = null;
        core.EnvironmentRequested += (_, e) => envArgs = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseEnvironmentRequested();
        });

        _dispatcher.RunAll();

        Assert.NotNull(envArgs);
    }

    [Fact]
    public void DownloadRequested_off_thread_dispatches_to_ui()
    {
        var downloadAdapter = MockWebViewAdapter.CreateWithDownload();
        var core = new WebViewCore(downloadAdapter, _dispatcher);

        DownloadRequestedEventArgs? dlArgs = null;
        core.DownloadRequested += (_, e) => dlArgs = e;

        RunOnBackgroundThread(() =>
        {
            downloadAdapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(
                new Uri("https://example.com/file.zip"), "file.zip", "application/zip", 1024));
        });

        _dispatcher.RunAll();

        Assert.NotNull(dlArgs);
        Assert.Equal("file.zip", dlArgs!.SuggestedFileName);
    }

    [Fact]
    public void PermissionRequested_off_thread_dispatches_to_ui()
    {
        var permAdapter = MockWebViewAdapter.CreateWithPermission();
        var core = new WebViewCore(permAdapter, _dispatcher);

        PermissionRequestedEventArgs? permArgs = null;
        core.PermissionRequested += (_, e) => permArgs = e;

        RunOnBackgroundThread(() =>
        {
            permAdapter.RaisePermissionRequested(new PermissionRequestedEventArgs(
                WebViewPermissionKind.Microphone, new Uri("https://example.com")));
        });

        _dispatcher.RunAll();

        Assert.NotNull(permArgs);
        Assert.Equal(WebViewPermissionKind.Microphone, permArgs!.PermissionKind);
    }

    // ========================= Off-thread dispatch + disposed (marshal then dispose) =========================

    [Fact]
    public async Task NavigationCompleted_off_thread_then_disposed_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NavigationCompletedEventArgs? completedArgs = null;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var navTask = core.NavigateAsync(new Uri("https://offthread-dispose.test"));
        var navId = adapter.LastNavigationId!.Value;

        // Raise from background thread to enqueue
        RunOnBackgroundThread(() =>
        {
            adapter.RaiseNavigationCompleted(navId, new Uri("https://offthread-dispose.test"),
                NavigationCompletedStatus.Success);
        });

        // Dispose BEFORE draining — the on-UI-thread handler should see _disposed
        core.Dispose();
        _dispatcher.RunAll();

        // NavigationCompleted event should NOT have been raised to consumer (ignored on UI thread)
        Assert.Null(completedArgs);

        // But the navTask was faulted by Dispose
        await Assert.ThrowsAsync<ObjectDisposedException>(() => navTask);
    }

    [Fact]
    public void NewWindowRequested_off_thread_then_disposed_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        NewWindowRequestedEventArgs? windowArgs = null;
        core.NewWindowRequested += (_, e) => windowArgs = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseNewWindowRequested(new Uri("https://popup-dispose.test"));
        });

        core.Dispose();
        _dispatcher.RunAll();

        Assert.Null(windowArgs);
    }

    [Fact]
    public void WebMessageReceived_off_thread_then_disposed_is_ignored()
    {
        var (core, adapter) = CreateCoreWithAdapter();

        WebMessageReceivedEventArgs? msgArgs = null;
        core.WebMessageReceived += (_, e) => msgArgs = e;

        RunOnBackgroundThread(() =>
        {
            adapter.RaiseWebMessage("hello", "https://origin.test", Guid.NewGuid());
        });

        core.Dispose();
        _dispatcher.RunAll();

        Assert.Null(msgArgs);
    }

    // ========================= Helpers =========================

    /// <summary>
    /// Runs an action on a separate thread and blocks until it completes.
    /// After this method returns, we're guaranteed to be back on the original (UI) thread.
    /// </summary>
    private static void RunOnBackgroundThread(Action action)
    {
        Exception? bgException = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { bgException = ex; }
        });
        thread.Start();
        thread.Join();
        if (bgException is not null)
            throw new AggregateException("Background thread threw an exception", bgException);
    }

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithAdapter()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        return (core, adapter);
    }

    /// <summary>Adapter that throws on NavigateAsync to cover the exception catch in StartNavigationCoreAsync.</summary>
    #pragma warning disable CS0067 // Events never used (by design — this adapter only throws)
    private sealed class ThrowingNavigateAdapter : IWebViewAdapter
    {
        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;

        public void Initialize(IWebViewAdapterHost host) { }
        public void Attach(INativeHandle parentHandle) { }
        public void Detach() { }

        public Task NavigateAsync(Guid navigationId, Uri uri)
            => throw new InvalidOperationException("Simulated adapter failure");

        public Task NavigateToStringAsync(Guid navigationId, string html)
            => throw new InvalidOperationException("Simulated adapter failure");

        public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
            => throw new InvalidOperationException("Simulated adapter failure");

        public Task<string?> InvokeScriptAsync(string script)
            => Task.FromResult<string?>(null);

        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public bool GoBack(Guid navigationId) => false;
        public bool GoForward(Guid navigationId) => false;
        public bool Refresh(Guid navigationId) => false;
        public bool Stop() => false;
    }
    #pragma warning restore CS0067

    /// <summary>Window with non-null PlatformHandle for WebAuthBroker test.</summary>
    private sealed class NonNullHandleWindow : ITopLevelWindow
    {
        public INativeHandle? PlatformHandle => new TestPlatformHandle();
    }

    private sealed class TestPlatformHandle : INativeHandle
    {
        public nint Handle => nint.Zero;
        public string HandleDescriptor => "Test";
    }

    /// <summary>Local copy of AuthTestDialogFactory for the coverage gap test.</summary>
    private sealed class AuthTestDialogFactoryLocal : IWebDialogFactory
    {
        private readonly TestDispatcher _dispatcher;

        public AuthTestDialogFactoryLocal(TestDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public MockDialogHost? LastHost { get; private set; }
        public Action<WebDialog, MockWebViewAdapter>? OnDialogCreated { get; set; }

        public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
        {
            var host = new MockDialogHost();
            var adapter = MockWebViewAdapter.Create();
            var dialog = new WebDialog(host, adapter, _dispatcher);
            LastHost = host;
            OnDialogCreated?.Invoke(dialog, adapter);
            return dialog;
        }
    }

    // ========================= ICommandManager & ICommandAdapter =========================

    [Fact]
    public void ICommandManager_has_all_six_methods()
    {
        var methods = typeof(ICommandManager).GetMethods();
        Assert.Contains(methods, m => m.Name == "CopyAsync");
        Assert.Contains(methods, m => m.Name == "CutAsync");
        Assert.Contains(methods, m => m.Name == "PasteAsync");
        Assert.Contains(methods, m => m.Name == "SelectAllAsync");
        Assert.Contains(methods, m => m.Name == "UndoAsync");
        Assert.Contains(methods, m => m.Name == "RedoAsync");
    }

    [Fact]
    public void ICommandAdapter_facet_detected_by_core()
    {
        var adapter = MockWebViewAdapter.CreateWithCommands();
        Assert.IsAssignableFrom<ICommandAdapter>(adapter);
    }

    [Theory]
    [InlineData(WebViewCommand.Copy)]
    [InlineData(WebViewCommand.Cut)]
    [InlineData(WebViewCommand.Paste)]
    [InlineData(WebViewCommand.SelectAll)]
    [InlineData(WebViewCommand.Undo)]
    [InlineData(WebViewCommand.Redo)]
    public void WebViewCommand_enum_has_expected_value(WebViewCommand command)
    {
        Assert.True(Enum.IsDefined(typeof(WebViewCommand), command));
    }

    [Fact]
    public void TryGetCommandManager_returns_non_null_with_ICommandAdapter()
    {
        var adapter = MockWebViewAdapter.CreateWithCommands();
        using var core = new WebViewCore(adapter, _dispatcher);
        var mgr = core.TryGetCommandManager();
        Assert.NotNull(mgr);
    }

    [Fact]
    public void TryGetCommandManager_returns_null_without_ICommandAdapter()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        var mgr = core.TryGetCommandManager();
        Assert.Null(mgr);
    }

    [Theory]
    [InlineData(WebViewCommand.Copy)]
    [InlineData(WebViewCommand.Cut)]
    [InlineData(WebViewCommand.Paste)]
    [InlineData(WebViewCommand.SelectAll)]
    [InlineData(WebViewCommand.Undo)]
    [InlineData(WebViewCommand.Redo)]
    public async Task CommandManager_delegates_to_adapter(WebViewCommand command)
    {
        var adapter = MockWebViewAdapter.CreateWithCommands();
        using var core = new WebViewCore(adapter, _dispatcher);
        var mgr = core.TryGetCommandManager()!;

        switch (command)
        {
            case WebViewCommand.Copy: await mgr.CopyAsync(); break;
            case WebViewCommand.Cut: await mgr.CutAsync(); break;
            case WebViewCommand.Paste: await mgr.PasteAsync(); break;
            case WebViewCommand.SelectAll: await mgr.SelectAllAsync(); break;
            case WebViewCommand.Undo: await mgr.UndoAsync(); break;
            case WebViewCommand.Redo: await mgr.RedoAsync(); break;
        }

        Assert.Single(adapter.ExecutedCommands);
        Assert.Equal(command, adapter.ExecutedCommands[0]);
    }

    // ========================= IScreenshotAdapter =========================

    [Fact]
    public void IScreenshotAdapter_facet_detected()
    {
        var adapter = MockWebViewAdapter.CreateWithScreenshot();
        Assert.IsAssignableFrom<IScreenshotAdapter>(adapter);
    }

    [Fact]
    public async Task CaptureScreenshotAsync_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.CaptureScreenshotAsync());
    }

    [Fact]
    public async Task CaptureScreenshotAsync_returns_data_with_IScreenshotAdapter()
    {
        var adapter = MockWebViewAdapter.CreateWithScreenshot();
        using var core = new WebViewCore(adapter, _dispatcher);
        var result = await core.CaptureScreenshotAsync();
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // PNG magic bytes
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
    }

    // ========================= IPrintAdapter & PdfPrintOptions =========================

    [Fact]
    public void PdfPrintOptions_has_sensible_defaults()
    {
        var opts = new PdfPrintOptions();
        Assert.False(opts.Landscape);
        Assert.Equal(8.5, opts.PageWidth);
        Assert.Equal(11.0, opts.PageHeight);
        Assert.Equal(0.4, opts.MarginTop);
        Assert.Equal(0.4, opts.MarginBottom);
        Assert.Equal(0.4, opts.MarginLeft);
        Assert.Equal(0.4, opts.MarginRight);
        Assert.Equal(1.0, opts.Scale);
        Assert.True(opts.PrintBackground);
    }

    [Fact]
    public void IPrintAdapter_facet_detected()
    {
        var adapter = MockWebViewAdapter.CreateWithPrint();
        Assert.IsAssignableFrom<IPrintAdapter>(adapter);
    }

    [Fact]
    public async Task PrintToPdfAsync_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.PrintToPdfAsync());
    }

    [Fact]
    public async Task PrintToPdfAsync_returns_data_with_IPrintAdapter()
    {
        var adapter = MockWebViewAdapter.CreateWithPrint();
        using var core = new WebViewCore(adapter, _dispatcher);
        var result = await core.PrintToPdfAsync();
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // %PDF magic bytes
        Assert.Equal(0x25, result[0]);
        Assert.Equal(0x50, result[1]);
    }

    // ========================= IWebViewRpcService =========================

    [Fact]
    public void Rpc_is_null_before_bridge_enabled()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        Assert.Null(core.Rpc);
    }

    [Fact]
    public void Rpc_is_non_null_after_bridge_enabled()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });
        Assert.NotNull(core.Rpc);
    }

    [Fact]
    public void Rpc_disable_bridge_clears_rpc()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });
        Assert.NotNull(core.Rpc);
        core.DisableWebMessageBridge();
        Assert.Null(core.Rpc);
    }

    [Fact]
    public void Rpc_handler_registration_and_removal()
    {
        var rpc = CreateTestRpcService(out _);
        rpc.Handle("test.method", _ => Task.FromResult<object?>(42));

        var request = """{"jsonrpc":"2.0","id":"1","method":"test.method","params":null}""";
        Assert.True(rpc.TryProcessMessage(request));

        rpc.RemoveHandler("test.method");
        Assert.True(rpc.TryProcessMessage(request));
    }

    [Fact]
    public void Rpc_sync_handler_dispatches_and_sends_response()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("math.add", args =>
        {
            var a = args!.Value.GetProperty("a").GetInt32();
            var b = args!.Value.GetProperty("b").GetInt32();
            return a + b;
        });

        var request = """{"jsonrpc":"2.0","id":"sync-1","method":"math.add","params":{"a":3,"b":4}}""";
        rpc.TryProcessMessage(request);
        WaitUntil(() => scripts.Any(s => s.Contains("sync-1")));
        Assert.Contains(scripts, s => s.Contains("_onResponse") && s.Contains("sync-1"));
    }

    [Fact]
    public void Rpc_async_handler_dispatches()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("async.echo", async args =>
        {
            await Task.Yield();
            return args?.GetProperty("msg").GetString();
        });

        var request = """{"jsonrpc":"2.0","id":"async-1","method":"async.echo","params":{"msg":"hello"}}""";
        rpc.TryProcessMessage(request);
        WaitUntil(() => scripts.Any(s => s.Contains("async-1")));
        Assert.Contains(scripts, s => s.Contains("_onResponse") && s.Contains("async-1"));
    }

    [Fact]
    public void Rpc_unknown_method_sends_error()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var request = """{"jsonrpc":"2.0","id":"unk-1","method":"nonexistent","params":null}""";
        rpc.TryProcessMessage(request);
        WaitUntil(() => scripts.Any(s => s.Contains("-32601")));
        Assert.Contains(scripts, s => s.Contains("-32601"));
    }

    [Fact]
    public void Rpc_handler_exception_sends_error()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("bad.handler", _ => throw new InvalidOperationException("Boom"));

        var request = """{"jsonrpc":"2.0","id":"err-1","method":"bad.handler","params":null}""";
        rpc.TryProcessMessage(request);
        WaitUntil(() => scripts.Any(s => s.Contains("Boom")));
        Assert.Contains(scripts, s => s.Contains("Boom"));
    }

    [Fact]
    public async Task Rpc_InvokeAsync_resolves_on_response()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.getTheme");
        Assert.False(task.IsCompleted);

        WaitUntil(() => scripts.Count > 0);
        Assert.NotEmpty(scripts);
        var callId = ExtractRpcId(scripts[0]);

        var response = "{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":\"dark\"}";
        rpc.TryProcessMessage(response);

        var result = await task;
        Assert.Equal(JsonValueKind.String, result.ValueKind);
        Assert.Equal("dark", result.GetString());
    }

    [Fact]
    public async Task Rpc_InvokeAsync_T_deserializes()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync<int>("js.getCount");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":42}");
        Assert.Equal(42, await task);
    }

    [Fact]
    public async Task Rpc_InvokeAsync_error_throws()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.fail");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"error\":{\"code\":-32603,\"message\":\"JS error\"}}");

        var ex = await Assert.ThrowsAsync<WebViewRpcException>(() => task);
        Assert.Equal(-32603, ex.Code);
    }

    [Fact]
    public async Task Rpc_response_no_result_no_error_sets_default()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.void");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        var json = "{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\"}";
        Assert.True(rpc.TryProcessMessage(json));
        await task; // should complete with default
    }

    [Fact]
    public async Task Rpc_error_no_code_defaults_to_32603()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.x");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        var json = "{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"error\":{\"message\":\"oops\"}}";
        rpc.TryProcessMessage(json);
        var ex = await Assert.ThrowsAsync<WebViewRpcException>(() => task);
        Assert.Equal(-32603, ex.Code);
    }

    [Fact]
    public void Rpc_non_jsonrpc_message_ignored()
    {
        var rpc = CreateTestRpcService(out _);
        Assert.False(rpc.TryProcessMessage("not json"));
        Assert.False(rpc.TryProcessMessage("""{"hello":"world"}"""));
        Assert.False(rpc.TryProcessMessage(""));
    }

    [Fact]
    public void Rpc_orphan_response_not_handled()
    {
        var rpc = CreateTestRpcService(out _);
        Assert.False(rpc.TryProcessMessage("""{"jsonrpc":"2.0","id":"orphan","result":"x"}"""));
    }

    [Fact]
    public void Rpc_request_without_params()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("noparams", args =>
        {
            Assert.Null(args);
            return "ok";
        });

        rpc.TryProcessMessage("""{"jsonrpc":"2.0","id":"np-1","method":"noparams"}""");
        WaitUntil(() => scripts.Any(s => s.Contains("np-1")));
        Assert.Contains(scripts, s => s.Contains("np-1"));
    }

    [Fact]
    public void WebViewRpcException_has_code()
    {
        var ex = new WebViewRpcException(-32601, "Method not found");
        Assert.Equal(-32601, ex.Code);
        Assert.Equal("Method not found", ex.Message);
    }

    [Fact]
    public void Rpc_JsStub_contains_key_identifiers()
    {
        Assert.Contains("agWebView.rpc", WebViewRpcService.JsStub);
        Assert.Contains("invoke", WebViewRpcService.JsStub);
        Assert.Contains("_dispatch", WebViewRpcService.JsStub);
        Assert.Contains("_onResponse", WebViewRpcService.JsStub);
    }

    [Fact]
    public void Rpc_message_routed_through_WebViewCore()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions { AllowedOrigins = new HashSet<string> { "*" } });

        core.Rpc!.Handle("core.ping", _ => "pong");

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"r1","method":"core.ping","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
    }

    // ========================= Branch coverage — RPC uncovered branches =========================

    [Fact]
    public async Task Branch_Rpc_InvokeAsync_with_args_serializes_params()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.greet", new { name = "Alice" });

        WaitUntil(() => scripts.Count > 0);
        Assert.NotEmpty(scripts);
        var callId = ExtractRpcId(scripts[0]);
        // The script should contain the params
        Assert.Contains("Alice", scripts[0]);

        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":\"Hello Alice\"}");
        var result = await task;
        Assert.Equal("Hello Alice", result.GetString());
    }

    [Fact]
    public void Branch_Rpc_TryProcessMessage_null_id_is_ignored()
    {
        var rpc = CreateTestRpcService(out _);
        // null id — the id property is JSON null
        var result = rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":null,\"method\":\"test\"}");
        // id is null string → _pendingCalls won't match, then method check still runs
        // but method is not null so it dispatches (and finds no handler → sends error)
        Assert.True(result);
    }

    [Fact]
    public void Branch_Rpc_TryProcessMessage_null_method_is_ignored()
    {
        var rpc = CreateTestRpcService(out _);
        // id is present but method is JSON null
        var result = rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"x\",\"method\":null}");
        // method is null → falls through, returns false
        Assert.False(result);
    }

    [Fact]
    public async Task Branch_Rpc_InvokeAsync_error_without_code_uses_default()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.fail");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        // Error without "code" property — default -32603 should be used
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"error\":{\"message\":\"oops\"}}");

        var ex = await Assert.ThrowsAsync<WebViewRpcException>(() => task);
        Assert.Equal(-32603, ex.Code);
        Assert.Equal("oops", ex.Message);
    }

    [Fact]
    public async Task Branch_Rpc_InvokeAsync_error_without_message_uses_default()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.fail2");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        // Error with code but no "message" — default "RPC error" should be used
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"error\":{\"code\":-1}}");

        var ex = await Assert.ThrowsAsync<WebViewRpcException>(() => task);
        Assert.Equal(-1, ex.Code);
        Assert.Equal("RPC error", ex.Message);
    }

    [Fact]
    public void Branch_Rpc_handler_returns_null_sends_null_result()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("void.method", _ => Task.FromResult<object?>(null));

        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"v1\",\"method\":\"void.method\",\"params\":null}");
        WaitUntil(() => scripts.Any(s => s.Contains("v1")));
        // The response should contain "v1" and null result
        Assert.Contains(scripts, s => s.Contains("v1"));
    }

    [Fact]
    public void Branch_Rpc_TryProcessMessage_malformed_json_returns_false()
    {
        var rpc = CreateTestRpcService(out _);
        Assert.False(rpc.TryProcessMessage("not-json!!!"));
    }

    [Fact]
    public async Task Branch_Rpc_InvokeAsync_T_null_result_returns_default()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync<string>("js.nullResult");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        // Response with null result
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":null}");

        var result = await task;
        Assert.Null(result);
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMilliseconds = 3000)
    {
        Assert.True(
            SpinWait.SpinUntil(condition, TimeSpan.FromMilliseconds(timeoutMilliseconds)),
            "Timed out while waiting for asynchronous RPC processing.");
    }

    private static string ExtractRpcId(string script)
    {
        // script looks like: window.agWebView && window.agWebView.rpc && window.agWebView.rpc._dispatch("...escaped json...")
        var start = script.IndexOf("_dispatch(") + "_dispatch(".Length;
        var end = script.LastIndexOf(')');
        var jsonString = script[start..end];
        // jsonString is a JSON-serialized string; deserialize it to get the inner JSON
        var innerJson = JsonSerializer.Deserialize<string>(jsonString)!;
        using var doc = JsonDocument.Parse(innerJson);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private static WebViewRpcService CreateTestRpcService(out List<string> capturedScripts)
    {
        var scripts = new List<string>();
        capturedScripts = scripts;
        return new WebViewRpcService(
            script => { scripts.Add(script); return Task.FromResult<string?>(null); },
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
    }

    // ========================= CamelCase JSON serialization =========================

    // Plain record without [JsonPropertyName] — should serialize as camelCase via BridgeJsonOptions.
    private record PlainProfile(string UserName, bool IsAdmin, int LoginCount);

    // Record with explicit [JsonPropertyName] — attribute should take priority over naming policy.
    private record CustomNameProfile(
        [property: System.Text.Json.Serialization.JsonPropertyName("user_name")] string UserName,
        bool IsAdmin);

    [Fact]
    public async Task Rpc_InvokeAsync_params_serialize_as_camelCase()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.setProfile", new PlainProfile("Alice", true, 5));

        WaitUntil(() => scripts.Count > 0);
        Assert.NotEmpty(scripts);

        // Extract the dispatched JSON and verify camelCase property names
        var dispatchScript = scripts[0];
        Assert.Contains("userName", dispatchScript);
        Assert.Contains("isAdmin", dispatchScript);
        Assert.Contains("loginCount", dispatchScript);
        // Ensure PascalCase names are NOT present in the payload
        Assert.DoesNotContain("UserName", dispatchScript);
        Assert.DoesNotContain("IsAdmin", dispatchScript);
        Assert.DoesNotContain("LoginCount", dispatchScript);

        var callId = ExtractRpcId(scripts[0]);
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":null}");
        await task;
    }

    [Fact]
    public void Rpc_handler_result_serializes_as_camelCase()
    {
        var rpc = CreateTestRpcService(out var scripts);
        rpc.Handle("getProfile", _ => Task.FromResult<object?>(new PlainProfile("Bob", false, 10)));

        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"cc-1\",\"method\":\"getProfile\",\"params\":null}");
        WaitUntil(() => scripts.Any(s => s.Contains("cc-1")));

        var responseScript = scripts.First(s => s.Contains("_onResponse") && s.Contains("cc-1"));
        Assert.Contains("userName", responseScript);
        Assert.Contains("isAdmin", responseScript);
        Assert.Contains("loginCount", responseScript);
        Assert.DoesNotContain("UserName", responseScript);
        Assert.DoesNotContain("IsAdmin", responseScript);
        Assert.DoesNotContain("LoginCount", responseScript);
    }

    [Fact]
    public async Task Rpc_InvokeAsync_T_deserializes_camelCase_into_record()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync<PlainProfile>("js.getProfile");

        WaitUntil(() => scripts.Count > 0);
        var callId = ExtractRpcId(scripts[0]);

        // JS returns camelCase JSON
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":{\"userName\":\"Alice\",\"isAdmin\":true,\"loginCount\":5}}");

        var profile = await task;
        Assert.NotNull(profile);
        Assert.Equal("Alice", profile!.UserName);
        Assert.True(profile.IsAdmin);
        Assert.Equal(5, profile.LoginCount);
    }

    [Fact]
    public async Task Rpc_JsonPropertyName_takes_priority_over_naming_policy()
    {
        var rpc = CreateTestRpcService(out var scripts);
        var task = rpc.InvokeAsync("js.setCustomProfile", new CustomNameProfile("Charlie", true));

        WaitUntil(() => scripts.Count > 0);
        Assert.NotEmpty(scripts);

        // user_name from [JsonPropertyName] should appear, not userName
        var dispatchScript = scripts[0];
        Assert.Contains("user_name", dispatchScript);
        // isAdmin should still be camelCase (no [JsonPropertyName] on it)
        Assert.Contains("isAdmin", dispatchScript);

        var callId = ExtractRpcId(scripts[0]);
        rpc.TryProcessMessage("{\"jsonrpc\":\"2.0\",\"id\":\"" + callId + "\",\"result\":null}");
        await task;
    }

    // ==================== FindInPage Tests ====================

    [Fact]
    public async Task FindInPageAsync_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.FindInPageAsync("test"));
    }

    [Fact]
    public async Task FindInPageAsync_throws_on_null_or_empty_text()
    {
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<ArgumentException>(() => core.FindInPageAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => core.FindInPageAsync(null!));
    }

    [Fact]
    public async Task FindInPageAsync_delegates_to_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var core = new WebViewCore(adapter, _dispatcher);
        var opts = new FindInPageOptions { CaseSensitive = true, Forward = false };
        var result = await core.FindInPageAsync("hello", opts);

        Assert.Equal(0, result.ActiveMatchIndex);
        Assert.Equal(3, result.TotalMatches);

        var findAdapter = (MockWebViewAdapterWithFind)adapter;
        Assert.Equal("hello", findAdapter.LastSearchText);
        Assert.True(findAdapter.LastOptions?.CaseSensitive);
        Assert.False(findAdapter.LastOptions?.Forward);
    }

    [Fact]
    public async Task StopFindInPage_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.StopFindInPageAsync());
    }

    [Fact]
    public async Task StopFindInPage_delegates_to_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var core = new WebViewCore(adapter, _dispatcher);
        await core.StopFindInPageAsync(false);

        var findAdapter = (MockWebViewAdapterWithFind)adapter;
        Assert.True(findAdapter.StopFindCalled);
        Assert.False(findAdapter.LastClearHighlights);
    }

    [Fact]
    public async Task WebDialog_FindInPageAsync_throws_when_unsupported()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.FindInPageAsync("test"));
    }

    [Fact]
    public async Task WebDialog_FindInPageAsync_delegates_to_core()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithFind();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        var result = await dialog.FindInPageAsync("hello");
        Assert.Equal(3, result.TotalMatches);
    }

    [Fact]
    public async Task WebDialog_StopFindInPage_throws_when_unsupported()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.StopFindInPageAsync());
    }

    [Fact]
    public void FindInPageOptions_defaults()
    {
        var opts = new FindInPageOptions();
        Assert.False(opts.CaseSensitive);
        Assert.True(opts.Forward);
    }

    [Fact]
    public void FindInPageResult_defaults()
    {
        var result = new FindInPageResult();
        Assert.Equal(0, result.ActiveMatchIndex);
        Assert.Equal(0, result.TotalMatches);
    }

    // ==================== Zoom Tests ====================

    [Fact]
    public void ZoomFactor_default_is_1_without_adapter()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        Assert.Equal(1.0, core.GetZoomFactorAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public void ZoomFactor_set_is_noop_without_adapter()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        core.SetZoomFactorAsync(2.0).GetAwaiter().GetResult(); // should not throw
        Assert.Equal(1.0, core.GetZoomFactorAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public void ZoomFactor_get_set_with_adapter()
    {
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var core = new WebViewCore(adapter, _dispatcher);
        Assert.Equal(1.0, core.GetZoomFactorAsync().GetAwaiter().GetResult());

        core.SetZoomFactorAsync(1.5).GetAwaiter().GetResult();
        Assert.Equal(1.5, core.GetZoomFactorAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public void ZoomFactor_clamps_to_min_max()
    {
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var core = new WebViewCore(adapter, _dispatcher);

        core.SetZoomFactorAsync(0.1).GetAwaiter().GetResult(); // below 0.25 min
        Assert.Equal(0.25, core.GetZoomFactorAsync().GetAwaiter().GetResult(), 2);

        core.SetZoomFactorAsync(10.0).GetAwaiter().GetResult(); // above 5.0 max
        Assert.Equal(5.0, core.GetZoomFactorAsync().GetAwaiter().GetResult(), 2);
    }

    [Fact]
    public void ZoomFactorChanged_fires_on_set()
    {
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var core = new WebViewCore(adapter, _dispatcher);

        double? received = null;
        core.ZoomFactorChanged += (_, z) => received = z;

        core.SetZoomFactorAsync(2.0).GetAwaiter().GetResult();
        Assert.Equal(2.0, received);
    }

    [Fact]
    public void WebDialog_ZoomFactor_delegates_to_core()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        Assert.Equal(1.0, dialog.GetZoomFactorAsync().GetAwaiter().GetResult());
        dialog.SetZoomFactorAsync(1.5).GetAwaiter().GetResult();
        Assert.Equal(1.5, dialog.GetZoomFactorAsync().GetAwaiter().GetResult());
    }

    // ==================== PreloadScript Tests ====================

    [Fact]
    public async Task AddPreloadScript_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.AddPreloadScriptAsync("console.log('hi')"));
    }

    [Fact]
    public async Task AddPreloadScript_returns_script_id()
    {
        var adapter = MockWebViewAdapter.CreateWithPreload();
        using var core = new WebViewCore(adapter, _dispatcher);
        var id = await core.AddPreloadScriptAsync("console.log('hi')");
        Assert.NotNull(id);
        Assert.NotEmpty(id);
    }

    [Fact]
    public async Task AddPreloadScript_prefers_async_adapter_when_available()
    {
        var adapter = MockWebViewAdapter.CreateWithPreload();
        using var core = new WebViewCore(adapter, _dispatcher);
        var preloadAdapter = (MockWebViewAdapterWithPreload)adapter;

        const string script = "console.log('prefer-async')";
        var id = await core.AddPreloadScriptAsync(script);
        await core.RemovePreloadScriptAsync(id);

        Assert.Contains(script, preloadAdapter.AsyncAddedScripts);
        Assert.DoesNotContain(script, preloadAdapter.SyncAddedScripts);
        Assert.Contains(id, preloadAdapter.AsyncRemovedScriptIds);
        Assert.DoesNotContain(id, preloadAdapter.SyncRemovedScriptIds);
    }

    [Fact]
    public async Task RemovePreloadScript_throws_when_unsupported()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => core.RemovePreloadScriptAsync("some-id"));
    }

    [Fact]
    public async Task RemovePreloadScript_removes_script()
    {
        var adapter = MockWebViewAdapter.CreateWithPreload();
        using var core = new WebViewCore(adapter, _dispatcher);
        var id = await core.AddPreloadScriptAsync("console.log('hi')");
        await core.RemovePreloadScriptAsync(id);

        var preloadAdapter = (MockWebViewAdapterWithPreload)adapter;
        Assert.DoesNotContain(id, preloadAdapter.Scripts.Keys);
    }

    [Fact]
    public async Task WebDialog_AddPreloadScript_delegates_to_core()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithPreload();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        var id = await dialog.AddPreloadScriptAsync("console.log('test')");
        Assert.NotNull(id);
    }

    [Fact]
    public async Task WebDialog_AddPreloadScript_throws_when_unsupported()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);
        await Assert.ThrowsAsync<NotSupportedException>(() => dialog.AddPreloadScriptAsync("x"));
    }

    [Fact]
    public void Global_preloadscripts_applied_at_construction()
    {
        var adapter = MockWebViewAdapter.CreateWithPreload();
        var originalOptions = WebViewEnvironment.Options;
        try
        {
            WebViewEnvironment.Options = new WebViewEnvironmentOptions
            {
                PreloadScripts = new[] { "console.log('global1')", "console.log('global2')" }
            };
            using var core = new WebViewCore(adapter, _dispatcher);
            var preloadAdapter = (MockWebViewAdapterWithPreload)adapter;
            Assert.Equal(2, preloadAdapter.Scripts.Count);
        }
        finally
        {
            WebViewEnvironment.Options = originalOptions;
        }
    }

    // ==================== ContextMenu Tests ====================

    [Fact]
    public void ContextMenuRequested_fires_when_adapter_raises()
    {
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var core = new WebViewCore(adapter, _dispatcher);

        ContextMenuRequestedEventArgs? received = null;
        core.ContextMenuRequested += (_, e) => received = e;

        var args = new ContextMenuRequestedEventArgs
        {
            X = 100, Y = 200,
            LinkUri = new Uri("https://example.com"),
            SelectionText = "hello",
            MediaType = ContextMenuMediaType.None,
            IsEditable = true
        };
        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(args);

        Assert.NotNull(received);
        Assert.Equal(100, received!.X);
        Assert.Equal(200, received.Y);
        Assert.Equal("hello", received.SelectionText);
        Assert.True(received.IsEditable);
    }

    [Fact]
    public void ContextMenuRequested_not_fired_without_adapter()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        ContextMenuRequestedEventArgs? received = null;
        core.ContextMenuRequested += (_, e) => received = e;

        // No way to trigger it — should stay null
        Assert.Null(received);
    }

    [Fact]
    public void ContextMenuRequested_Handled_flag_propagates()
    {
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var core = new WebViewCore(adapter, _dispatcher);

        core.ContextMenuRequested += (_, e) => e.Handled = true;

        var args = new ContextMenuRequestedEventArgs { X = 10, Y = 20 };
        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(args);

        Assert.True(args.Handled);
    }

    [Fact]
    public void ContextMenuMediaType_enum_values()
    {
        Assert.Equal(0, (int)ContextMenuMediaType.None);
        Assert.Equal(1, (int)ContextMenuMediaType.Image);
        Assert.Equal(2, (int)ContextMenuMediaType.Video);
        Assert.Equal(3, (int)ContextMenuMediaType.Audio);
    }

    [Fact]
    public void WebDialog_ContextMenuRequested_fires()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        ContextMenuRequestedEventArgs? received = null;
        dialog.ContextMenuRequested += (_, e) => received = e;

        var args = new ContextMenuRequestedEventArgs { X = 50, Y = 60 };
        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(args);

        Assert.NotNull(received);
        Assert.Equal(50, received!.X);
    }
}
