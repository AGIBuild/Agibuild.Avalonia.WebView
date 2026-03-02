using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Manages an Avalonia <see cref="TrayIcon"/> lifecycle: create, update, hide, dispose.
/// All mutations are dispatched to the Avalonia UI thread.
/// </summary>
internal sealed class AvaloniaTrayManager : IDisposable
{
    private readonly ITrayIconResolver _iconResolver;
    private TrayIcon? _trayIcon;
    private bool _disposed;

    public AvaloniaTrayManager(ITrayIconResolver iconResolver)
    {
        _iconResolver = iconResolver ?? throw new ArgumentNullException(nameof(iconResolver));
    }

    /// <summary>
    /// Raised when the tray icon is clicked.
    /// </summary>
    public event Action<TrayInteractionEventArgs>? Clicked;

    /// <summary>
    /// Applies a tray state update. Creates the tray icon if needed, or hides/updates it.
    /// </summary>
    public void UpdateTrayState(WebViewTrayStateRequest request)
    {
        if (_disposed) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateTrayStateCore(request);
        }
        else
        {
            Dispatcher.UIThread.Post(() => UpdateTrayStateCore(request));
        }
    }

    private void UpdateTrayStateCore(WebViewTrayStateRequest request)
    {
        if (_disposed) return;

        if (!request.IsVisible)
        {
            if (_trayIcon is not null)
                _trayIcon.IsVisible = false;
            return;
        }

        if (_trayIcon is null)
        {
            _trayIcon = new TrayIcon();
            _trayIcon.Clicked += OnTrayIconClicked;
        }

        _trayIcon.IsVisible = true;

        if (request.Tooltip is not null)
            _trayIcon.ToolTipText = request.Tooltip;

        var icon = _iconResolver.Resolve(request.IconPath);
        if (icon is not null)
            _trayIcon.Icon = icon;
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        Clicked?.Invoke(new TrayInteractionEventArgs());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_trayIcon is not null)
        {
            _trayIcon.Clicked -= OnTrayIconClicked;
            _trayIcon.IsVisible = false;
            _trayIcon = null;
        }
    }
}

/// <summary>
/// Event args for tray interaction events.
/// </summary>
public sealed class TrayInteractionEventArgs : EventArgs
{
    /// <summary>UTC timestamp of the interaction.</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}
