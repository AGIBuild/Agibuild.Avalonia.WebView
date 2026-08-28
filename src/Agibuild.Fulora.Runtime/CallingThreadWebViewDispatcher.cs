namespace Agibuild.Fulora;

/// <summary>
/// Dispatcher used by DI when no Avalonia UI thread is available.
/// CheckAccess is true only on the constructing thread; background callers are not treated as UI.
/// </summary>
internal sealed class CallingThreadWebViewDispatcher : IWebViewDispatcher
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;

    public bool CheckAccess() => Environment.CurrentManagedThreadId == _threadId;

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!CheckAccess())
        {
            throw new InvalidOperationException("The calling thread does not have access to this dispatcher.");
        }

        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (!CheckAccess())
        {
            throw new InvalidOperationException("The calling thread does not have access to this dispatcher.");
        }

        return Task.FromResult(func());
    }

    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (!CheckAccess())
        {
            throw new InvalidOperationException("The calling thread does not have access to this dispatcher.");
        }

        return func();
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (!CheckAccess())
        {
            throw new InvalidOperationException("The calling thread does not have access to this dispatcher.");
        }

        return func();
    }
}
