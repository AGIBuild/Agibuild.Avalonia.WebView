using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Controls;

/// <summary>
/// Avalonia-based implementation of <see cref="IDialogHost"/> for E2E testing.
/// Wraps a real Avalonia <see cref="Window"/> to verify WebDialog/AuthBroker flows.
/// </summary>
internal sealed class AvaloniaDialogHost : IDialogHost
{
    private readonly Window _window;
    private bool _closed;

    public AvaloniaDialogHost()
    {
        _window = new Window
        {
            Width = 800,
            Height = 600,
            Title = "WebDialog",
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _window.Closing += (_, _) =>
        {
            if (!_closed)
            {
                _closed = true;
                HostClosing?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    public string? Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    public bool CanUserResize
    {
        get => _window.CanResize;
        set => _window.CanResize = value;
    }

    public event EventHandler? HostClosing;

    public void Show()
    {
        _window.Show();
    }

    public bool ShowWithOwner(IPlatformHandle owner)
    {
        // Avalonia doesn't directly support showing with a raw platform handle as owner.
        // Show as a standalone window instead.
        _window.Show();
        return true;
    }

    public void Close()
    {
        if (_closed) return;
        _closed = true;
        _window.Close();
    }

    public bool Resize(int width, int height)
    {
        _window.Width = width;
        _window.Height = height;
        return true;
    }

    public bool Move(int x, int y)
    {
        _window.Position = new PixelPoint(x, y);
        return true;
    }

    /// <summary>
    /// Gets the underlying Avalonia Window for native control hosting.
    /// </summary>
    public Window Window => _window;
}

/// <summary>
/// Factory that creates real Avalonia-hosted WebDialogs for E2E testing.
/// </summary>
internal sealed class AvaloniaWebDialogFactory : IWebDialogFactory
{
    private readonly IWebViewDispatcher _dispatcher;

    public AvaloniaWebDialogFactory(IWebViewDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
    {
        var host = new AvaloniaDialogHost();

        // Try to create a real platform adapter for the dialog.
        if (!Adapters.Abstractions.WebViewAdapterRegistry.TryCreateForCurrentPlatform(
                out var adapter, out var reason))
        {
            throw new InvalidOperationException($"Cannot create WebDialog: {reason}");
        }

        var dialog = new WebDialog(host, adapter, _dispatcher);
        return dialog;
    }
}
