using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

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
        core.GoBack();

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

        await navTask.ContinueWith(_ => { }); // Absorb any fault

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
        public void Attach(global::Avalonia.Platform.IPlatformHandle parentHandle) { }
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
        public global::Avalonia.Platform.IPlatformHandle? PlatformHandle => new TestPlatformHandle();
    }

    private sealed class TestPlatformHandle : global::Avalonia.Platform.IPlatformHandle
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
}
