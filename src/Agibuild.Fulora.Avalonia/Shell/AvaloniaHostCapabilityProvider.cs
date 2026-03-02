using System;
using System.Runtime.InteropServices;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Avalonia-based implementation of <see cref="IWebViewHostCapabilityProvider"/> that binds
/// tray and menu operations to Avalonia <c>TrayIcon</c> and <c>NativeMenu</c> controls.
/// Non-tray/menu operations are delegated to an inner provider.
/// </summary>
public sealed class AvaloniaHostCapabilityProvider : IWebViewHostCapabilityProvider, IDisposable
{
    private readonly IWebViewHostCapabilityProvider _inner;
    private readonly AvaloniaTrayManager _trayManager;
    private readonly AvaloniaMenuManager _menuManager;
    private readonly bool _isDesktopPlatform;

    /// <summary>
    /// Creates a new Avalonia host capability provider.
    /// </summary>
    /// <param name="inner">Inner provider for non-tray/menu operations (clipboard, file dialog, etc.).</param>
    /// <param name="iconResolver">
    /// Optional tray icon resolver. Defaults to a composite resolver supporting avares:// and file paths.
    /// </param>
    public AvaloniaHostCapabilityProvider(
        IWebViewHostCapabilityProvider inner,
        ITrayIconResolver? iconResolver = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _isDesktopPlatform = IsDesktop();
        _trayManager = new AvaloniaTrayManager(iconResolver ?? CompositeIconResolver.CreateDefault());
        _menuManager = new AvaloniaMenuManager();
    }

    /// <summary>
    /// Raised when the tray icon is clicked.
    /// </summary>
    public event Action<TrayInteractionEventArgs>? TrayClicked
    {
        add => _trayManager.Clicked += value;
        remove => _trayManager.Clicked -= value;
    }

    /// <summary>
    /// Raised when a native menu item is clicked.
    /// </summary>
    public event Action<MenuInteractionEventArgs>? MenuItemClicked
    {
        add => _menuManager.MenuItemClicked += value;
        remove => _menuManager.MenuItemClicked -= value;
    }

    /// <summary>
    /// Gets the current Avalonia NativeMenu managed by this provider.
    /// </summary>
    public Avalonia.Controls.NativeMenu? NativeMenu => _menuManager.Menu;

    public string? ReadClipboardText() => _inner.ReadClipboardText();

    public void WriteClipboardText(string text) => _inner.WriteClipboardText(text);

    public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
        => _inner.ShowOpenFileDialog(request);

    public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
        => _inner.ShowSaveFileDialog(request);

    public void OpenExternal(Uri uri) => _inner.OpenExternal(uri);

    public void ShowNotification(WebViewNotificationRequest request) => _inner.ShowNotification(request);

    public void ApplyMenuModel(WebViewMenuModelRequest request)
    {
        if (!_isDesktopPlatform) return;
        _menuManager.ApplyMenuModel(request);
    }

    public void UpdateTrayState(WebViewTrayStateRequest request)
    {
        if (!_isDesktopPlatform) return;
        _trayManager.UpdateTrayState(request);
    }

    public void ExecuteSystemAction(WebViewSystemActionRequest request) => _inner.ExecuteSystemAction(request);

    public void Dispose()
    {
        _trayManager.Dispose();
        _menuManager.Dispose();
    }

    private static bool IsDesktop()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
           RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
           RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
