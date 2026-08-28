using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class CallingThreadWebViewDispatcherTests
{
    [Fact]
    public void CheckAccess_is_true_only_on_the_constructing_thread()
    {
        var dispatcher = new CallingThreadWebViewDispatcher();
        Assert.True(dispatcher.CheckAccess());

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
    public async Task InvokeAsync_overloads_run_on_the_constructing_thread()
    {
        var dispatcher = new CallingThreadWebViewDispatcher();
        var called = false;
        await dispatcher.InvokeAsync(() => called = true);
        Assert.True(called);
        Assert.Equal(7, await dispatcher.InvokeAsync(() => 7));
        await dispatcher.InvokeAsync(() => Task.CompletedTask);
        Assert.Equal(8, await dispatcher.InvokeAsync(() => Task.FromResult(8)));
    }

    [Fact]
    public void InvokeAsync_throws_from_a_background_thread()
    {
        var dispatcher = new CallingThreadWebViewDispatcher();
        var errors = new Exception?[4];
        var worker = new Thread(() =>
        {
            errors[0] = Record(() => { _ = dispatcher.InvokeAsync(() => { }); });
            errors[1] = Record(() => { _ = dispatcher.InvokeAsync(() => 1); });
            errors[2] = Record(() => { _ = dispatcher.InvokeAsync(() => Task.CompletedTask); });
            errors[3] = Record(() => { _ = dispatcher.InvokeAsync(() => Task.FromResult(1)); });
        })
        {
            IsBackground = true
        };
        worker.Start();
        worker.Join();
        Assert.All(errors, error => Assert.IsType<InvalidOperationException>(error));
    }

    private static Exception? Record(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public void InvokeAsync_rejects_null_delegates()
    {
        var dispatcher = new CallingThreadWebViewDispatcher();
        Assert.Throws<ArgumentNullException>(() => { _ = dispatcher.InvokeAsync((Action)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = dispatcher.InvokeAsync((Func<int>)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = dispatcher.InvokeAsync((Func<Task>)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = dispatcher.InvokeAsync((Func<Task<int>>)null!); });
    }
}
