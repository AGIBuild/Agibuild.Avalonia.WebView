using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewCoreAttachAsyncTests
{
    [Fact]
    public async Task Synchronous_backend_reaches_ready_and_raises_adapter_created_once()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var created = 0;
        core.AdapterCreated += (_, _) => created++;

        await core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), CancellationToken.None);

        Assert.Equal(1, adapter.AttachCallCount);
        Assert.Equal(1, created);
        Assert.Equal(WebViewLifecycleState.Ready, GetState(core));
        await core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), CancellationToken.None);
        Assert.Equal(1, adapter.AttachCallCount);
        Assert.Equal(1, created);
    }

    [Fact]
    public async Task Delayed_backend_stays_attaching_until_completion()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        adapter.AttachCompletion = new TaskCompletionSource();
        using var core = new WebViewCore(adapter, dispatcher);
        var created = 0;
        core.AdapterCreated += (_, _) => created++;

        var attach = core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), CancellationToken.None);
        Assert.False(attach.IsCompleted);
        Assert.Equal(WebViewLifecycleState.Attaching, GetState(core));
        Assert.Equal(0, created);

        adapter.AttachCompletion.SetResult();
        await attach;

        Assert.Equal(1, created);
        Assert.Equal(WebViewLifecycleState.Ready, GetState(core));
    }

    [Fact]
    public async Task Attach_failure_reaches_faulted()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        adapter.AttachCompletion = new TaskCompletionSource();
        using var core = new WebViewCore(adapter, dispatcher);
        var created = 0;
        core.AdapterCreated += (_, _) => created++;

        var attach = core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), CancellationToken.None);
        adapter.AttachCompletion.SetException(new InvalidOperationException("native failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => attach);
        Assert.Equal("native failed", ex.Message);
        Assert.Equal(0, created);
        Assert.Equal(WebViewLifecycleState.Faulted, GetState(core));
    }

    [Fact]
    public async Task Cancellation_before_completion_prevents_ready()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        adapter.AttachCompletion = new TaskCompletionSource();
        using var core = new WebViewCore(adapter, dispatcher);
        var created = 0;
        core.AdapterCreated += (_, _) => created++;
        using var cts = new CancellationTokenSource();

        var attach = core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), cts.Token);
        cts.Cancel();
        adapter.AttachCompletion.TrySetCanceled(cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attach);
        Assert.Equal(0, created);
        Assert.NotEqual(WebViewLifecycleState.Ready, GetState(core));
    }

    [Fact]
    public async Task Late_success_after_detach_does_not_publish_ready()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        adapter.AttachCompletion = new TaskCompletionSource();
        using var core = new WebViewCore(adapter, dispatcher);
        var created = 0;
        core.AdapterCreated += (_, _) => created++;

        var attach = core.AttachAsync(new TestPlatformHandle(IntPtr.Zero, "test-parent"), CancellationToken.None);
        core.Detach();
        adapter.AttachCompletion.TrySetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attach);

        Assert.Equal(0, created);
        Assert.Equal(WebViewLifecycleState.Detached, GetState(core));
    }

    private static WebViewLifecycleState GetState(WebViewCore core)
    {
        var context = typeof(WebViewCore)
            .GetField("_context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(core)!;
        var lifecycle = context.GetType().GetProperty("Lifecycle")!.GetValue(context)!;
        return (WebViewLifecycleState)lifecycle.GetType().GetProperty("CurrentState")!.GetValue(lifecycle)!;
    }
}
