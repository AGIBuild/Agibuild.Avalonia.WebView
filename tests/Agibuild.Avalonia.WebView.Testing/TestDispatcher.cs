using System.Collections.Concurrent;

namespace Agibuild.Avalonia.WebView.Testing;

public sealed class TestDispatcher : IWebViewDispatcher
{
    private readonly int _uiThreadId;
    private readonly ConcurrentQueue<WorkItemBase> _queue = new();

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
        _queue.Enqueue(item);
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
        _queue.Enqueue(item);
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
        _queue.Enqueue(item);
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
        _queue.Enqueue(item);
        return item.TaskTyped;
    }

    /// <summary>
    /// Runs all queued work on the UI thread deterministically.
    /// </summary>
    public void RunAll()
    {
        EnsureUiThread();

        while (_queue.TryDequeue(out var item))
        {
            item.Execute();
        }
    }

    public int QueuedCount => _queue.Count;

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

