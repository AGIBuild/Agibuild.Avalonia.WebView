using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Production implementation of <see cref="IWebDialog"/> backed by an Avalonia <see cref="Window"/>
/// containing a <see cref="WebView"/> control. The WebView control handles all native platform
/// adapter creation, NativeControlHost lifecycle, and attachment automatically.
/// <para>
/// Usage:
/// <code>
/// var dialog = new AvaloniaWebDialog();
/// dialog.Title = "Sign In";
/// dialog.Show();
/// await dialog.NavigateAsync(new Uri("https://example.com"));
/// </code>
/// </para>
/// </summary>
public sealed class AvaloniaWebDialog : IWebDialog
{
    private readonly Window _window;
    private readonly WebView _webView;
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _disposed;
    private bool _shown;

    public AvaloniaWebDialog(IWebViewEnvironmentOptions? options = null)
    {
        // Apply environment options BEFORE creating the WebView so that
        // WebViewCore picks them up during construction/attachment.
        if (options is not null)
        {
            var original = WebViewEnvironment.Options;
            WebViewEnvironment.Options = new WebViewEnvironmentOptions
            {
                EnableDevTools = options.EnableDevTools,
                UseEphemeralSession = options.UseEphemeralSession,
                CustomUserAgent = options.CustomUserAgent
            };
            // Note: This sets the global options temporarily.
            // A future improvement could scope options per-instance.
        }

        _webView = new WebView();

        // Listen for the first NavigationCompleted to signal readiness.
        // This fires when the WebView's NativeControlHost has been created
        // and the adapter is attached and ready for navigation.
        _webView.NavigationStarted += OnFirstNavigationEvent;

        _window = new Window
        {
            Content = _webView,
            Width = 800,
            Height = 600,
            Title = "WebDialog",
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _window.Closing += OnWindowClosing;

        // When the window is opened, the WebView attaches to the visual tree,
        // which triggers NativeControlHost.CreateNativeControlCore → adapter attach.
        // Mark ready once the layout pass completes.
        _window.Opened += OnWindowOpened;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        _window.Opened -= OnWindowOpened;
        // After the window is opened, the visual tree is attached and
        // NativeControlHost.CreateNativeControlCore has been called.
        // Use a low-priority dispatch to allow the layout to finish.
        Dispatcher.UIThread.Post(() => _readyTcs.TrySetResult(), DispatcherPriority.Loaded);
    }

    private void OnFirstNavigationEvent(object? sender, NavigationStartingEventArgs e)
    {
        _webView.NavigationStarted -= OnFirstNavigationEvent;
        _readyTcs.TrySetResult();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_disposed)
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }
    }

    // ==== IWebDialog — Window Management ====
    // All window-management methods are thread-safe: if called from a non-UI thread
    // the operation is marshalled to the Avalonia UI thread automatically.

    public string? Title
    {
        get => _window.Title;
        set => RunOnUIThread(() => _window.Title = value);
    }

    public bool CanUserResize
    {
        get => _window.CanResize;
        set => RunOnUIThread(() => _window.CanResize = value);
    }

    public void Show()
    {
        _shown = true;
        RunOnUIThread(() => _window.Show());
    }

    public bool Show(IPlatformHandle owner)
    {
        _shown = true;
        RunOnUIThread(() => _window.Show());
        return true;
    }

    public void Close()
    {
        if (_disposed) return;
        RunOnUIThread(() => _window.Close());
    }

    public bool Resize(int width, int height)
    {
        RunOnUIThread(() =>
        {
            _window.Width = width;
            _window.Height = height;
        });
        return true;
    }

    public bool Move(int x, int y)
    {
        RunOnUIThread(() => _window.Position = new PixelPoint(x, y));
        return true;
    }

    public event EventHandler? Closing;

    // ==== IWebView — Delegated to embedded WebView control ====

    public Uri Source
    {
        get => _webView.Source ?? new Uri("about:blank");
        set => _webView.Source = value;
    }

    public bool CanGoBack => _webView.CanGoBack;
    public bool CanGoForward => _webView.CanGoForward;
    public bool IsLoading => _webView.IsLoading;
    public Guid ChannelId => _webView.ChannelId;

    public async Task NavigateAsync(Uri uri)
    {
        // Do NOT use ConfigureAwait(false) here: _webView is an Avalonia UI control
        // and must be accessed on the UI thread. Keeping the SynchronizationContext
        // ensures the continuation runs on the Avalonia dispatcher.
        await EnsureReadyAsync();
        ThrowIfDisposed();
        await _webView.NavigateAsync(uri);
    }

    public async Task NavigateToStringAsync(string html)
    {
        await EnsureReadyAsync();
        ThrowIfDisposed();
        await _webView.NavigateToStringAsync(html);
    }

    public async Task NavigateToStringAsync(string html, Uri? baseUrl)
    {
        await EnsureReadyAsync();
        ThrowIfDisposed();
        await _webView.NavigateToStringAsync(html, baseUrl);
    }

    public async Task<string?> InvokeScriptAsync(string script)
    {
        await EnsureReadyAsync();
        ThrowIfDisposed();
        return await _webView.InvokeScriptAsync(script);
    }

    public bool GoBack() => _webView.GoBack();
    public bool GoForward() => _webView.GoForward();
    public bool Refresh() => _webView.Refresh();
    public bool Stop() => _webView.Stop();

    public ICookieManager? TryGetCookieManager() => _webView.TryGetCookieManager();
    public ICommandManager? TryGetCommandManager() => null;

    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted
    {
        add => _webView.NavigationStarted += value;
        remove => _webView.NavigationStarted -= value;
    }

    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted
    {
        add => _webView.NavigationCompleted += value;
        remove => _webView.NavigationCompleted -= value;
    }

    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested
    {
        add => _webView.NewWindowRequested += value;
        remove => _webView.NewWindowRequested -= value;
    }

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived
    {
        add => _webView.WebMessageReceived += value;
        remove => _webView.WebMessageReceived -= value;
    }

    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested
    {
        add => _webView.WebResourceRequested += value;
        remove => _webView.WebResourceRequested -= value;
    }

    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested
    {
        add => _webView.EnvironmentRequested += value;
        remove => _webView.EnvironmentRequested -= value;
    }

    public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated
    {
        add => _webView.AdapterCreated += value;
        remove => _webView.AdapterCreated -= value;
    }

    public event EventHandler? AdapterDestroyed
    {
        add => _webView.AdapterDestroyed += value;
        remove => _webView.AdapterDestroyed -= value;
    }

    // ==== IDisposable ====

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _readyTcs.TrySetResult(); // unblock any waiters
        RunOnUIThread(() =>
        {
            _window.Closing -= OnWindowClosing;
            _window.Close();
        });
    }

    // ==== Private ====

    private Task EnsureReadyAsync()
    {
        ThrowIfDisposed();
        if (_readyTcs.Task.IsCompleted) return Task.CompletedTask;
        if (!_shown)
        {
            throw new InvalidOperationException(
                "WebDialog must be shown (Show()) before calling navigation methods. " +
                "The WebView needs to be attached to the visual tree first.");
        }
        return _readyTcs.Task;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Executes <paramref name="action"/> on the Avalonia UI thread.
    /// If already on the UI thread, runs synchronously; otherwise posts to the dispatcher.
    /// </summary>
    private static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
