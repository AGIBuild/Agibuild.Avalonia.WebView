namespace Agibuild.Fulora;

/// <summary>
/// Capability: observe download requests initiated from page content. Handlers
/// may cancel or redirect downloads via <see cref="DownloadRequestedEventArgs"/>.
/// </summary>
public interface IWebViewDownloads
{
    /// <summary>
    /// Raised when the page initiates a download. Handlers run on the UI thread.
    /// </summary>
    event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;
}
