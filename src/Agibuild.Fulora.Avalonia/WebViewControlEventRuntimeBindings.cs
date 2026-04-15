namespace Agibuild.Fulora;

internal sealed class WebViewControlEventCallbacks(
    Action<NavigationStartingEventArgs> raiseNavigationStarted,
    Action<NavigationCompletedEventArgs> raiseNavigationCompleted,
    Action<NewWindowRequestedEventArgs> raiseNewWindowRequested,
    Action<WebMessageReceivedEventArgs> raiseWebMessageReceived,
    Action<WebResourceRequestedEventArgs> raiseWebResourceRequested,
    Action<EnvironmentRequestedEventArgs> raiseEnvironmentRequested,
    Action<DownloadRequestedEventArgs> raiseDownloadRequested,
    Action<PermissionRequestedEventArgs> raisePermissionRequested,
    Action<AdapterCreatedEventArgs> raiseAdapterCreated,
    Action raiseAdapterDestroyed,
    Action<double> raiseZoomFactorChanged)
{
    public Action<NavigationStartingEventArgs> RaiseNavigationStarted { get; } = raiseNavigationStarted ?? throw new ArgumentNullException(nameof(raiseNavigationStarted));
    public Action<NavigationCompletedEventArgs> RaiseNavigationCompleted { get; } = raiseNavigationCompleted ?? throw new ArgumentNullException(nameof(raiseNavigationCompleted));
    public Action<NewWindowRequestedEventArgs> RaiseNewWindowRequested { get; } = raiseNewWindowRequested ?? throw new ArgumentNullException(nameof(raiseNewWindowRequested));
    public Action<WebMessageReceivedEventArgs> RaiseWebMessageReceived { get; } = raiseWebMessageReceived ?? throw new ArgumentNullException(nameof(raiseWebMessageReceived));
    public Action<WebResourceRequestedEventArgs> RaiseWebResourceRequested { get; } = raiseWebResourceRequested ?? throw new ArgumentNullException(nameof(raiseWebResourceRequested));
    public Action<EnvironmentRequestedEventArgs> RaiseEnvironmentRequested { get; } = raiseEnvironmentRequested ?? throw new ArgumentNullException(nameof(raiseEnvironmentRequested));
    public Action<DownloadRequestedEventArgs> RaiseDownloadRequested { get; } = raiseDownloadRequested ?? throw new ArgumentNullException(nameof(raiseDownloadRequested));
    public Action<PermissionRequestedEventArgs> RaisePermissionRequested { get; } = raisePermissionRequested ?? throw new ArgumentNullException(nameof(raisePermissionRequested));
    public Action<AdapterCreatedEventArgs> RaiseAdapterCreated { get; } = raiseAdapterCreated ?? throw new ArgumentNullException(nameof(raiseAdapterCreated));
    public Action RaiseAdapterDestroyed { get; } = raiseAdapterDestroyed ?? throw new ArgumentNullException(nameof(raiseAdapterDestroyed));
    public Action<double> RaiseZoomFactorChanged { get; } = raiseZoomFactorChanged ?? throw new ArgumentNullException(nameof(raiseZoomFactorChanged));
}

internal sealed class WebViewControlInteractionAccessors(
    Func<EventHandler<ContextMenuRequestedEventArgs>?> getContextMenuHandlers,
    Func<EventHandler<DragEventArgs>?> getDragEnteredHandlers,
    Func<EventHandler<DragEventArgs>?> getDragOverHandlers,
    Func<EventHandler<EventArgs>?> getDragLeftHandlers,
    Func<EventHandler<DropEventArgs>?> getDropCompletedHandlers)
{
    public Func<EventHandler<ContextMenuRequestedEventArgs>?> GetContextMenuHandlers { get; } = getContextMenuHandlers ?? throw new ArgumentNullException(nameof(getContextMenuHandlers));
    public Func<EventHandler<DragEventArgs>?> GetDragEnteredHandlers { get; } = getDragEnteredHandlers ?? throw new ArgumentNullException(nameof(getDragEnteredHandlers));
    public Func<EventHandler<DragEventArgs>?> GetDragOverHandlers { get; } = getDragOverHandlers ?? throw new ArgumentNullException(nameof(getDragOverHandlers));
    public Func<EventHandler<EventArgs>?> GetDragLeftHandlers { get; } = getDragLeftHandlers ?? throw new ArgumentNullException(nameof(getDragLeftHandlers));
    public Func<EventHandler<DropEventArgs>?> GetDropCompletedHandlers { get; } = getDropCompletedHandlers ?? throw new ArgumentNullException(nameof(getDropCompletedHandlers));
}

internal sealed class WebViewControlNavigationHooks(
    Func<Uri, Task> navigateInPlaceAsync,
    Func<double> getInitialZoomFactor,
    Action<double> applyInitialZoomFactor)
{
    public Func<Uri, Task> NavigateInPlaceAsync { get; } = navigateInPlaceAsync ?? throw new ArgumentNullException(nameof(navigateInPlaceAsync));
    public Func<double> GetInitialZoomFactor { get; } = getInitialZoomFactor ?? throw new ArgumentNullException(nameof(getInitialZoomFactor));
    public Action<double> ApplyInitialZoomFactor { get; } = applyInitialZoomFactor ?? throw new ArgumentNullException(nameof(applyInitialZoomFactor));
}
