namespace Agibuild.Avalonia.WebView.Testing;

public static class DispatcherTestPump
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static void Run(TestDispatcher dispatcher, Func<Task> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        var task = Task.Run(action);
        Wait(dispatcher, task, timeout ?? DefaultTimeout);
        task.GetAwaiter().GetResult();
    }

    public static T Run<T>(TestDispatcher dispatcher, Func<Task<T>> action, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        var task = Task.Run(action);
        Wait(dispatcher, task, timeout ?? DefaultTimeout);
        return task.GetAwaiter().GetResult();
    }

    private static void Wait(TestDispatcher dispatcher, Task task, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (!task.IsCompleted)
        {
            dispatcher.RunAll();
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out while waiting for dispatcher task completion.");
            }

            Thread.Sleep(1);
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
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out while waiting for dispatcher condition.");
            }

            Thread.Sleep(1);
        }

        dispatcher.RunAll();
    }
}
