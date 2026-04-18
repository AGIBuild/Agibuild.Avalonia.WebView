namespace Agibuild.Fulora;

/// <summary>
/// Capability: observe requests to open a new window (target="_blank", window.open).
/// When unhandled, the navigation falls back to in-place in the current WebView.
/// </summary>
public interface IWebViewPopupWindows
{
    /// <summary>
    /// Raised when page content requests a new window. Set
    /// <see cref="NewWindowRequestedEventArgs.Handled"/> to claim the navigation.
    /// </summary>
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
}
