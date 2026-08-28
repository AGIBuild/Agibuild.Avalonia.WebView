using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class AvaloniaWebViewDispatcherTests
{
    [Fact]
    public void CheckAccess_is_false_on_a_background_thread()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        var backgroundAccess = true;
        var worker = new Thread(() => backgroundAccess = dispatcher.CheckAccess())
        {
            IsBackground = true
        };
        worker.Start();
        worker.Join();
        Assert.False(backgroundAccess);
    }

    [Fact]
    public async Task InvokeAsync_action_runs_inline_when_access_is_allowed()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        if (!dispatcher.CheckAccess())
        {
            return;
        }

        var called = false;
        await dispatcher.InvokeAsync(() => called = true);
        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_func_propagates_result()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        if (!dispatcher.CheckAccess())
        {
            return;
        }

        Assert.Equal(7, await dispatcher.InvokeAsync(() => 7));
        Assert.Equal(8, await dispatcher.InvokeAsync(() => Task.FromResult(8)));
        await dispatcher.InvokeAsync(() => Task.CompletedTask);
    }

    [Fact]
    public async Task InvokeAsync_propagates_synchronous_exception()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        if (!dispatcher.CheckAccess())
        {
            return;
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.InvokeAsync(() => throw new InvalidOperationException("boom")));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task InvokeAsync_propagates_asynchronous_exception()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        if (!dispatcher.CheckAccess())
        {
            return;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.InvokeAsync(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("async-boom");
            }));
    }

    [Fact]
    public async Task InvokeAsync_propagates_canceled_task()
    {
        var dispatcher = new AvaloniaWebViewDispatcher();
        if (!dispatcher.CheckAccess())
        {
            return;
        }

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => dispatcher.InvokeAsync(() => Task.FromCanceled(new CancellationToken(true))));
    }

    [Fact]
    public async Task Two_instances_have_independent_completion()
    {
        var first = new AvaloniaWebViewDispatcher();
        var second = new AvaloniaWebViewDispatcher();
        if (!first.CheckAccess())
        {
            return;
        }

        var firstDone = false;
        var secondDone = false;
        var firstTask = first.InvokeAsync(() => firstDone = true);
        var secondTask = second.InvokeAsync(() => secondDone = true);
        await Task.WhenAll(firstTask, secondTask);
        Assert.True(firstDone);
        Assert.True(secondDone);
        Assert.Equal(1, await first.InvokeAsync(() => 1));
        Assert.Equal(2, await second.InvokeAsync(() => 2));
    }
}
