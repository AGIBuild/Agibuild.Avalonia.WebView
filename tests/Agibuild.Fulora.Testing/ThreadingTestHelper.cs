namespace Agibuild.Fulora.Testing;

public static class ThreadingTestHelper
{
    public static Task RunOffThread(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using var ready = new ManualResetEventSlim(false);
        var tcs = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                ready.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Off-thread invocation did not start within timeout.");
        }

        return tcs.Task.Unwrap();
    }

    public static Task<T> RunOffThread<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using var ready = new ManualResetEventSlim(false);
        var tcs = new TaskCompletionSource<Task<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                ready.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Off-thread invocation did not start within timeout.");
        }

        return tcs.Task.Unwrap();
    }

    public static void PumpUntil(TestDispatcher dispatcher, Func<bool> condition, TimeSpan? timeout = null)
    {
        DispatcherTestPump.WaitUntil(dispatcher, condition, timeout);
    }
}
