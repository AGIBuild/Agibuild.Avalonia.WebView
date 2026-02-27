namespace Agibuild.Fulora;

/// <summary>
/// Generic <see cref="IWebViewDispatcher"/> backed by a captured <see cref="SynchronizationContext"/>.
/// </summary>
internal sealed class SynchronizationContextWebViewDispatcher : IWebViewDispatcher
{
    private readonly SynchronizationContext _context;
    private readonly bool _hasCapturedContext;

    public SynchronizationContextWebViewDispatcher()
    {
        _context = SynchronizationContext.Current ?? new SynchronizationContext();
        _hasCapturedContext = SynchronizationContext.Current is not null;
    }

    public bool CheckAccess() => !_hasCapturedContext || SynchronizationContext.Current == _context;

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.Post(_ =>
        {
            try
            {
                action();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            return Task.FromResult(func());
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.Post(_ =>
        {
            try
            {
                tcs.TrySetResult(func());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    public async Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        await InvokeAsync(async () =>
        {
            await func().ConfigureAwait(true);
            return true;
        }).ConfigureAwait(false);
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            return func();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.Post(async _ =>
        {
            try
            {
                var value = await func().ConfigureAwait(true);
                tcs.TrySetResult(value);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }
}
