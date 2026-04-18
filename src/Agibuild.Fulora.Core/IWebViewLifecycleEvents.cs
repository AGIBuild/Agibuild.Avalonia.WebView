namespace Agibuild.Fulora;

/// <summary>
/// Capability: observe the creation and destruction of the underlying platform
/// adapter. Each WebView lifecycle raises <c>AdapterCreated</c> exactly once
/// and <c>AdapterDestroyed</c> at most once before disposal.
/// </summary>
public interface IWebViewLifecycleEvents
{
    /// <summary>
    /// Raised after the native adapter is attached and ready for navigation.
    /// </summary>
    event EventHandler<AdapterCreatedEventArgs>? AdapterCreated;

    /// <summary>
    /// Raised exactly once as the adapter is being torn down. Subscribers must
    /// not enqueue new work on the WebView from the handler body.
    /// </summary>
    event EventHandler? AdapterDestroyed;
}
