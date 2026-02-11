using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// An Avalonia control that embeds a platform-native WebView.
/// <para>
/// Usage in XAML:
/// <code>&lt;agw:WebView Source="https://example.com" /&gt;</code>
/// </para>
/// </summary>
public class WebView : NativeControlHost
{
    // ---------------------------------------------------------------------------
    //  Avalonia StyledProperties
    // ---------------------------------------------------------------------------

    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<WebView, Uri?>(nameof(Source));

    public static readonly DirectProperty<WebView, bool> CanGoBackProperty =
        AvaloniaProperty.RegisterDirect<WebView, bool>(nameof(CanGoBack), o => o.CanGoBack);

    public static readonly DirectProperty<WebView, bool> CanGoForwardProperty =
        AvaloniaProperty.RegisterDirect<WebView, bool>(nameof(CanGoForward), o => o.CanGoForward);

    public static readonly DirectProperty<WebView, bool> IsLoadingProperty =
        AvaloniaProperty.RegisterDirect<WebView, bool>(nameof(IsLoading), o => o.IsLoading);

    // ---------------------------------------------------------------------------
    //  Internal state
    // ---------------------------------------------------------------------------

    private WebViewCore? _core;
    private bool _coreAttached;
    private bool _adapterUnavailable;
    private ILoggerFactory? _loggerFactory;

    // ---------------------------------------------------------------------------
    //  Constructor
    // ---------------------------------------------------------------------------

    static WebView()
    {
        SourceProperty.Changed.AddClassHandler<WebView>((wv, e) => wv.OnSourceChanged(e));
    }

    // ---------------------------------------------------------------------------
    //  Public surface (mirrors IWebView)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the <see cref="ILoggerFactory"/> used to create loggers for internal diagnostics.
    /// Set this before the control is attached to the visual tree for full coverage.
    /// When <c>null</c>, logging is disabled (NullLogger).
    /// </summary>
    public ILoggerFactory? LoggerFactory
    {
        get => _loggerFactory;
        set => _loggerFactory = value;
    }

    /// <summary>
    /// Gets or sets the current navigation URI. Setting this triggers a navigation.
    /// </summary>
    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool CanGoBack => _core?.CanGoBack ?? false;

    public bool CanGoForward => _core?.CanGoForward ?? false;

    /// <summary>
    /// <c>true</c> while a navigation is in progress.
    /// </summary>
    public bool IsLoading => _core?.IsLoading ?? false;

    /// <summary>
    /// <c>true</c> when a platform adapter is available and the WebView is functional.
    /// <c>false</c> on platforms without a registered adapter (e.g. Android before the adapter is implemented).
    /// </summary>
    public bool IsAvailable => _core is not null && _coreAttached;

    /// <summary>
    /// The channel id for web message bridge isolation.
    /// Only valid after the control is attached to the visual tree.
    /// </summary>
    public Guid ChannelId => _core?.ChannelId ?? Guid.Empty;

    // --- Navigation ---

