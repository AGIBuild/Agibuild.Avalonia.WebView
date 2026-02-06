using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Bridges <see cref="IWebViewDispatcher"/> to Avalonia's UI-thread <see cref="Dispatcher"/>.
/// Thin wrapper — excluded from coverage because it requires an Avalonia runtime.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AvaloniaWebViewDispatcher : IWebViewDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public Task InvokeAsync(Action action)
        => Dispatcher.UIThread.InvokeAsync(action).GetTask();

    public Task<T> InvokeAsync<T>(Func<T> func)
        => Dispatcher.UIThread.InvokeAsync(func).GetTask();

    public Task InvokeAsync(Func<Task> func)
        => Dispatcher.UIThread.InvokeAsync(func);

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
        => Dispatcher.UIThread.InvokeAsync(func);
}
