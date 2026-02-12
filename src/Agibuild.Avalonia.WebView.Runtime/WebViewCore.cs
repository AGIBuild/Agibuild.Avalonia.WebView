using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Avalonia.WebView;

public sealed class WebViewCore : IWebView, IWebViewAdapterHost, IDisposable
{
    private static readonly Uri AboutBlank = new("about:blank");

    private readonly IWebViewAdapter _adapter;
    private readonly IWebViewDispatcher _dispatcher;
    private readonly ILogger<WebViewCore> _logger;

    /// <summary>
    /// Creates a new <see cref="IWebView"/> using the default platform adapter for the current OS.
    /// This is the recommended entry-point; callers never need to reference the internal adapter types.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IWebView CreateDefault(IWebViewDispatcher dispatcher)
        => CreateDefault(dispatcher, NullLogger<WebViewCore>.Instance);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IWebView CreateDefault(IWebViewDispatcher dispatcher, ILogger<WebViewCore> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(logger);
        return new WebViewCore(WebViewAdapterFactory.CreateDefaultAdapter(), dispatcher, logger);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static WebViewCore CreateForControl(IWebViewDispatcher dispatcher, ILogger<WebViewCore>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        return new WebViewCore(WebViewAdapterFactory.CreateDefaultAdapter(), dispatcher, logger ?? NullLogger<WebViewCore>.Instance);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal void Attach(global::Avalonia.Platform.IPlatformHandle parentHandle)
    {
        _logger.LogDebug("Attach: parentHandle.HandleDescriptor={Descriptor}", parentHandle.HandleDescriptor);
        _adapter.Attach(parentHandle);
        _logger.LogDebug("Attach: completed");

        // Raise AdapterCreated after successful attach, before any pending navigation.
        var handle = TryGetWebViewHandle();
        _logger.LogDebug("AdapterCreated: raising with handle={HasHandle}", handle is not null);
        AdapterCreated?.Invoke(this, new AdapterCreatedEventArgs(handle));
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal void Detach()
    {
        _logger.LogDebug("Detach: begin");
        RaiseAdapterDestroyedOnce();
        _adapter.Detach();
        _logger.LogDebug("Detach: completed");
    }

    // Volatile: checked off-UI-thread in adapter callbacks before dispatching.
    private volatile bool _disposed;

    // Guards at-most-once firing of AdapterDestroyed.
    private bool _adapterDestroyed;

    // Only accessed on the UI thread (all paths go through _dispatcher).
    private NavigationOperation? _activeNavigation;
    private Uri _source;

    private readonly ICookieManager? _cookieManager;
    private readonly ICommandManager? _commandManager;
    private readonly IScreenshotAdapter? _screenshotAdapter;
    private readonly IPrintAdapter? _printAdapter;
    private readonly IFindInPageAdapter? _findInPageAdapter;
    private readonly IZoomAdapter? _zoomAdapter;
    private readonly IPreloadScriptAdapter? _preloadScriptAdapter;
    private readonly IContextMenuAdapter? _contextMenuAdapter;

    private bool _webMessageBridgeEnabled;
    private IWebMessagePolicy? _webMessagePolicy;
    private IWebMessageDropDiagnosticsSink? _webMessageDropDiagnosticsSink;
    private WebViewRpcService? _rpcService;
    private RuntimeBridgeService? _bridgeService;
    private SpaHostingService? _spaHostingService;

    internal WebViewCore(IWebViewAdapter adapter, IWebViewDispatcher dispatcher)
        : this(adapter, dispatcher, NullLogger<WebViewCore>.Instance)
    {
    }

    internal WebViewCore(IWebViewAdapter adapter, IWebViewDispatcher dispatcher, ILogger<WebViewCore> logger)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? NullLogger<WebViewCore>.Instance;

        _source = AboutBlank;
        ChannelId = Guid.NewGuid();

        _logger.LogDebug("WebViewCore created: channelId={ChannelId}, adapter={AdapterType}",
            ChannelId, adapter.GetType().FullName);

        _adapter.Initialize(this);
        _logger.LogDebug("Adapter initialized");

        // Apply global environment options if adapter supports them.
        if (_adapter is IWebViewAdapterOptions adapterOptions)
        {
            var envOptions = WebViewEnvironment.Options;
            adapterOptions.ApplyEnvironmentOptions(envOptions);
            _logger.LogDebug("Environment options applied: DevTools={DevTools}, Ephemeral={Ephemeral}, UA={UA}",
                envOptions.EnableDevTools, envOptions.UseEphemeralSession, envOptions.CustomUserAgent ?? "(default)");
        }

        _cookieManager = adapter is ICookieAdapter cookieAdapter
            ? new RuntimeCookieManager(cookieAdapter, this, _dispatcher, _logger)
            : null;
        _logger.LogDebug("Cookie support: {Supported}", _cookieManager is not null);

        // Register custom schemes if adapter supports it.
        if (_adapter is ICustomSchemeAdapter customSchemeAdapter)
        {
            var schemes = WebViewEnvironment.Options.CustomSchemes;
            if (schemes.Count > 0)
            {
                customSchemeAdapter.RegisterCustomSchemes(schemes);
                _logger.LogDebug("Custom schemes registered: {Count}", schemes.Count);
            }
        }

        // Subscribe to download events if adapter supports it.
        if (_adapter is IDownloadAdapter downloadAdapter)
        {
            downloadAdapter.DownloadRequested += OnAdapterDownloadRequested;
            _logger.LogDebug("Download support: enabled");
        }

        // Subscribe to permission events if adapter supports it.
        if (_adapter is IPermissionAdapter permissionAdapter)
        {
            permissionAdapter.PermissionRequested += OnAdapterPermissionRequested;
            _logger.LogDebug("Permission support: enabled");
        }

        // Detect command support.
        _commandManager = _adapter is ICommandAdapter commandAdapter
            ? new RuntimeCommandManager(commandAdapter)
            : null;
        _logger.LogDebug("Command support: {Supported}", _commandManager is not null);

        // Detect screenshot support.
        _screenshotAdapter = _adapter as IScreenshotAdapter;
        _logger.LogDebug("Screenshot support: {Supported}", _screenshotAdapter is not null);

        // Detect print support.
        _printAdapter = _adapter as IPrintAdapter;
        _logger.LogDebug("Print support: {Supported}", _printAdapter is not null);

        // Detect find-in-page support.
        _findInPageAdapter = _adapter as IFindInPageAdapter;
        _logger.LogDebug("Find-in-page support: {Supported}", _findInPageAdapter is not null);

        // Detect zoom support.
        _zoomAdapter = _adapter as IZoomAdapter;
        if (_zoomAdapter is not null)
        {
            _zoomAdapter.ZoomFactorChanged += OnAdapterZoomFactorChanged;
        }
        _logger.LogDebug("Zoom support: {Supported}", _zoomAdapter is not null);

        // Detect preload script support and apply global scripts.
        _preloadScriptAdapter = _adapter as IPreloadScriptAdapter;
        if (_preloadScriptAdapter is not null)
        {
            var globalScripts = WebViewEnvironment.Options.PreloadScripts;
            foreach (var script in globalScripts)
            {
                _preloadScriptAdapter.AddPreloadScript(script);
            }
            if (globalScripts.Count > 0)
                _logger.LogDebug("Global preload scripts applied: {Count}", globalScripts.Count);
        }
        _logger.LogDebug("Preload script support: {Supported}", _preloadScriptAdapter is not null);

        // Subscribe to context menu events if adapter supports it.
        _contextMenuAdapter = _adapter as IContextMenuAdapter;
        if (_contextMenuAdapter is not null)
        {
            _contextMenuAdapter.ContextMenuRequested += OnAdapterContextMenuRequested;
        }
        _logger.LogDebug("Context menu support: {Supported}", _contextMenuAdapter is not null);

        _adapter.NavigationCompleted += OnAdapterNavigationCompleted;
        _adapter.NewWindowRequested += OnAdapterNewWindowRequested;
        _adapter.WebMessageReceived += OnAdapterWebMessageReceived;
        _adapter.WebResourceRequested += OnAdapterWebResourceRequested;
        _adapter.EnvironmentRequested += OnAdapterEnvironmentRequested;
    }

    public Uri Source
    {
        get => _source;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfDisposed();
            ThrowIfNotOnUiThread(nameof(Source));

            _logger.LogDebug("Source set: {Uri}", value);
            SetSourceInternal(value);

            // Source is a sync API surface; we still start navigation to keep semantics consistent.
            _ = StartNavigationCoreAsync(
                requestUri: value,
                adapterInvoke: navigationId => _adapter.NavigateAsync(navigationId, value),
                updateSource: false).ContinueWith(static _ => { }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    public bool CanGoBack => _adapter.CanGoBack;

    public bool CanGoForward => _adapter.CanGoForward;

    public bool IsLoading => _activeNavigation is not null;

    public Guid ChannelId { get; }

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Dispose: begin");

        // Raise AdapterDestroyed if not already raised during Detach().
        RaiseAdapterDestroyedOnce();

        _disposed = true;

        _adapter.NavigationCompleted -= OnAdapterNavigationCompleted;
        _adapter.NewWindowRequested -= OnAdapterNewWindowRequested;
        _adapter.WebMessageReceived -= OnAdapterWebMessageReceived;
        _adapter.WebResourceRequested -= OnAdapterWebResourceRequested;
        _adapter.EnvironmentRequested -= OnAdapterEnvironmentRequested;

        if (_adapter is IDownloadAdapter downloadAdapter)
            downloadAdapter.DownloadRequested -= OnAdapterDownloadRequested;
        if (_adapter is IPermissionAdapter permissionAdapter)
            permissionAdapter.PermissionRequested -= OnAdapterPermissionRequested;
        if (_zoomAdapter is not null)
            _zoomAdapter.ZoomFactorChanged -= OnAdapterZoomFactorChanged;

        if (_contextMenuAdapter is not null)
            _contextMenuAdapter.ContextMenuRequested -= OnAdapterContextMenuRequested;

        if (_activeNavigation is not null)
        {
            _logger.LogDebug("Dispose: faulting active navigation id={NavigationId}", _activeNavigation.NavigationId);
            // After disposal, async APIs must not hang. No events must be raised.
            _activeNavigation.TrySetFault(new ObjectDisposedException(nameof(WebViewCore)));
            _activeNavigation = null;
        }

        _bridgeService?.Dispose();
        _bridgeService = null;

        _spaHostingService?.Dispose();
        _spaHostingService = null;

        _logger.LogDebug("Dispose: completed");
    }

    public Task NavigateAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        _logger.LogDebug("NavigateAsync: {Uri}", uri);

        return InvokeAsyncOnUiThread(() => StartNavigationCoreAsync(
            requestUri: uri,
            adapterInvoke: navigationId => _adapter.NavigateAsync(navigationId, uri)));
    }

    public Task NavigateToStringAsync(string html)
        => NavigateToStringAsync(html, baseUrl: null);

    public Task NavigateToStringAsync(string html, Uri? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(html);
        var requestUri = baseUrl ?? AboutBlank;
        _logger.LogDebug("NavigateToStringAsync: html length={Length}, baseUrl={BaseUrl}", html.Length, baseUrl);

        return InvokeAsyncOnUiThread(() => StartNavigationCoreAsync(
            requestUri: requestUri,
            adapterInvoke: navigationId => _adapter.NavigateToStringAsync(navigationId, html, baseUrl)));
    }

    public Task<string?> InvokeScriptAsync(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        _logger.LogDebug("InvokeScriptAsync: script length={Length}", script.Length);

        if (_disposed)
        {
            return Task.FromException<string?>(new ObjectDisposedException(nameof(WebViewCore)));
        }

        return _dispatcher.CheckAccess()
            ? InvokeScriptOnUiThreadAsync(script)
            : _dispatcher.InvokeAsync(() => InvokeScriptOnUiThreadAsync(script));

        async Task<string?> InvokeScriptOnUiThreadAsync(string s)
        {
            ThrowIfDisposed();

            try
            {
                var result = await _adapter.InvokeScriptAsync(s).ConfigureAwait(false);
                _logger.LogDebug("InvokeScriptAsync: result length={Length}", result?.Length ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "InvokeScriptAsync: failed");
                throw new WebViewScriptException("Script execution failed.", ex);
            }
        }
    }

    public bool GoBack()
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(GoBack));

        if (!_adapter.CanGoBack)
        {
            _logger.LogDebug("GoBack: no history, skipped");
            return false;
        }

        var navigationId = StartCommandNavigation(requestUri: Source);
        if (navigationId == Guid.Empty)
        {
            _logger.LogDebug("GoBack: canceled by NavigationStarted handler");
            return false;
        }

        var accepted = _adapter.GoBack(navigationId);
        if (!accepted)
        {
            _logger.LogDebug("GoBack: adapter rejected, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            return false;
        }

        _logger.LogDebug("GoBack: started, id={NavigationId}", navigationId);
        return true;
    }

    public bool GoForward()
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(GoForward));

