using System.Threading;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class SynchronizationContextWebViewDispatcherTests
{
    [Fact]
    public void CheckAccess_remains_true_on_captured_thread_when_sync_context_changes()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var dispatcher = new SynchronizationContextWebViewDispatcher();

            // Simulate UI thread execution where SynchronizationContext.Current is not the captured instance.
            SynchronizationContext.SetSynchronizationContext(null);

            Assert.True(dispatcher.CheckAccess());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task InvokeAsync_Action_executes_immediately_when_access_allowed()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var dispatcher = new SynchronizationContextWebViewDispatcher();
            var called = false;

            await dispatcher.InvokeAsync(() => { called = true; });

            Assert.True(called);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task InvokeAsync_Action_posts_to_captured_context_when_access_denied()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            var trackingContext = new TrackingSynchronizationContext();
            var dispatcher = CreateDispatcherOnDedicatedThread(trackingContext);

            var called = false;
            await dispatcher.InvokeAsync(() => { called = true; });

            Assert.True(called);
            Assert.True(trackingContext.PostCount > 0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task InvokeAsync_Action_propagates_exceptions()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            var trackingContext = new TrackingSynchronizationContext();
            var dispatcher = CreateDispatcherOnDedicatedThread(trackingContext);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.InvokeAsync(() =>
                throw new InvalidOperationException("boom")));
            Assert.Equal("boom", ex.Message);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task InvokeAsync_FuncT_returns_value_for_posted_execution()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            var trackingContext = new TrackingSynchronizationContext();
            var dispatcher = CreateDispatcherOnDedicatedThread(trackingContext);

            var result = await dispatcher.InvokeAsync(() => 42);

            Assert.Equal(42, result);
            Assert.True(trackingContext.PostCount > 0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task InvokeAsync_FuncTask_and_FuncTaskT_cover_async_paths()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            var trackingContext = new TrackingSynchronizationContext();
            var dispatcher = CreateDispatcherOnDedicatedThread(trackingContext);

            await dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(1, TestContext.Current.CancellationToken);
            });

            var value = await dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(1, TestContext.Current.CancellationToken);
                return "ok";
            });

            Assert.Equal("ok", value);
            Assert.True(trackingContext.PostCount > 0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task CheckAccess_allows_when_no_context_captured_even_cross_thread()
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            var dispatcher = new SynchronizationContextWebViewDispatcher();
            var result = await Task.Run(dispatcher.CheckAccess, TestContext.Current.CancellationToken);
            Assert.True(result);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private sealed class TrackingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;
        public int PostCount => _postCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCount);
            d(state);
        }
    }

    private static SynchronizationContextWebViewDispatcher CreateDispatcherOnDedicatedThread(SynchronizationContext context)
    {
        SynchronizationContextWebViewDispatcher? dispatcher = null;
        Exception? failure = null;
        using var ready = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                dispatcher = new SynchronizationContextWebViewDispatcher();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                ready.Set();
            }
        });

        thread.Start();
        ready.Wait();
        thread.Join();
        if (failure is not null)
            throw failure;

        return dispatcher!;
    }
}
