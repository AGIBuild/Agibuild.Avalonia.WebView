using Agibuild.Fulora.Adapters.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

/// <summary>
/// Owns the subscription lifecycle for adapter-plane events on a single <see cref="IWebViewAdapter"/>
/// instance. Converts adapter <c>EventHandler&lt;E&gt;</c> signatures into the corresponding
/// <see cref="WebViewAdapterEventRouter"/> dispatch entries and guarantees symmetric unhook on
/// <see cref="Dispose"/>.
/// </summary>
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
        WebViewAdapterEventRouter router)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(router.OnNavigationCompleted);
        ArgumentNullException.ThrowIfNull(router.OnNewWindowRequested);
        ArgumentNullException.ThrowIfNull(router.OnWebMessageReceived);
        ArgumentNullException.ThrowIfNull(router.OnWebResourceRequested);
        ArgumentNullException.ThrowIfNull(router.OnEnvironmentRequested);
        ArgumentNullException.ThrowIfNull(router.OnDownloadRequested);
        ArgumentNullException.ThrowIfNull(router.OnPermissionRequested);

        _navigationCompleted = (_, e) => router.OnNavigationCompleted(e);
        _newWindowRequested = (_, e) => router.OnNewWindowRequested(e);
        _webMessageReceived = (_, e) => router.OnWebMessageReceived(e);
        _webResourceRequested = (_, e) => router.OnWebResourceRequested(e);
        _environmentRequested = (_, e) => router.OnEnvironmentRequested(e);

        _adapter.NavigationCompleted += _navigationCompleted;
        _adapter.NewWindowRequested += _newWindowRequested;
        _adapter.WebMessageReceived += _webMessageReceived;
        _adapter.WebResourceRequested += _webResourceRequested;
        _adapter.EnvironmentRequested += _environmentRequested;

        if (_adapter is IDownloadAdapter downloadAdapter)
        {
            _downloadRequested = (_, e) => router.OnDownloadRequested(e);
            downloadAdapter.DownloadRequested += _downloadRequested;
            _logger.LogDebug("Download support: enabled");
        }

        if (_adapter is IPermissionAdapter permissionAdapter)
        {
            _permissionRequested = (_, e) => router.OnPermissionRequested(e);
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
