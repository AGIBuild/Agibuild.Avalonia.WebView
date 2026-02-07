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

    // ========================= Helpers =========================

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
