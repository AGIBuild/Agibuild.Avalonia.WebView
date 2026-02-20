using System.IO;
using Agibuild.Avalonia.WebView;
using Avalonia.Input;

namespace HybridApp.Desktop;

public partial class MainWindow
{
    private EventHandler<NewWindowRequestedEventArgs>? _newWindowHandler;
    private EventHandler<PermissionRequestedEventArgs>? _permissionHandler;
    private EventHandler<DownloadRequestedEventArgs>? _downloadHandler;
    private EventHandler<KeyEventArgs>? _shortcutHandler;
    private WebViewShortcutRouter? _shortcutRouter;

    partial void InitializeShellPreset()
    {
        if (_newWindowHandler is not null)
            return;

        // App-shell preset: provide common host-side shell governance defaults.
        _newWindowHandler = (_, e) =>
        {
            if (e.Uri is null)
                return;

            e.Handled = true;
            _ = WebView.NavigateAsync(e.Uri);
        };

        _permissionHandler = (_, e) =>
        {
            if (e.PermissionKind == WebViewPermissionKind.Notifications)
            {
                e.State = PermissionState.Deny;
            }
        };

        _downloadHandler = (_, e) =>
        {
            if (e.Cancel || !string.IsNullOrWhiteSpace(e.DownloadPath))
                return;

            var fileName = string.IsNullOrWhiteSpace(e.SuggestedFileName) ? "download.bin" : e.SuggestedFileName!;
            e.DownloadPath = Path.Combine(Path.GetTempPath(), fileName);
        };

        _shortcutRouter = new WebViewShortcutRouter(WebView);
        _shortcutHandler = async (_, e) =>
        {
            if (_shortcutRouter is null)
                return;

            if (await _shortcutRouter.TryExecuteAsync(e))
                e.Handled = true;
        };

        WebView.NewWindowRequested += _newWindowHandler;
        WebView.PermissionRequested += _permissionHandler;
        WebView.DownloadRequested += _downloadHandler;
        KeyDown += _shortcutHandler;
    }

    partial void DisposeShellPreset()
    {
        if (_newWindowHandler is not null)
            WebView.NewWindowRequested -= _newWindowHandler;
        if (_permissionHandler is not null)
            WebView.PermissionRequested -= _permissionHandler;
        if (_downloadHandler is not null)
            WebView.DownloadRequested -= _downloadHandler;
        if (_shortcutHandler is not null)
            KeyDown -= _shortcutHandler;

        _newWindowHandler = null;
        _permissionHandler = null;
        _downloadHandler = null;
        _shortcutHandler = null;
        _shortcutRouter = null;
    }
}
