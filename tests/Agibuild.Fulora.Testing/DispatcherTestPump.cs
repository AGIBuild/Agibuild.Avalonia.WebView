namespace Agibuild.Fulora.Testing;

public static class DispatcherTestPump
{
    // Generous default to absorb stressed-CI scheduling latency. Tests that intentionally
    // need a tighter bound pass an explicit timeout. The previous 15s default was tight
    // enough that GitHub-hosted runners under heavy parallel load occasionally timed out
    // even on tests that complete in <100ms locally.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    // Maximum wait per signal slice. Even if no signal arrives, we still wake up periodically
    // to pump RunAll() and re-evaluate the deadline — keeps liveness in case a non-dispatcher
    // task (e.g. Task.Run continuation) completes the awaited task without enqueuing dispatcher work.
    private static readonly TimeSpan SignalSlice = TimeSpan.FromMilliseconds(50);

    public static void Run(TestDispatcher dispatcher, Func<Task> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        var task = Task.Run(action);
        // Continuation-based signalling: when the awaited task finishes off-thread, nudge the pump
        // so it wakes up immediately instead of waiting out a SignalSlice.
        _ = task.ContinueWith(_ => dispatcher.Signal(), TaskScheduler.Default);
        Wait(dispatcher, task, timeout ?? DefaultTimeout);
        task.GetAwaiter().GetResult();
    }

    public static T Run<T>(TestDispatcher dispatcher, Func<Task<T>> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        var task = Task.Run(action);
        _ = task.ContinueWith(_ => dispatcher.Signal(), TaskScheduler.Default);
        Wait(dispatcher, task, timeout ?? DefaultTimeout);
        return task.GetAwaiter().GetResult();
    }

    private static void Wait(TestDispatcher dispatcher, Task task, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (!task.IsCompleted)
        {
            dispatcher.RunAll();

            if (task.IsCompleted)
            {
                break;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out while waiting for dispatcher task completion.");
            }

            // Wait for either a dispatcher enqueue, an external completion signal, or the slice to
            // elapse. This eliminates the previous tight Thread.Sleep(1) loop that, under heavy CI
            // parallelism, caused enough context-switch contention to deadline tests.
            dispatcher.WaitForWork(SignalSlice);
        }

        dispatcher.RunAll();
    }

    public static void WaitUntil(TestDispatcher dispatcher, Func<bool> condition, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(condition);

        var deadline = DateTime.UtcNow.Add(timeout ?? DefaultTimeout);
        while (!condition())
        {
            dispatcher.RunAll();

            if (condition())
            {
                break;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out while waiting for dispatcher condition.");
            }

            dispatcher.WaitForWork(SignalSlice);
        }

        dispatcher.RunAll();
    }
}
