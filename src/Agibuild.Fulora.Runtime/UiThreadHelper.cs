using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

/// <summary>
/// Observes dispatcher work that originates at synchronous native-event boundaries.
/// Failures are logged; the Task is never discarded unobserved.
/// </summary>
internal static class UiThreadHelper
{
    public static void ObserveDispatch(
        IWebViewDispatcher dispatcher,
        bool disposed,
        bool adapterDestroyed,
        Action action,
        ILogger? logger = null,
        string? logMessageWhenIgnored = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        if (disposed || adapterDestroyed)
        {
            if (logger is not null && logMessageWhenIgnored is not null)
            {
                logger.LogDebug(logMessageWhenIgnored);
            }

            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Observe(dispatcher.InvokeAsync(action), logger);
    }

    public static void Observe(Task task, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        _ = task.ContinueWith(
            t =>
            {
                var error = t.Exception?.GetBaseException();
                if (error is null)
                {
                    return;
                }

                logger?.LogError(error, "UI dispatch failed.");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
