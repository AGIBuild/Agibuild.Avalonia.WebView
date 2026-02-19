using System.IO;
using Agibuild.Avalonia.WebView;

namespace HybridApp.Desktop;

public partial class MainWindow
{
    private EventHandler<NewWindowRequestedEventArgs>? _newWindowHandler;
    private EventHandler<PermissionRequestedEventArgs>? _permissionHandler;
    private EventHandler<DownloadRequestedEventArgs>? _downloadHandler;

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

        WebView.NewWindowRequested += _newWindowHandler;
        WebView.PermissionRequested += _permissionHandler;
        WebView.DownloadRequested += _downloadHandler;
    }

    partial void DisposeShellPreset()
    {
        if (_newWindowHandler is not null)
            WebView.NewWindowRequested -= _newWindowHandler;
        if (_permissionHandler is not null)
            WebView.PermissionRequested -= _permissionHandler;
        if (_downloadHandler is not null)
            WebView.DownloadRequested -= _downloadHandler;

        _newWindowHandler = null;
        _permissionHandler = null;
        _downloadHandler = null;
    }
}