    public Task NavigateAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        EnsureCore();
        return _core!.NavigateAsync(uri);
    }

    public Task NavigateToStringAsync(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        EnsureCore();
        return _core!.NavigateToStringAsync(html);
    }

    public Task NavigateToStringAsync(string html, Uri? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(html);
        EnsureCore();
        return _core!.NavigateToStringAsync(html, baseUrl);
    }

    /// <summary>
    /// Returns a cookie manager if the underlying adapter supports it; otherwise <c>null</c>.
    /// </summary>
    public ICookieManager? TryGetCookieManager()
    {
        return _core?.TryGetCookieManager();
    }

    /// <summary>
    /// Returns a command manager if the underlying adapter supports it; otherwise <c>null</c>.
    /// </summary>
    public ICommandManager? TryGetCommandManager()
    {
        return _core?.TryGetCommandManager();
    }

    /// <summary>
    /// Gets the RPC service for bidirectional JS ↔ C# method calls.
    /// Returns <c>null</c> until the WebMessage bridge is enabled.
    /// </summary>
    public IWebViewRpcService? Rpc => _core?.Rpc;

    /// <summary>
    /// Captures a screenshot of the current viewport as a PNG byte array.
    /// Throws <see cref="NotSupportedException"/> if the adapter does not support screenshots.
    /// </summary>
    public Task<byte[]> CaptureScreenshotAsync()
    {
        if (_core is null)
            throw new InvalidOperationException("WebView is not initialized.");
        return _core.CaptureScreenshotAsync();
    }

    /// <summary>
    /// Prints the current page to a PDF byte array.
    /// Throws <see cref="NotSupportedException"/> if the adapter does not support printing.
    /// </summary>
    public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null)
    {
        if (_core is null)
            throw new InvalidOperationException("WebView is not initialized.");
        return _core.PrintToPdfAsync(options);
    }

    /// <summary>
    /// Returns the underlying platform WebView handle, or <c>null</c> if not available.
    /// </summary>
    public IPlatformHandle? TryGetWebViewHandle()
    {
        return _core?.TryGetWebViewHandle();
    }

    /// <summary>
    /// Sets the custom User-Agent string at runtime.
    /// Pass <c>null</c> to revert to the platform default.
    /// </summary>
    public void SetCustomUserAgent(string? userAgent)
    {
        _core?.SetCustomUserAgent(userAgent);
    }

    /// <summary>
    /// Enables the WebMessage bridge with the specified policy options.
    /// After this call, incoming <c>WebMessageReceived</c> events are filtered by origin, protocol, and channel.
    /// Must be called on the UI thread after the control is attached.
    /// </summary>
    public void EnableWebMessageBridge(WebMessageBridgeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureCore();
        _core!.EnableWebMessageBridge(options);
    }

    /// <summary>
    /// Disables the WebMessage bridge. Subsequent incoming web messages will be silently dropped.
    /// Must be called on the UI thread.
    /// </summary>
    public void DisableWebMessageBridge()
    {
        EnsureCore();
        _core!.DisableWebMessageBridge();
    }

    public Task<string?> InvokeScriptAsync(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        EnsureCore();
        return _core!.InvokeScriptAsync(script);
    }

    public bool GoBack()
    {
        if (_core is null) return false;
        return _core.GoBack();
    }

    public bool GoForward()
    {
        if (_core is null) return false;
        return _core.GoForward();
    }

    public bool Refresh()
    {
        if (_core is null) return false;
        return _core.Refresh();
    }

    public bool Stop()
    {
        if (_core is null) return false;
        return _core.Stop();
    }

    // --- Events (bubbled from WebViewCore) ---

    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
    public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;
    public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested;
    public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated;
    public event EventHandler? AdapterDestroyed;

    // ---------------------------------------------------------------------------
    //  NativeControlHost lifecycle
    // ---------------------------------------------------------------------------

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);

        try
        {
            var dispatcher = new AvaloniaWebViewDispatcher();
            var effectiveLoggerFactory = _loggerFactory ?? WebViewEnvironment.LoggerFactory;
            var logger = effectiveLoggerFactory?.CreateLogger<WebViewCore>()
                         ?? (ILogger<WebViewCore>)NullLogger<WebViewCore>.Instance;

            _core = WebViewCore.CreateForControl(dispatcher, logger);

            // Subscribe before Attach so we receive AdapterCreated raised during Attach().
            SubscribeCoreEvents();

            _core.Attach(handle);
            _coreAttached = true;

            // If Source was set before attachment, navigate now (after AdapterCreated).
            var pendingSource = Source;
            if (pendingSource is not null)
            {
                _ = _core.NavigateAsync(pendingSource);
            }
        }
        catch (PlatformNotSupportedException)
        {
            // No adapter for this platform — degrade gracefully (empty control).
            _core?.Dispose();
            _core = null;
            _coreAttached = false;
            _adapterUnavailable = true;
        }
        catch
        {
            _core?.Dispose();
            _core = null;
            _coreAttached = false;
            throw;
        }

        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        UnsubscribeCoreEvents();

        if (_coreAttached)
        {
            _core?.Detach();
            _coreAttached = false;
        }

        _core?.Dispose();
        _core = null;

        base.DestroyNativeControlCore(control);
    }

    // ---------------------------------------------------------------------------
    //  Private helpers
    // ---------------------------------------------------------------------------

    private void OnSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_core is null || !_coreAttached)
        {
            return;
        }

        if (e.NewValue is Uri newUri)
        {
            _ = _core.NavigateAsync(newUri);
        }
    }

    private void EnsureCore()
    {
        if (_core is null)
        {
            if (_adapterUnavailable)
            {
                throw new PlatformNotSupportedException(
                    "No WebView adapter is available for the current platform. " +
                    "WebView functionality is not supported.");
            }

            throw new InvalidOperationException(
                "WebView is not yet attached to the visual tree. " +
                "Wait until the control is loaded before calling navigation methods.");
        }
    }

    private void SubscribeCoreEvents()
    {
        if (_core is null) return;

        _core.NavigationStarted += OnCoreNavigationStarted;
        _core.NavigationCompleted += OnCoreNavigationCompleted;
        _core.NewWindowRequested += OnCoreNewWindowRequested;
        _core.WebMessageReceived += OnCoreWebMessageReceived;
        _core.WebResourceRequested += OnCoreWebResourceRequested;
        _core.EnvironmentRequested += OnCoreEnvironmentRequested;
        _core.DownloadRequested += OnCoreDownloadRequested;
        _core.PermissionRequested += OnCorePermissionRequested;
        _core.AdapterCreated += OnCoreAdapterCreated;
        _core.AdapterDestroyed += OnCoreAdapterDestroyed;
    }

    private void UnsubscribeCoreEvents()
    {
        if (_core is null) return;

        _core.NavigationStarted -= OnCoreNavigationStarted;
        _core.NavigationCompleted -= OnCoreNavigationCompleted;
        _core.NewWindowRequested -= OnCoreNewWindowRequested;
        _core.WebMessageReceived -= OnCoreWebMessageReceived;
        _core.WebResourceRequested -= OnCoreWebResourceRequested;
        _core.EnvironmentRequested -= OnCoreEnvironmentRequested;
        _core.DownloadRequested -= OnCoreDownloadRequested;
        _core.PermissionRequested -= OnCorePermissionRequested;
        _core.AdapterCreated -= OnCoreAdapterCreated;
        _core.AdapterDestroyed -= OnCoreAdapterDestroyed;
    }

    private void OnCoreNavigationStarted(object? sender, NavigationStartingEventArgs e)
    {
        NavigationStarted?.Invoke(this, e);
        RaisePropertyChanged(IsLoadingProperty, false, true);
    }

    private void OnCoreNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
    {
        NavigationCompleted?.Invoke(this, e);
        RaisePropertyChanged(IsLoadingProperty, true, false);
        RaisePropertyChanged(CanGoBackProperty, !CanGoBack, CanGoBack);
        RaisePropertyChanged(CanGoForwardProperty, !CanGoForward, CanGoForward);
    }

    private void OnCoreNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        NewWindowRequested?.Invoke(this, e);

        // If the consumer did not handle the event, navigate in the current view.
        if (!e.Handled && e.Uri is not null && _core is not null)
        {
            _ = _core.NavigateAsync(e.Uri);
        }
    }

    private void OnCoreWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
        => WebMessageReceived?.Invoke(this, e);

    private void OnCoreWebResourceRequested(object? sender, WebResourceRequestedEventArgs e)
        => WebResourceRequested?.Invoke(this, e);

    private void OnCoreEnvironmentRequested(object? sender, EnvironmentRequestedEventArgs e)
        => EnvironmentRequested?.Invoke(this, e);

    private void OnCoreDownloadRequested(object? sender, DownloadRequestedEventArgs e)
        => DownloadRequested?.Invoke(this, e);

    private void OnCorePermissionRequested(object? sender, PermissionRequestedEventArgs e)
        => PermissionRequested?.Invoke(this, e);

    private void OnCoreAdapterCreated(object? sender, AdapterCreatedEventArgs e)
        => AdapterCreated?.Invoke(this, e);

    private void OnCoreAdapterDestroyed(object? sender, EventArgs e)
        => AdapterDestroyed?.Invoke(this, EventArgs.Empty);
}
