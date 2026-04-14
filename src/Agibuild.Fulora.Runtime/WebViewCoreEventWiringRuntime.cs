using Agibuild.Fulora.Adapters.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

internal sealed class WebViewCoreEventWiringRuntime : IDisposable
{
    private readonly IWebViewAdapter _adapter;
    private readonly ILogger _logger;

    private readonly EventHandler<NavigationCompletedEventArgs> _navigationCompleted;
    private readonly EventHandler<NewWindowRequestedEventArgs> _newWindowRequested;
    private readonly EventHandler<WebMessageReceivedEventArgs> _webMessageReceived;
    private readonly EventHandler<WebResourceRequestedEventArgs> _webResourceRequested;
    private readonly EventHandler<EnvironmentRequestedEventArgs> _environmentRequested;
    private readonly EventHandler<DownloadRequestedEventArgs>? _downloadRequested;
    private readonly EventHandler<PermissionRequestedEventArgs>? _permissionRequested;

    public WebViewCoreEventWiringRuntime(
        IWebViewAdapter adapter,
        ILogger logger,
        EventHandler<NavigationCompletedEventArgs> navigationCompleted,
        EventHandler<NewWindowRequestedEventArgs> newWindowRequested,
        EventHandler<WebMessageReceivedEventArgs> webMessageReceived,
        EventHandler<WebResourceRequestedEventArgs> webResourceRequested,
        EventHandler<EnvironmentRequestedEventArgs> environmentRequested,
        EventHandler<DownloadRequestedEventArgs>? downloadRequested,
        EventHandler<PermissionRequestedEventArgs>? permissionRequested)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationCompleted = navigationCompleted ?? throw new ArgumentNullException(nameof(navigationCompleted));
        _newWindowRequested = newWindowRequested ?? throw new ArgumentNullException(nameof(newWindowRequested));
        _webMessageReceived = webMessageReceived ?? throw new ArgumentNullException(nameof(webMessageReceived));
        _webResourceRequested = webResourceRequested ?? throw new ArgumentNullException(nameof(webResourceRequested));
        _environmentRequested = environmentRequested ?? throw new ArgumentNullException(nameof(environmentRequested));
        _downloadRequested = downloadRequested;
        _permissionRequested = permissionRequested;

        _adapter.NavigationCompleted += _navigationCompleted;
        _adapter.NewWindowRequested += _newWindowRequested;
        _adapter.WebMessageReceived += _webMessageReceived;
        _adapter.WebResourceRequested += _webResourceRequested;
        _adapter.EnvironmentRequested += _environmentRequested;

        if (_adapter is IDownloadAdapter downloadAdapter && _downloadRequested is not null)
        {
            downloadAdapter.DownloadRequested += _downloadRequested;
            _logger.LogDebug("Download support: enabled");
        }

        if (_adapter is IPermissionAdapter permissionAdapter && _permissionRequested is not null)
        {
            permissionAdapter.PermissionRequested += _permissionRequested;
            _logger.LogDebug("Permission support: enabled");
        }
    }

    public void Dispose()
    {
        _adapter.NavigationCompleted -= _navigationCompleted;
        _adapter.NewWindowRequested -= _newWindowRequested;
        _adapter.WebMessageReceived -= _webMessageReceived;
        _adapter.WebResourceRequested -= _webResourceRequested;
        _adapter.EnvironmentRequested -= _environmentRequested;

        if (_adapter is IDownloadAdapter downloadAdapter && _downloadRequested is not null)
            downloadAdapter.DownloadRequested -= _downloadRequested;

        if (_adapter is IPermissionAdapter permissionAdapter && _permissionRequested is not null)
            permissionAdapter.PermissionRequested -= _permissionRequested;
    }
}