        if (!_adapter.CanGoForward)
        {
            _logger.LogDebug("GoForward: no forward history, skipped");
            return false;
        }

        var navigationId = StartCommandNavigation(requestUri: Source);
        if (navigationId == Guid.Empty)
        {
            _logger.LogDebug("GoForward: canceled by NavigationStarted handler");
            return false;
        }

        var accepted = _adapter.GoForward(navigationId);
        if (!accepted)
        {
            _logger.LogDebug("GoForward: adapter rejected, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            return false;
        }

        _logger.LogDebug("GoForward: started, id={NavigationId}", navigationId);
        return true;
    }

    public bool Refresh()
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(Refresh));

        var navigationId = StartCommandNavigation(requestUri: Source);
        if (navigationId == Guid.Empty)
        {
            _logger.LogDebug("Refresh: canceled by NavigationStarted handler");
            return false;
        }

        var accepted = _adapter.Refresh(navigationId);
        if (!accepted)
        {
            _logger.LogDebug("Refresh: adapter rejected, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            return false;
        }

        _logger.LogDebug("Refresh: started, id={NavigationId}", navigationId);
        return true;
    }

    public bool Stop()
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(Stop));

        if (_activeNavigation is null)
        {
            _logger.LogDebug("Stop: no active navigation");
            return false;
        }

        _logger.LogDebug("Stop: canceling active navigation id={NavigationId}", _activeNavigation.NavigationId);
        _adapter.Stop();
        CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
        return true;
    }

    ValueTask<NativeNavigationStartingDecision> IWebViewAdapterHost.OnNativeNavigationStartingAsync(NativeNavigationStartingInfo info)
    {
        _logger.LogDebug("OnNativeNavigationStarting: correlationId={CorrelationId}, uri={Uri}, isMainFrame={IsMainFrame}",
            info.CorrelationId, info.RequestUri, info.IsMainFrame);

        if (_disposed)
        {
            _logger.LogDebug("OnNativeNavigationStarting: disposed, denying");
            return ValueTask.FromResult(new NativeNavigationStartingDecision(IsAllowed: false, NavigationId: Guid.Empty));
        }

        if (_dispatcher.CheckAccess())
        {
            return ValueTask.FromResult(OnNativeNavigationStartingOnUiThread(info));
        }

        return new ValueTask<NativeNavigationStartingDecision>(
            _dispatcher.InvokeAsync(() => OnNativeNavigationStartingOnUiThread(info)));
    }

    private NativeNavigationStartingDecision OnNativeNavigationStartingOnUiThread(NativeNavigationStartingInfo info)
    {
        if (_disposed)
        {
            return new NativeNavigationStartingDecision(IsAllowed: false, NavigationId: Guid.Empty);
        }

        ThrowIfNotOnUiThread(nameof(IWebViewAdapterHost.OnNativeNavigationStartingAsync));

        // Sub-frame navigations are not part of the v1 contract surface.
        if (!info.IsMainFrame)
        {
            _logger.LogDebug("OnNativeNavigationStarting: sub-frame, auto-allow");
            return new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: Guid.Empty);
        }

        var requestUri = info.RequestUri.AbsoluteUri != AboutBlank.AbsoluteUri ? info.RequestUri : AboutBlank;

        // Redirects / subsequent navigation actions within the same correlation id stay within one NavigationId.
        if (_activeNavigation is not null && _activeNavigation.CorrelationId == info.CorrelationId)
        {
            if (_activeNavigation.RequestUri.AbsoluteUri == requestUri.AbsoluteUri)
            {
                _logger.LogDebug("OnNativeNavigationStarting: same-URL redirect, id={NavigationId}", _activeNavigation.NavigationId);
                return new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: _activeNavigation.NavigationId);
            }

            _activeNavigation.UpdateRequestUri(requestUri);
            SetSourceInternal(requestUri);

            var redirectArgs = new NavigationStartingEventArgs(_activeNavigation.NavigationId, requestUri);
            _logger.LogDebug("Event NavigationStarted (redirect): id={NavigationId}, uri={Uri}", _activeNavigation.NavigationId, requestUri);
            NavigationStarted?.Invoke(this, redirectArgs);

            if (redirectArgs.Cancel)
            {
                _logger.LogDebug("OnNativeNavigationStarting: redirect canceled by handler, id={NavigationId}", _activeNavigation.NavigationId);
                var activeNavigationId = _activeNavigation.NavigationId;
                CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
                return new NativeNavigationStartingDecision(IsAllowed: false, NavigationId: activeNavigationId);
            }

            return new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: _activeNavigation.NavigationId);
        }

        // New native navigation supersedes any active navigation.
        if (_activeNavigation is not null)
        {
            _logger.LogDebug("OnNativeNavigationStarting: superseding active navigation id={NavigationId}", _activeNavigation.NavigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Superseded, error: null);
        }

        SetSourceInternal(requestUri);

        var navigationId = Guid.NewGuid();
        _activeNavigation = new NavigationOperation(navigationId, correlationId: info.CorrelationId, requestUri);

        var startingArgs = new NavigationStartingEventArgs(navigationId, requestUri);
        _logger.LogDebug("Event NavigationStarted (native): id={NavigationId}, uri={Uri}", navigationId, requestUri);
        NavigationStarted?.Invoke(this, startingArgs);

        if (startingArgs.Cancel)
        {
            _logger.LogDebug("OnNativeNavigationStarting: canceled by handler, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            return new NativeNavigationStartingDecision(IsAllowed: false, NavigationId: navigationId);
        }

        _logger.LogDebug("OnNativeNavigationStarting: allowed, id={NavigationId}", navigationId);
        return new NativeNavigationStartingDecision(IsAllowed: true, NavigationId: navigationId);
    }

    public ICookieManager? TryGetCookieManager() => _cookieManager;

    public ICommandManager? TryGetCommandManager() => _commandManager;

    public IWebViewRpcService? Rpc => _rpcService;

    // ==================== DevTools ====================

    public void OpenDevTools()
    {
        ThrowIfDisposed();
        if (_adapter is IDevToolsAdapter devTools)
            devTools.OpenDevTools();
        else
            _logger.LogDebug("DevTools: adapter does not support runtime toggle");
    }

    public void CloseDevTools()
    {
        ThrowIfDisposed();
        if (_adapter is IDevToolsAdapter devTools)
            devTools.CloseDevTools();
    }

    public bool IsDevToolsOpen => _adapter is IDevToolsAdapter devTools && devTools.IsDevToolsOpen;

    // ==================== Bridge ====================

    public IBridgeService Bridge
    {
        get
        {
            ThrowIfDisposed();

            if (_bridgeService is not null)
                return _bridgeService;

            // Auto-enable bridge with defaults if needed.
            if (!_webMessageBridgeEnabled)
            {
                EnableWebMessageBridge(new WebMessageBridgeOptions());
            }

            _bridgeService = new RuntimeBridgeService(
                _rpcService!,
                script => InvokeScriptAsync(script),
                _logger,
                enableDevTools: WebViewEnvironment.Options.EnableDevTools);

            _logger.LogDebug("Bridge: auto-created RuntimeBridgeService");
            return _bridgeService;
        }
    }

    public Task<byte[]> CaptureScreenshotAsync()
    {
        ThrowIfDisposed();
        if (_screenshotAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support screenshot capture.");
        return _screenshotAdapter.CaptureScreenshotAsync();
    }

    public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null)
    {
        ThrowIfDisposed();
        if (_printAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support PDF printing.");
        return _printAdapter.PrintToPdfAsync(options);
    }

    // ==================== Zoom ====================

    private const double MinZoom = 0.25;
    private const double MaxZoom = 5.0;

    /// <summary>Raised when the zoom factor changes.</summary>
    public event EventHandler<double>? ZoomFactorChanged;

    /// <summary>
    /// Gets or sets the zoom factor (1.0 = 100%). Clamped to [0.25, 5.0].
    /// Returns 1.0 if the adapter does not support zoom.
    /// </summary>
    public double ZoomFactor
    {
        get => _zoomAdapter?.ZoomFactor ?? 1.0;
        set
        {
            ThrowIfDisposed();
            if (_zoomAdapter is null) return; // no-op without adapter
            _zoomAdapter.ZoomFactor = Math.Clamp(value, MinZoom, MaxZoom);
        }
    }

    private void OnAdapterZoomFactorChanged(object? sender, double newZoom)
    {
        if (_disposed) return;
        _ = _dispatcher.InvokeAsync(() => ZoomFactorChanged?.Invoke(this, newZoom));
    }

    /// <summary>Raised when the user triggers a context menu (right-click, long-press).</summary>
    public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested;

    private void OnAdapterContextMenuRequested(object? sender, ContextMenuRequestedEventArgs e)
    {
        if (_disposed) return;
        _ = _dispatcher.InvokeAsync(() => ContextMenuRequested?.Invoke(this, e));
    }

    /// <summary>
    /// Searches the current page for the given text.
    /// </summary>
    /// <param name="text">The search text. Must not be null or empty.</param>
    /// <param name="options">Optional search options (case sensitivity, direction).</param>
    /// <returns>A <see cref="FindInPageResult"/> with match count and active index.</returns>
    /// <exception cref="NotSupportedException">The adapter does not implement <see cref="IFindInPageAdapter"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is null or empty.</exception>
    public Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Search text must not be null or empty.", nameof(text));
        if (_findInPageAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support find-in-page.");
        return _findInPageAdapter.FindAsync(text, options);
    }

    /// <summary>
    /// Clears find-in-page highlights and resets search state.
    /// </summary>
    /// <param name="clearHighlights">Whether to remove visual highlights. Default: true.</param>
    /// <exception cref="NotSupportedException">The adapter does not implement <see cref="IFindInPageAdapter"/>.</exception>
    public void StopFindInPage(bool clearHighlights = true)
    {
        ThrowIfDisposed();
        if (_findInPageAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support find-in-page.");
        _findInPageAdapter.StopFind(clearHighlights);
    }

    /// <summary>
    /// Registers a JavaScript snippet to run at document start on every page load.
    /// </summary>
    /// <param name="javaScript">The script to inject.</param>
    /// <returns>An opaque script ID that can be passed to <see cref="RemovePreloadScript"/>.</returns>
    /// <exception cref="NotSupportedException">The adapter does not implement <see cref="IPreloadScriptAdapter"/>.</exception>
    public string AddPreloadScript(string javaScript)
    {
        ThrowIfDisposed();
        if (_preloadScriptAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support preload scripts.");
        return _preloadScriptAdapter.AddPreloadScript(javaScript);
    }

    /// <summary>
    /// Removes a previously registered preload script by its ID.
    /// </summary>
    /// <param name="scriptId">The ID returned by <see cref="AddPreloadScript"/>.</param>
    /// <exception cref="NotSupportedException">The adapter does not implement <see cref="IPreloadScriptAdapter"/>.</exception>
    public void RemovePreloadScript(string scriptId)
    {
        ThrowIfDisposed();
        if (_preloadScriptAdapter is null)
            throw new NotSupportedException("The current WebView adapter does not support preload scripts.");
        _preloadScriptAdapter.RemovePreloadScript(scriptId);
    }

    /// <summary>
    /// Delegates to the adapter's <see cref="INativeWebViewHandleProvider.TryGetWebViewHandle()"/>
    /// if the adapter supports it; otherwise returns <c>null</c>.
    /// Returns <c>null</c> after <see cref="AdapterDestroyed"/> has been raised.
    /// </summary>
    public global::Avalonia.Platform.IPlatformHandle? TryGetWebViewHandle()
    {
        if (_adapterDestroyed)
        {
            return null;
        }

        if (_adapter is not INativeWebViewHandleProvider provider)
        {
            return null;
        }

        return _dispatcher.CheckAccess()
            ? provider.TryGetWebViewHandle()
            : _dispatcher.InvokeAsync(() => provider.TryGetWebViewHandle()).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sets the custom User-Agent string at runtime.
    /// Pass <c>null</c> to revert to the platform default.
    /// </summary>
    public void SetCustomUserAgent(string? userAgent)
    {
        ThrowIfDisposed();
        if (_adapter is IWebViewAdapterOptions adapterOptions)
        {
            if (_dispatcher.CheckAccess())
            {
                adapterOptions.SetCustomUserAgent(userAgent);
            }
            else
            {
                _ = _dispatcher.InvokeAsync(() => adapterOptions.SetCustomUserAgent(userAgent));
            }

            _logger.LogDebug("CustomUserAgent set to: {UA}", userAgent ?? "(default)");
        }
    }

    public void EnableWebMessageBridge(WebMessageBridgeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(EnableWebMessageBridge));

        _webMessageBridgeEnabled = true;
        _webMessagePolicy = new DefaultWebMessagePolicy(options.AllowedOrigins, options.ProtocolVersion, ChannelId);
        _webMessageDropDiagnosticsSink = options.DropDiagnosticsSink;
        _rpcService ??= new WebViewRpcService(script => InvokeScriptAsync(script), _logger);

        // Inject RPC JS stub.
        _ = InvokeScriptAsync(WebViewRpcService.JsStub);

        _logger.LogDebug("WebMessageBridge enabled: originCount={Count}, protocol={Protocol}",
            options.AllowedOrigins?.Count ?? 0, options.ProtocolVersion);
    }

    public void DisableWebMessageBridge()
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread(nameof(DisableWebMessageBridge));

        _webMessageBridgeEnabled = false;
        _webMessagePolicy = null;
        _webMessageDropDiagnosticsSink = null;
        _rpcService = null;

        _logger.LogDebug("WebMessageBridge disabled");
    }

    // ==================== SPA Hosting ====================

    /// <summary>
    /// Enables SPA hosting. Registers the custom scheme, subscribes to WebResourceRequested,
    /// and optionally auto-enables the bridge.
    /// </summary>
    public void EnableSpaHosting(SpaHostingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        if (_spaHostingService is not null)
            throw new InvalidOperationException("SPA hosting is already enabled.");

        _spaHostingService = new SpaHostingService(options, _logger);

        // Register custom scheme with the adapter.
        if (_adapter is ICustomSchemeAdapter customSchemeAdapter)
        {
            customSchemeAdapter.RegisterCustomSchemes([_spaHostingService.GetSchemeRegistration()]);
        }

        // Subscribe to WebResourceRequested to intercept app:// requests.
        WebResourceRequested += OnSpaWebResourceRequested;

        // Auto-enable bridge if requested.
        if (options.AutoInjectBridgeScript && !_webMessageBridgeEnabled)
        {
            EnableWebMessageBridge(new WebMessageBridgeOptions());
        }

        _logger.LogDebug("SPA hosting enabled: scheme={Scheme}, devServer={DevServer}",
            options.Scheme, options.DevServerUrl ?? "(embedded)");
    }

    private void OnSpaWebResourceRequested(object? sender, WebResourceRequestedEventArgs e)
    {
        _spaHostingService?.TryHandle(e);
    }

    private Task InvokeAsyncOnUiThread(Func<Task> func)
    {
        if (_disposed)
        {
            return Task.FromException(new ObjectDisposedException(nameof(WebViewCore)));
        }

        return _dispatcher.CheckAccess()
            ? func()
            : _dispatcher.InvokeAsync(func);
    }

    private Task StartNavigationCoreAsync(Uri requestUri, Func<Guid, Task> adapterInvoke)
        => StartNavigationCoreAsync(requestUri, adapterInvoke, updateSource: true);

    private async Task StartNavigationCoreAsync(Uri requestUri, Func<Guid, Task> adapterInvoke, bool updateSource)
    {
        ThrowIfDisposed();
        ThrowIfNotOnUiThread("async navigation");

        if (updateSource)
        {
            SetSourceInternal(requestUri.AbsoluteUri != AboutBlank.AbsoluteUri ? requestUri : AboutBlank);
        }

        if (_activeNavigation is not null)
        {
            _logger.LogDebug("StartNavigation: superseding active navigation id={NavigationId}", _activeNavigation.NavigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Superseded, error: null);
        }

        var navigationId = Guid.NewGuid();
        var operation = new NavigationOperation(navigationId, correlationId: navigationId, requestUri);
        _activeNavigation = operation;

        var startingArgs = new NavigationStartingEventArgs(navigationId, requestUri);
        _logger.LogDebug("Event NavigationStarted (API): id={NavigationId}, uri={Uri}", navigationId, requestUri);
        NavigationStarted?.Invoke(this, startingArgs);

        if (startingArgs.Cancel)
        {
            _logger.LogDebug("StartNavigation: canceled by handler, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            await operation.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await adapterInvoke(navigationId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "StartNavigation: adapter invocation failed, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Failure, ex);
            await operation.Task.ConfigureAwait(false);
            return;
        }

        await operation.Task.ConfigureAwait(false);
    }

    private Guid StartCommandNavigation(Uri requestUri)
    {
        if (_activeNavigation is not null)
        {
            _logger.LogDebug("StartCommandNavigation: superseding active navigation id={NavigationId}", _activeNavigation.NavigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Superseded, error: null);
        }

        var navigationId = Guid.NewGuid();
        _activeNavigation = new NavigationOperation(navigationId, correlationId: navigationId, requestUri);

        var args = new NavigationStartingEventArgs(navigationId, requestUri);
        _logger.LogDebug("Event NavigationStarted (command): id={NavigationId}, uri={Uri}", navigationId, requestUri);
        NavigationStarted?.Invoke(this, args);

        if (args.Cancel)
        {
            _logger.LogDebug("StartCommandNavigation: canceled by handler, id={NavigationId}", navigationId);
            CompleteActiveNavigation(NavigationCompletedStatus.Canceled, error: null);
            return Guid.Empty;
        }

        return navigationId;
    }

    private void OnAdapterNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
    {
        _logger.LogDebug("Adapter.NavigationCompleted received: id={NavigationId}, status={Status}, uri={Uri}",
            e.NavigationId, e.Status, e.RequestUri);

        if (_disposed || _adapterDestroyed)
        {
            _logger.LogDebug("Adapter.NavigationCompleted: ignored (disposed or destroyed)");
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            OnAdapterNavigationCompletedOnUiThread(e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => OnAdapterNavigationCompletedOnUiThread(e));
    }

    private void OnAdapterNavigationCompletedOnUiThread(NavigationCompletedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (_activeNavigation is null)
        {
            _logger.LogDebug("Adapter.NavigationCompleted: no active navigation, ignoring id={NavigationId}", e.NavigationId);
            return;
        }

        if (e.NavigationId != _activeNavigation.NavigationId)
        {
            _logger.LogDebug("Adapter.NavigationCompleted: id mismatch (received={Received}, active={Active}), ignoring",
                e.NavigationId, _activeNavigation.NavigationId);
            // Late or unrelated completion; ignore to preserve exactly-once per active NavigationId.
            return;
        }

        var status = e.Status;
        var error = e.Error;

        if (status == NavigationCompletedStatus.Failure && error is null)
        {
            error = new Exception("Navigation failed.");
        }

        _activeNavigation.UpdateRequestUri(e.RequestUri);
        CompleteActiveNavigation(status, error);
    }

    private void OnAdapterNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        _logger.LogDebug("Event NewWindowRequested: uri={Uri}", e.Uri);

        if (_disposed || _adapterDestroyed)
        {
            _logger.LogDebug("NewWindowRequested: ignored (disposed or destroyed)");
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            HandleNewWindowRequestedOnUiThread(e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => HandleNewWindowRequestedOnUiThread(e));
    }

    private void HandleNewWindowRequestedOnUiThread(NewWindowRequestedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        NewWindowRequested?.Invoke(this, e);

        if (!e.Handled && e.Uri is not null)
        {
            _logger.LogDebug("NewWindowRequested: unhandled, navigating in-view to {Uri}", e.Uri);
            _ = NavigateAsync(e.Uri);
        }
    }

    private void OnAdapterWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        _logger.LogDebug("Event WebMessageReceived: origin={Origin}, channelId={ChannelId}", e.Origin, e.ChannelId);

        if (_disposed || _adapterDestroyed)
        {
            _logger.LogDebug("WebMessageReceived: ignored (disposed or destroyed)");
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            OnAdapterWebMessageReceivedOnUiThread(e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => OnAdapterWebMessageReceivedOnUiThread(e));
    }

    private void OnAdapterWebMessageReceivedOnUiThread(WebMessageReceivedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (!_webMessageBridgeEnabled)
        {
            _logger.LogDebug("WebMessageReceived: bridge not enabled, dropping");
            return;
        }

        var policy = _webMessagePolicy;
        if (policy is null)
        {
            _logger.LogDebug("WebMessageReceived: no policy, dropping");
            return;
        }

        var envelope = new WebMessageEnvelope(
            Body: e.Body,
            Origin: e.Origin,
            ChannelId: e.ChannelId,
            ProtocolVersion: e.ProtocolVersion);

        var decision = policy.Evaluate(in envelope);
        if (decision.IsAllowed)
        {
            // Try RPC dispatch first.
            if (_rpcService is not null && _rpcService.TryProcessMessage(e.Body))
            {
                _logger.LogDebug("WebMessageReceived: handled as RPC message");
                return;
            }

            _logger.LogDebug("WebMessageReceived: policy allowed, forwarding");
            WebMessageReceived?.Invoke(this, e);
            return;
        }

        var reason = decision.DropReason ?? WebMessageDropReason.OriginNotAllowed;
        _logger.LogDebug("WebMessageReceived: policy denied, reason={Reason}", reason);
        _webMessageDropDiagnosticsSink?.OnMessageDropped(new WebMessageDropDiagnostic(reason, e.Origin, e.ChannelId));
    }

    private void OnAdapterWebResourceRequested(object? sender, WebResourceRequestedEventArgs e)
    {
        _logger.LogDebug("Event WebResourceRequested");

        if (_disposed || _adapterDestroyed)
        {
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            WebResourceRequested?.Invoke(this, e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => WebResourceRequested?.Invoke(this, e));
    }

    private void OnAdapterEnvironmentRequested(object? sender, EnvironmentRequestedEventArgs e)
    {
        _logger.LogDebug("Event EnvironmentRequested");

        if (_disposed || _adapterDestroyed)
        {
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            EnvironmentRequested?.Invoke(this, e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => EnvironmentRequested?.Invoke(this, e));
    }

    private void OnAdapterDownloadRequested(object? sender, DownloadRequestedEventArgs e)
    {
        _logger.LogDebug("Event DownloadRequested: uri={Uri}, file={File}", e.DownloadUri, e.SuggestedFileName);

        if (_disposed || _adapterDestroyed) return;

        if (_dispatcher.CheckAccess())
        {
            DownloadRequested?.Invoke(this, e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => DownloadRequested?.Invoke(this, e));
    }

    private void OnAdapterPermissionRequested(object? sender, PermissionRequestedEventArgs e)
    {
        _logger.LogDebug("Event PermissionRequested: kind={Kind}, origin={Origin}", e.PermissionKind, e.Origin);

        if (_disposed || _adapterDestroyed) return;

        if (_dispatcher.CheckAccess())
        {
            PermissionRequested?.Invoke(this, e);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => PermissionRequested?.Invoke(this, e));
    }

    private void CompleteActiveNavigation(NavigationCompletedStatus status, Exception? error)
    {
        var operation = _activeNavigation;
        if (operation is null)
        {
            return;
        }

        _activeNavigation = null;

        _logger.LogDebug("Event NavigationCompleted: id={NavigationId}, status={Status}, uri={Uri}, error={Error}",
            operation.NavigationId, status, operation.RequestUri, error?.Message);

        NavigationCompletedEventArgs completedArgs;
        try
        {
            completedArgs = new NavigationCompletedEventArgs(
                operation.NavigationId,
                operation.RequestUri,
                status,
                status == NavigationCompletedStatus.Failure ? error : null);
        }
        catch (Exception ex)
        {
            completedArgs = new NavigationCompletedEventArgs(operation.NavigationId, operation.RequestUri, NavigationCompletedStatus.Failure, ex);
            status = NavigationCompletedStatus.Failure;
            error = ex;
        }

        NavigationCompleted?.Invoke(this, completedArgs);

        if (status == NavigationCompletedStatus.Failure)
        {
            // Preserve categorized exception subclasses from the adapter (Network, SSL, Timeout).
            var faultException = error is WebViewNavigationException navEx
                ? navEx
                : new WebViewNavigationException(
                    message: "Navigation failed.",
                    navigationId: operation.NavigationId,
                    requestUri: operation.RequestUri,
                    innerException: error);
            operation.TrySetFault(faultException);
        }
        else
        {
            operation.TrySetSuccess();
        }
    }

    private void RaiseAdapterDestroyedOnce()
    {
        if (_adapterDestroyed)
        {
            return;
        }

        _adapterDestroyed = true;
        _logger.LogDebug("AdapterDestroyed: raising");
        AdapterDestroyed?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfNotOnUiThread(string apiName)
    {
        if (!_dispatcher.CheckAccess())
        {
            throw new InvalidOperationException($"'{apiName}' must be called on the UI thread.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebViewCore));
        }
    }

    private void SetSourceInternal(Uri uri)
    {
        _source = uri;
    }

    /// <summary>
    /// Runtime wrapper around <see cref="ICookieAdapter"/> that adds lifecycle guards and dispatcher marshaling.
    /// </summary>
    private sealed class RuntimeCookieManager : ICookieManager
    {
        private readonly ICookieAdapter _cookieAdapter;
        private readonly WebViewCore _owner;
        private readonly IWebViewDispatcher _dispatcher;
        private readonly ILogger _logger;

        public RuntimeCookieManager(ICookieAdapter cookieAdapter, WebViewCore owner, IWebViewDispatcher dispatcher, ILogger logger)
        {
            _cookieAdapter = cookieAdapter;
            _owner = owner;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ThrowIfOwnerDisposed();
            _logger.LogDebug("CookieManager.GetCookiesAsync: {Uri}", uri);
            return _dispatcher.CheckAccess()
                ? _cookieAdapter.GetCookiesAsync(uri)
                : _dispatcher.InvokeAsync(() => _cookieAdapter.GetCookiesAsync(uri));
        }

        public Task SetCookieAsync(WebViewCookie cookie)
        {
            ArgumentNullException.ThrowIfNull(cookie);
            ThrowIfOwnerDisposed();
            _logger.LogDebug("CookieManager.SetCookieAsync: {Name}@{Domain}", cookie.Name, cookie.Domain);
            return _dispatcher.CheckAccess()
                ? _cookieAdapter.SetCookieAsync(cookie)
                : _dispatcher.InvokeAsync(() => _cookieAdapter.SetCookieAsync(cookie));
        }

        public Task DeleteCookieAsync(WebViewCookie cookie)
        {
            ArgumentNullException.ThrowIfNull(cookie);
            ThrowIfOwnerDisposed();
            _logger.LogDebug("CookieManager.DeleteCookieAsync: {Name}@{Domain}", cookie.Name, cookie.Domain);
            return _dispatcher.CheckAccess()
                ? _cookieAdapter.DeleteCookieAsync(cookie)
                : _dispatcher.InvokeAsync(() => _cookieAdapter.DeleteCookieAsync(cookie));
        }

        public Task ClearAllCookiesAsync()
        {
            ThrowIfOwnerDisposed();
            _logger.LogDebug("CookieManager.ClearAllCookiesAsync");
            return _dispatcher.CheckAccess()
                ? _cookieAdapter.ClearAllCookiesAsync()
                : _dispatcher.InvokeAsync(() => _cookieAdapter.ClearAllCookiesAsync());
        }

        private void ThrowIfOwnerDisposed()
        {
            if (_owner._disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewCore));
            }
        }
    }

    /// <summary>
    /// Runtime wrapper around <see cref="ICommandAdapter"/> that delegates editing commands.
    /// </summary>
    private sealed class RuntimeCommandManager : ICommandManager
    {
        private readonly ICommandAdapter _commandAdapter;

        public RuntimeCommandManager(ICommandAdapter commandAdapter)
        {
            _commandAdapter = commandAdapter;
        }

        public void Copy() => _commandAdapter.ExecuteCommand(WebViewCommand.Copy);
        public void Cut() => _commandAdapter.ExecuteCommand(WebViewCommand.Cut);
        public void Paste() => _commandAdapter.ExecuteCommand(WebViewCommand.Paste);
        public void SelectAll() => _commandAdapter.ExecuteCommand(WebViewCommand.SelectAll);
        public void Undo() => _commandAdapter.ExecuteCommand(WebViewCommand.Undo);
        public void Redo() => _commandAdapter.ExecuteCommand(WebViewCommand.Redo);
    }

    private sealed class NavigationOperation
    {
        private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public NavigationOperation(Guid navigationId, Guid correlationId, Uri requestUri)
        {
            NavigationId = navigationId;
            CorrelationId = correlationId;
            RequestUri = requestUri;
        }

        public Guid NavigationId { get; }
        public Guid CorrelationId { get; }
        public Uri RequestUri { get; private set; }

        public Task Task => _tcs.Task;

        public void UpdateRequestUri(Uri requestUri) => RequestUri = requestUri;

        public void TrySetSuccess() => _tcs.TrySetResult();

        public void TrySetFault(Exception ex) => _tcs.TrySetException(ex);
    }
}
