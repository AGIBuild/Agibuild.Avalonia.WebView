namespace Agibuild.Fulora;

/// <summary>
/// Capability: open, close, and query the developer tools pane attached to a WebView.
/// Implemented by every production <see cref="IWebView"/> — inherited by
/// <see cref="IWebViewFeatures"/> so existing callers continue to work unchanged.
/// </summary>
public interface IWebViewDevTools
{
    /// <summary>Opens the developer tools pane for this WebView.</summary>
    Task OpenDevToolsAsync();

    /// <summary>Closes the developer tools pane if open; no-op otherwise.</summary>
    Task CloseDevToolsAsync();

    /// <summary>Returns <see langword="true"/> when the developer tools pane is currently open.</summary>
    Task<bool> IsDevToolsOpenAsync();
}
