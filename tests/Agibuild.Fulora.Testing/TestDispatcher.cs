using System.Collections.Concurrent;

namespace Agibuild.Fulora.Testing;

public sealed class TestDispatcher : IWebViewDispatcher
{
    private readonly int _uiThreadId;
    private readonly ConcurrentQueue<WorkItemBase> _queue = new();

    // Signalled whenever a work item is enqueued OR a continuation may have completed a task
    // the pump is waiting on. Replaces the prior Thread.Sleep(1) busy-polling, which on stressed
    // CI runners caused massive context-switch contention and made multi-second tests time out.
    private readonly ManualResetEventSlim _workSignal = new(initialState: false);

    public TestDispatcher()
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
    }

    public int UiThreadId => _uiThreadId;

    public bool CheckAccess() => Environment.CurrentManagedThreadId == _uiThreadId;

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var item = new WorkItemVoid(() =>
        {
            action();
            return Task.CompletedTask;
        });
        EnqueueAndSignal(item);
        return item.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            return Task.FromResult(func());
        }

        var item = new WorkItemT<T>(() => Task.FromResult(func()));
        EnqueueAndSignal(item);
        return item.TaskTyped;
    }

    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            return func();
        }

        var item = new WorkItemVoid(func);
        EnqueueAndSignal(item);
        return item.Task;
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            return func();
        }

        var item = new WorkItemT<T>(func);
        EnqueueAndSignal(item);
        return item.TaskTyped;
    }

    /// <summary>
    /// Runs all queued work on the UI thread deterministically.
    /// </summary>
    public void RunAll()
    {
        EnsureUiThread();

        // Drain any pending signal because we are about to consume the queue. New enqueues
        // arriving after the drain will set it again, which is exactly what the pump needs.
        _workSignal.Reset();

        while (_queue.TryDequeue(out var item))
        {
            item.Execute();
        }
    }

    /// <summary>
    /// Blocks the calling (UI) thread until either work arrives in the queue or the timeout
    /// elapses. Returns true if signalled (caller should call <see cref="RunAll"/>) or false
    /// on timeout. The pump uses this in place of busy-polling.
    /// </summary>
    public bool WaitForWork(TimeSpan timeout)
    {
        return _workSignal.Wait(timeout);
    }

    /// <summary>
    /// Manually signals the pump — used by external callers (e.g. waiters that observed a
    /// task completing on a worker thread) to nudge the pump out of <see cref="WaitForWork"/>.
    /// </summary>
    public void Signal() => _workSignal.Set();

    public int QueuedCount => _queue.Count;

    private void EnqueueAndSignal(WorkItemBase item)
    {
        _queue.Enqueue(item);
        _workSignal.Set();
    }

    private void EnsureUiThread()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("TestDispatcher.RunAll must be called on the UI thread.");
        }
    }

    private abstract class WorkItemBase
    {
        public abstract Task Task { get; }
        public abstract void Execute();
    }

    private sealed class WorkItemT<T> : WorkItemBase
    {
        private readonly Func<Task<T>> _func;
        private readonly TaskCompletionSource<T> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkItemT(Func<Task<T>> func)
        {
            _func = func;
        }

        public override Task Task => _tcs.Task;

        public Task<T> TaskTyped => _tcs.Task;

        public override void Execute()
        {
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            try
            {
                var result = await _func().ConfigureAwait(false);
                _tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
        }
    }

    private sealed class WorkItemVoid : WorkItemBase
    {
        private readonly Func<Task> _func;
        private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkItemVoid(Func<Task> func)
        {
            _func = func;
        }

        public override Task Task => _tcs.Task;

        public override void Execute()
        {
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            try
            {
                await _func().ConfigureAwait(false);
                _tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
        }
    }
}

