using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewCoreNavigationRuntimeTests
{
    private readonly TestDispatcher _dispatcher = new();

    [Fact]
    public void Constructor_requires_host()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebViewCoreNavigationRuntime(null!, _dispatcher, NullLogger.Instance));
    }

    [Fact]
    public void Constructor_requires_dispatcher()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebViewCoreNavigationRuntime(new TestNavigationHost(), null!, NullLogger.Instance));
    }

    [Fact]
    public void Constructor_requires_logger()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebViewCoreNavigationRuntime(new TestNavigationHost(), _dispatcher, null!));
    }

    [Fact]
    public async Task NativeNavigationStarting_non_main_frame_is_auto_allowed()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        var decision = await runtime.OnNativeNavigationStartingAsync(new NativeNavigationStartingInfo(
            Guid.NewGuid(),
            new Uri("https://example.test/sub-frame"),
            false));

        Assert.True(decision.IsAllowed);
        Assert.Equal(Guid.Empty, decision.NavigationId);
        Assert.False(runtime.IsLoading);
        Assert.Empty(host.StartedEvents);
    }

    [Fact]
    public async Task StartNavigationRequest_supersedes_active_navigation_and_invokes_adapter()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        // Seed an "active" navigation via the public API so the runtime owns its own state.
        var preExistingTask = runtime.SetActiveNavigation(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://example.test/active"));
        Guid? invokedNavigationId = null;

        var operationTask = await runtime.StartNavigationRequestCoreAsync(
            new Uri("https://example.test/next"),
            navigationId =>
            {
                invokedNavigationId = navigationId;
                return Task.CompletedTask;
            },
            updateSource: true);

        // The pre-existing navigation should complete with Superseded (TrySetSuccess, so task is done).
        Assert.True(preExistingTask.IsCompletedSuccessfully);
        Assert.Equal([NavigationCompletedStatus.Superseded], host.CompletedEvents.Select(e => e.Status).ToArray());

        Assert.NotNull(invokedNavigationId);
        Assert.True(runtime.TryGetActiveNavigation(out var active));
        Assert.Equal(invokedNavigationId, active.NavigationId);

        Assert.Equal(new Uri("https://example.test/next"), host.LastSource);
        Assert.Single(host.StartedEvents);
        Assert.False(operationTask.IsCompleted);
    }

    [Fact]
    public async Task StartNavigationRequest_canceled_by_handler_completes_as_canceled_without_invoking_adapter()
    {
        var host = new TestNavigationHost
        {
            CancelStartedNavigations = true
        };
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var adapterInvoked = false;

        var operationTask = await runtime.StartNavigationRequestCoreAsync(
            new Uri("https://example.test/cancel"),
            _ =>
            {
                adapterInvoked = true;
                return Task.CompletedTask;
            },
            updateSource: true);

        Assert.False(adapterInvoked);
        Assert.Equal([NavigationCompletedStatus.Canceled], host.CompletedEvents.Select(e => e.Status).ToArray());
        await operationTask;
        Assert.False(runtime.IsLoading);
    }

    [Fact]
    public async Task StartNavigationRequest_adapter_exception_completes_as_failure()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        var operationTask = await runtime.StartNavigationRequestCoreAsync(
            new Uri("https://example.test/fail"),
            _ => Task.FromException(new InvalidOperationException("adapter boom")),
            updateSource: true);

        var completed = Assert.Single(host.CompletedEvents);
        Assert.Equal(NavigationCompletedStatus.Failure, completed.Status);
        // NavigationRuntime always wraps failures in WebViewNavigationException with the original
        // exception as inner, to preserve adapter-categorized subclasses when applicable.
        var wrapped = await Assert.ThrowsAsync<WebViewNavigationException>(() => operationTask);
        Assert.IsType<InvalidOperationException>(wrapped.InnerException);
        Assert.Equal("adapter boom", wrapped.InnerException!.Message);
    }

    [Fact]
    public void StartCommandNavigation_canceled_by_handler_returns_empty_guid()
    {
        var host = new TestNavigationHost
        {
            CancelStartedNavigations = true
        };
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        var navigationId = runtime.StartCommandNavigation(new Uri("https://example.test/command"));

        Assert.Equal(Guid.Empty, navigationId);
        Assert.Equal([NavigationCompletedStatus.Canceled], host.CompletedEvents.Select(e => e.Status).ToArray());
        Assert.False(runtime.IsLoading);
    }

    [Fact]
    public async Task NativeNavigationStarting_redirect_reuses_active_navigation_id()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var correlationId = Guid.NewGuid();
        var activeNavigationId = Guid.NewGuid();
        _ = runtime.SetActiveNavigation(activeNavigationId, correlationId, new Uri("https://example.test/start"));

        var decision = await runtime.OnNativeNavigationStartingAsync(new NativeNavigationStartingInfo(
            correlationId,
            new Uri("https://example.test/redirected"),
            true));

        Assert.True(decision.IsAllowed);
        Assert.Equal(activeNavigationId, decision.NavigationId);

        Assert.True(runtime.TryGetActiveNavigation(out var active));
        Assert.Equal(new Uri("https://example.test/redirected"), active.RequestUri);
        Assert.Equal(new Uri("https://example.test/redirected"), host.LastSource);

        Assert.Single(host.StartedEvents);
        Assert.Equal(activeNavigationId, host.StartedEvents[0].NavigationId);
    }

    [Fact]
    public async Task NativeNavigationStarting_when_redirect_is_canceled_denies_and_completes_navigation()
    {
        var host = new TestNavigationHost
        {
            CancelStartedNavigations = true
        };
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var correlationId = Guid.NewGuid();
        var activeNavigationId = Guid.NewGuid();
        _ = runtime.SetActiveNavigation(activeNavigationId, correlationId, new Uri("https://example.test/start"));

        var decision = await runtime.OnNativeNavigationStartingAsync(new NativeNavigationStartingInfo(
            correlationId,
            new Uri("https://example.test/redirected"),
            true));

        Assert.False(decision.IsAllowed);
        Assert.Equal(activeNavigationId, decision.NavigationId);
        var completed = Assert.Single(host.CompletedEvents);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed.Status);
        Assert.False(runtime.IsLoading);
    }

    [Fact]
    public void AdapterNavigationCompleted_id_mismatch_is_ignored()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        _ = runtime.SetActiveNavigation(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://example.test/active"));

        runtime.HandleAdapterNavigationCompleted(new NavigationCompletedEventArgs(
            Guid.NewGuid(),
            new Uri("https://example.test/other"),
            NavigationCompletedStatus.Success,
            error: null));

        Assert.Empty(host.CompletedEvents);
        Assert.True(runtime.IsLoading);
    }

    [Fact]
    public void AdapterNavigationCompleted_success_updates_request_uri_and_completes_navigation()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var navigationId = Guid.NewGuid();
        _ = runtime.SetActiveNavigation(navigationId, navigationId, new Uri("https://example.test/active"));

        runtime.HandleAdapterNavigationCompleted(new NavigationCompletedEventArgs(
            navigationId,
            new Uri("https://example.test/success"),
            NavigationCompletedStatus.Success,
            error: null));

        var completed = Assert.Single(host.CompletedEvents);
        Assert.Equal(NavigationCompletedStatus.Success, completed.Status);
        Assert.Null(completed.Error);
        Assert.Equal(new Uri("https://example.test/success"), completed.RequestUri);
        Assert.True(host.BridgeReinjected, "Success completion should trigger bridge re-injection.");
        Assert.False(runtime.IsLoading);
    }

    [Fact]
    public void FaultActiveForDispose_is_silent_and_clears_active_navigation()
    {
        // B3 guard: Dispose-path fault must never raise NavigationCompleted, because observers
        // are being torn down concurrently. The runtime must still release the active op so the
        // awaiting Task<> faults with the provided ObjectDisposedException.
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var operationTask = runtime.SetActiveNavigation(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://example.test/active"));

        runtime.FaultActiveForDispose(new ObjectDisposedException("WebViewCore"));

        Assert.Empty(host.CompletedEvents);
        Assert.False(runtime.IsLoading);
        Assert.True(operationTask.IsFaulted);
        Assert.IsType<ObjectDisposedException>(operationTask.Exception!.InnerException);
    }

    [Fact]
    public void FaultActiveForDispose_when_no_active_navigation_is_noop()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        // Must not throw and must not raise any events.
        runtime.FaultActiveForDispose(new ObjectDisposedException("WebViewCore"));

        Assert.Empty(host.CompletedEvents);
        Assert.False(runtime.IsLoading);
    }

    [Fact]
    public void TryStopActiveNavigation_when_no_active_returns_false()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);

        Assert.False(runtime.TryStopActiveNavigation());
        Assert.Empty(host.CompletedEvents);
    }

    [Fact]
    public void TryStopActiveNavigation_cancels_active_navigation()
    {
        var host = new TestNavigationHost();
        var runtime = new WebViewCoreNavigationRuntime(host, _dispatcher, NullLogger.Instance);
        var operationTask = runtime.SetActiveNavigation(Guid.NewGuid(), Guid.NewGuid(), new Uri("https://example.test/active"));

        Assert.True(runtime.TryStopActiveNavigation());
        var completed = Assert.Single(host.CompletedEvents);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed.Status);
        Assert.True(operationTask.IsCompletedSuccessfully);
        Assert.False(runtime.IsLoading);
    }

    private sealed class TestNavigationHost : IWebViewCoreNavigationHost
    {
        private readonly int _uiThreadId = Environment.CurrentManagedThreadId;

        public bool IsDisposed { get; set; }

        public bool IsAdapterDestroyed { get; set; }

        public bool CancelStartedNavigations { get; set; }

        public Uri? LastSource { get; private set; }

        public bool BridgeReinjected { get; private set; }

        public List<NavigationStartingEventArgs> StartedEvents { get; } = [];

        public List<NavigationCompletedEventArgs> CompletedEvents { get; } = [];

        public void RaiseNavigationStarting(NavigationStartingEventArgs args)
        {
            StartedEvents.Add(args);
            if (CancelStartedNavigations)
            {
                args.Cancel = true;
            }
        }

        public void RaiseNavigationCompleted(NavigationCompletedEventArgs args)
            => CompletedEvents.Add(args);

        public void ReinjectBridgeStubsIfEnabled()
            => BridgeReinjected = true;

        public void SetSource(Uri uri)
            => LastSource = uri;

        public void ThrowIfNotOnUiThread(string apiName)
        {
            if (Environment.CurrentManagedThreadId != _uiThreadId)
            {
                throw new InvalidOperationException($"'{apiName}' must be called on the UI thread.");
            }
        }
    }
}
