using Avalonia.Threading;

namespace Agibuild.Fulora;

internal sealed class AvaloniaWebViewDispatcher : IWebViewDispatcher
{
    private readonly Dispatcher _dispatcher;
    private readonly int _uiThreadId;

    public AvaloniaWebViewDispatcher()
        : this(Dispatcher.UIThread)
    {
    }

    internal AvaloniaWebViewDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _uiThreadId = Environment.CurrentManagedThreadId;
    }

    public bool CheckAccess()
        => Environment.CurrentManagedThreadId == _uiThreadId && _dispatcher.CheckAccess();

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(action).GetTask();
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (_dispatcher.CheckAccess())
        {
            return Task.FromResult(func());
        }

        return _dispatcher.InvokeAsync(func).GetTask();
    }

    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (_dispatcher.CheckAccess())
        {
            return Await(func());
        }

        return Await(_dispatcher.InvokeAsync(func));
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (_dispatcher.CheckAccess())
        {
            return Await(func());
        }

        return Await(_dispatcher.InvokeAsync(func));
    }

    private static async Task Await(Task task)
    {
        await task.ConfigureAwait(false);
    }

    private static async Task<T> Await<T>(Task<T> task)
    {
        return await task.ConfigureAwait(false);
    }
}
