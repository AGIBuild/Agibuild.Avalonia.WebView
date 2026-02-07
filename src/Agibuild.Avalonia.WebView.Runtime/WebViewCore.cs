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
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal void Detach()
    {
        _logger.LogDebug("Detach: begin");
        _adapter.Detach();
        _logger.LogDebug("Detach: completed");
    }

    // Volatile: checked off-UI-thread in adapter callbacks before dispatching.
    private volatile bool _disposed;

    // Only accessed on the UI thread (all paths go through _dispatcher).
    private NavigationOperation? _activeNavigation;
    private Uri _source;

    private readonly ICookieManager? _cookieManager;

    private bool _webMessageBridgeEnabled;
    private IWebMessagePolicy? _webMessagePolicy;
    private IWebMessageDropDiagnosticsSink? _webMessageDropDiagnosticsSink;

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Dispose: begin");
        _disposed = true;

        _adapter.NavigationCompleted -= OnAdapterNavigationCompleted;
        _adapter.NewWindowRequested -= OnAdapterNewWindowRequested;
        _adapter.WebMessageReceived -= OnAdapterWebMessageReceived;
        _adapter.WebResourceRequested -= OnAdapterWebResourceRequested;
        _adapter.EnvironmentRequested -= OnAdapterEnvironmentRequested;

        if (_activeNavigation is not null)
        {
            _logger.LogDebug("Dispose: faulting active navigation id={NavigationId}", _activeNavigation.NavigationId);
            // After disposal, async APIs must not hang. No events must be raised.
            _activeNavigation.TrySetFault(new ObjectDisposedException(nameof(WebViewCore)));
            _activeNavigation = null;
        }

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

    public ICommandManager? TryGetCommandManager() => null;

    /// <summary>
    /// Delegates to the adapter's <see cref="INativeWebViewHandleProvider.TryGetWebViewHandle()"/>
    /// if the adapter supports it; otherwise returns <c>null</c>.
    /// </summary>
    public global::Avalonia.Platform.IPlatformHandle? TryGetWebViewHandle()
    {
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

        _logger.LogDebug("WebMessageBridge disabled");
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

        if (_disposed)
        {
            _logger.LogDebug("Adapter.NavigationCompleted: ignored (disposed)");
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

        if (_disposed)
        {
            _logger.LogDebug("NewWindowRequested: ignored (disposed)");
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

        if (_disposed)
        {
            _logger.LogDebug("WebMessageReceived: ignored (disposed)");
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

        if (_disposed)
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

        if (_disposed)
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
