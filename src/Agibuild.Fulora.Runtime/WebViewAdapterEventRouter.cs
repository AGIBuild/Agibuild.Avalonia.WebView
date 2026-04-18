namespace Agibuild.Fulora;

/// <summary>
/// Routing table for adapter-plane events consumed by <see cref="WebViewCoreEventWiringRuntime"/>.
/// </summary>
/// <remarks>
/// The wiring runtime owns subscription and signature adaptation; it does not know which concrete
/// runtime a given adapter event should land in. Packaging the seven dispatch targets into a
/// single value type keeps that decoupling explicit and lets callers (<see cref="WebViewCore"/>)
/// keep the "which runtime handles what" decision in one place.
/// </remarks>
internal readonly record struct WebViewAdapterEventRouter(
    Action<NavigationCompletedEventArgs> OnNavigationCompleted,
    Action<NewWindowRequestedEventArgs> OnNewWindowRequested,
    Action<WebMessageReceivedEventArgs> OnWebMessageReceived,
    Action<WebResourceRequestedEventArgs> OnWebResourceRequested,
    Action<EnvironmentRequestedEventArgs> OnEnvironmentRequested,
    Action<DownloadRequestedEventArgs> OnDownloadRequested,
    Action<PermissionRequestedEventArgs> OnPermissionRequested);
