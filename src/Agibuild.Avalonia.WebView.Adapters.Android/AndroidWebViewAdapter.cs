using System.Runtime.Versioning;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Java.Interop;
using JavaObject = Java.Lang.Object;
using AWebView = Android.Webkit.WebView;

namespace Agibuild.Avalonia.WebView.Adapters.Android;

[SupportedOSPlatform("android")]
internal sealed class AndroidWebViewAdapter : IWebViewAdapter, INativeWebViewHandleProvider, ICookieAdapter, IWebViewAdapterOptions
{
    private static bool DiagnosticsEnabled
        => string.Equals(System.Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal);

    private IWebViewAdapterHost? _host;

    private bool _initialized;
    private bool _attached;
    private bool _detached;

    // Android WebView objects
    private AWebView? _webView;
    private AdapterWebViewClient? _webViewClient;
    private AdapterWebChromeClient? _webChromeClient;
    private AndroidJsBridge? _jsBridge;
    private Handler? _mainHandler;

    // Navigation state
    private readonly object _navLock = new();

    // API-initiated navigation tracking
    private Guid _pendingApiNavigationId;
    private bool _pendingApiNavigation;
    private readonly HashSet<string> _apiNavUrls = new(); // URLs from API calls currently in flight
    private readonly Dictionary<string, Guid> _apiNavIdByUrl = new(); // URL → API NavigationId

    // Native-initiated navigation tracking
    private Guid _currentCorrelationId;
    private Guid _currentNativeNavigationId;
    private bool _hasActiveNavigation;
    private string? _activeNavigationUrl;

    // Guard exactly-once completion per NavigationId
    private readonly HashSet<Guid> _completedNavIds = new();

    // Tracks whether current navigation had an error (set in OnReceivedError, consumed in OnPageFinished)
    private bool _navigationErrorOccurred;
    private Exception? _navigationError;
    private Guid _navigationErrorNavId;

    // Environment options (stored before Attach, applied after WebView creation)
    private IWebViewEnvironmentOptions? _pendingOptions;

    public bool CanGoBack => _webView?.CanGoBack() ?? false;
    public bool CanGoForward => _webView?.CanGoForward() ?? false;

    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested
    {
        add { }
        remove { }
    }

    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested
    {
        add { }
        remove { }
    }

    // ==================== Lifecycle ====================

    public void Initialize(IWebViewAdapterHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (_initialized)
        {
            throw new InvalidOperationException($"{nameof(Initialize)} can only be called once.");
        }

        _initialized = true;
        _host = host;
    }

    public void Attach(IPlatformHandle parentHandle)
    {
        ArgumentNullException.ThrowIfNull(parentHandle);
        ThrowIfNotInitialized();

        if (_detached)
        {
            throw new InvalidOperationException($"{nameof(Attach)} cannot be called after {nameof(Detach)}.");
        }

        if (_attached)
        {
            throw new InvalidOperationException($"{nameof(Attach)} can only be called once.");
        }

        if (!OperatingSystem.IsAndroid())
        {
            throw new PlatformNotSupportedException("Android WebView adapter can only be used on Android.");
        }

        _attached = true;
        _mainHandler = new Handler(Looper.MainLooper!);

        // Resolve the parent ViewGroup from the platform handle.
        // On Avalonia Android, the IPlatformHandle.Handle is a GCHandle pointer to the native View.
        var parentView = ResolveParentView(parentHandle);

        // Create WebView on the UI thread.
        PostOnUiThread(() => InitializeWebView(parentView));
    }

    private void InitializeWebView(ViewGroup parentView)
    {
        try
        {
            var context = parentView.Context
                          ?? global::Android.App.Application.Context;

            _webView = new AWebView(context);

            // Configure WebSettings
            var settings = _webView.Settings!;
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.AllowContentAccess = true;
            settings.AllowFileAccess = false;
            settings.MixedContentMode = MixedContentHandling.CompatibilityMode;
            settings.SetSupportMultipleWindows(true);

            // Apply pending options
            ApplyPendingOptions();

            // Create and attach WebViewClient
            _webViewClient = new AdapterWebViewClient(this);
            _webView.SetWebViewClient(_webViewClient);

            // Create and attach WebChromeClient
            _webChromeClient = new AdapterWebChromeClient(this);
            _webView.SetWebChromeClient(_webChromeClient);

            // Set up JavaScript bridge for WebMessage receive path
            var channelId = _host?.ChannelId ?? Guid.Empty;
            _jsBridge = new AndroidJsBridge(this, channelId);
            _webView.AddJavascriptInterface(_jsBridge, "__agibuildBridge");

            // Set layout params and add to parent
            _webView.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);
            parentView.AddView(_webView);

            if (DiagnosticsEnabled)
            {
                Console.WriteLine("[Agibuild.WebView] Android WebView initialized successfully.");
            }
        }
        catch (Exception ex)
        {
            if (DiagnosticsEnabled)
            {
                Console.WriteLine($"[Agibuild.WebView] Android WebView initialization failed: {ex.Message}");
            }
        }
    }

    public void Detach()
    {
        ThrowIfNotInitialized();

        if (_detached)
        {
            return;
        }

        _detached = true;
        _attached = false;

        RunOnUiThread(() =>
        {
            try
            {
                if (_webView is not null)
                {
                    _webView.StopLoading();
                    _webView.SetWebViewClient(null!);
                    _webView.SetWebChromeClient(null!);

                    if (_jsBridge is not null)
                    {
                        _webView.RemoveJavascriptInterface("__agibuildBridge");
                    }

                    // Remove from parent
                    if (_webView.Parent is ViewGroup parent)
                    {
                        parent.RemoveView(_webView);
                    }

                    _webView.Destroy();
                }
            }
            finally
            {
                _webView = null;
                _webViewClient = null;
                _webChromeClient = null;
                _jsBridge = null;

                lock (_navLock)
                {
                    _apiNavUrls.Clear();
                    _apiNavIdByUrl.Clear();
                    _completedNavIds.Clear();
                    _pendingApiNavigation = false;
                    _hasActiveNavigation = false;
                }
            }
        });
    }

    private void ApplyPendingOptions()
    {
        if (_webView is null || _pendingOptions is null) return;

        if (_pendingOptions.EnableDevTools)
        {
            AWebView.SetWebContentsDebuggingEnabled(true);
        }

        if (_pendingOptions.CustomUserAgent is not null)
        {
            _webView.Settings!.UserAgentString = _pendingOptions.CustomUserAgent;
        }
    }

    // ==================== Navigation — API-initiated ====================

    public Task NavigateAsync(Guid navigationId, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ThrowIfNotAttached();

        lock (_navLock) { BeginApiNavigation(navigationId, uri.AbsoluteUri); }

        RunOnUiThread(() => _webView?.LoadUrl(uri.AbsoluteUri));
        return Task.CompletedTask;
    }

    public Task NavigateToStringAsync(Guid navigationId, string html)
        => NavigateToStringAsync(navigationId, html, baseUrl: null);

    public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(html);
        ThrowIfNotAttached();

        if (baseUrl is null)
        {
            lock (_navLock) { BeginApiNavigation(navigationId, "data:text/html"); }
            RunOnUiThread(() => _webView?.LoadData(html, "text/html", "UTF-8"));
        }
        else
        {
            lock (_navLock) { BeginApiNavigation(navigationId, baseUrl.AbsoluteUri); }
            RunOnUiThread(() => _webView?.LoadDataWithBaseURL(
                baseUrl.AbsoluteUri, html, "text/html", "UTF-8", null));
        }

        return Task.CompletedTask;
    }

    // ==================== Navigation — commands ====================

    public bool GoBack(Guid navigationId)
    {
        ThrowIfNotAttached();
        if (_webView is null || !_webView.CanGoBack()) return false;
        lock (_navLock) { BeginApiNavigation(navigationId, null); }
        RunOnUiThread(() => _webView?.GoBack());
        return true;
    }

    public bool GoForward(Guid navigationId)
    {
        ThrowIfNotAttached();
        if (_webView is null || !_webView.CanGoForward()) return false;
        lock (_navLock) { BeginApiNavigation(navigationId, null); }
        RunOnUiThread(() => _webView?.GoForward());
        return true;
    }

    public bool Refresh(Guid navigationId)
    {
        ThrowIfNotAttached();
        if (_webView is null) return false;
        lock (_navLock) { BeginApiNavigation(navigationId, null); }
        RunOnUiThread(() => _webView?.Reload());
        return true;
    }

    public bool Stop()
    {
        ThrowIfNotAttached();
        RunOnUiThread(() => _webView?.StopLoading());
        return true;
    }

    // ==================== Script execution ====================

    public Task<string?> InvokeScriptAsync(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        ThrowIfNotAttached();

        if (_webView is null)
        {
            throw new InvalidOperationException("WebView is not available.");
        }

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        RunOnUiThread(() =>
        {
            _webView.EvaluateJavascript(script, new ScriptResultCallback(tcs));
        });

        return tcs.Task;
    }

    // ==================== ICookieAdapter ====================

    public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
    {
        ThrowIfNotAttachedForCookies();

        var cookieManager = CookieManager.Instance;
        if (cookieManager is null)
        {
            return Task.FromResult<IReadOnlyList<WebViewCookie>>(Array.Empty<WebViewCookie>());
        }

        var cookieString = cookieManager.GetCookie(uri.AbsoluteUri);
        var result = ParseCookieString(cookieString, uri);
        return Task.FromResult<IReadOnlyList<WebViewCookie>>(result);
    }

    public Task SetCookieAsync(WebViewCookie cookie)
    {
        ThrowIfNotAttachedForCookies();

        var cookieManager = CookieManager.Instance;
        if (cookieManager is null)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cookieString = FormatCookieString(cookie);
        var url = $"https://{cookie.Domain}{cookie.Path}";

        cookieManager.SetCookie(url, cookieString, new CookieValueCallback(tcs));
        cookieManager.Flush();
        return tcs.Task;
    }

    public Task DeleteCookieAsync(WebViewCookie cookie)
    {
        ThrowIfNotAttachedForCookies();

        var cookieManager = CookieManager.Instance;
        if (cookieManager is null)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Android has no direct delete API — set with expired date.
        var expiredCookie = $"{cookie.Name}=; Domain={cookie.Domain}; Path={cookie.Path}; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
        var url = $"https://{cookie.Domain}{cookie.Path}";

        cookieManager.SetCookie(url, expiredCookie, new CookieValueCallback(tcs));
        cookieManager.Flush();
        return tcs.Task;
    }

    public Task ClearAllCookiesAsync()
    {
        ThrowIfNotAttachedForCookies();

        var cookieManager = CookieManager.Instance;
        if (cookieManager is null)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cookieManager.RemoveAllCookies(new CookieValueCallback(tcs));
        cookieManager.Flush();
        return tcs.Task;
    }

    // ==================== INativeWebViewHandleProvider ====================

    public IPlatformHandle? TryGetWebViewHandle()
    {
        if (!_attached || _detached || _webView is null) return null;

        // Return a PlatformHandle wrapping the Android WebView's native handle.
        return new PlatformHandle(_webView.Handle, "AndroidWebView");
    }

    // ==================== IWebViewAdapterOptions ====================

    public void ApplyEnvironmentOptions(IWebViewEnvironmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfNotInitialized();

        if (_webView is not null)
        {
            RunOnUiThread(() =>
            {
            if (options.EnableDevTools)
            {
                AWebView.SetWebContentsDebuggingEnabled(true);
            }

                if (options.CustomUserAgent is not null)
                {
                    _webView.Settings!.UserAgentString = options.CustomUserAgent;
                }
            });
        }
        else
        {
            _pendingOptions = options;
        }
    }

    public void SetCustomUserAgent(string? userAgent)
    {
        ThrowIfNotInitialized();
        if (_webView is not null)
        {
            RunOnUiThread(() => _webView.Settings!.UserAgentString = userAgent ?? string.Empty);
        }
    }

    // ==================== WebViewClient callbacks ====================

    internal void OnShouldOverrideUrlLoading(AWebView? view, IWebResourceRequest? request)
    {
        // This is called from AdapterWebViewClient.ShouldOverrideUrlLoading
        // and only for navigations we need to intercept.
    }

    internal bool HandleShouldOverrideUrlLoading(AWebView? view, IWebResourceRequest? request)
    {
        if (_detached || request?.Url is null) return false;

        var url = request.Url.ToString() ?? string.Empty;

        lock (_navLock)
        {
            // Check if this is an API-initiated navigation.
            if (_apiNavUrls.Contains(url))
            {
                if (DiagnosticsEnabled)
                {
                    Console.WriteLine($"[Agibuild.WebView] ShouldOverrideUrlLoading (API): url={url}");
                }
                return false; // Allow API navigation to proceed.
            }

            // Also check if we have a pending API navigation without URL tracking
            // (e.g., GoBack/GoForward/Refresh).
            if (_pendingApiNavigation)
            {
                _pendingApiNavigation = false;
                _hasActiveNavigation = true;
                _activeNavigationUrl = url;
                _apiNavIdByUrl[url] = _pendingApiNavigationId;
                _apiNavUrls.Add(url);

                if (DiagnosticsEnabled)
                {
                    Console.WriteLine($"[Agibuild.WebView] ShouldOverrideUrlLoading (API pending): url={url}, navId={_pendingApiNavigationId}");
                }
                return false;
            }
        }

        // Native-initiated navigation — consult the host.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var requestUri))
        {
            return false;
        }

        var host = _host;
        if (host is null)
        {
            return true; // Cancel if no host.
        }

        Guid correlationId;
        lock (_navLock)
        {
            // New native navigation chain.
            correlationId = Guid.NewGuid();
            _currentCorrelationId = correlationId;
        }

        var info = new NativeNavigationStartingInfo(correlationId, requestUri, IsMainFrame: true);
        var decisionTask = host.OnNativeNavigationStartingAsync(info);

        var decision = decisionTask.IsCompleted
            ? decisionTask.Result
            : decisionTask.AsTask().GetAwaiter().GetResult();

        if (DiagnosticsEnabled)
        {
            Console.WriteLine($"[Agibuild.WebView] ShouldOverrideUrlLoading (native): url={url}, allowed={decision.IsAllowed}, navId={decision.NavigationId}");
        }

        if (!decision.IsAllowed)
        {
            if (decision.NavigationId != Guid.Empty)
            {
                RaiseNavigationCompleted(decision.NavigationId, requestUri, NavigationCompletedStatus.Canceled, error: null);
            }
            return true; // Cancel the navigation.
        }

        if (decision.NavigationId != Guid.Empty)
        {
            lock (_navLock)
            {
                _currentNativeNavigationId = decision.NavigationId;
                _hasActiveNavigation = true;
                _activeNavigationUrl = url;
            }
        }

        return false; // Allow the navigation.
    }

    internal void OnPageStarted(AWebView? view, string? url)
    {
        if (_detached || url is null) return;

        lock (_navLock)
        {
            // Inject WebMessage bridge script on each page load.
            InjectBridgeScript(view);

            if (!_hasActiveNavigation) return;

            // URL change during active navigation indicates a redirect.
            if (_activeNavigationUrl is not null && !string.Equals(_activeNavigationUrl, url, StringComparison.Ordinal))
            {
                // Redirect detected — update tracked URL but keep same correlation/navigation IDs.
                _activeNavigationUrl = url;

                if (DiagnosticsEnabled)
                {
                    Console.WriteLine($"[Agibuild.WebView] OnPageStarted (redirect): url={url}, correlationId={_currentCorrelationId}");
                }
            }
        }
    }

    internal void OnPageFinished(AWebView? view, string? url)
    {
        if (_detached) return;

        Guid navigationId;
        Uri requestUri;

        lock (_navLock)
        {
            if (!_hasActiveNavigation) return;

            // Determine NavigationId — either from API or native tracking.
            if (url is not null && _apiNavIdByUrl.TryGetValue(url, out var apiNavId))
            {
                navigationId = apiNavId;
                _apiNavIdByUrl.Remove(url);
                _apiNavUrls.Remove(url);
            }
            else if (_currentNativeNavigationId != Guid.Empty)
            {
                navigationId = _currentNativeNavigationId;
            }
            else if (_pendingApiNavigation)
            {
                navigationId = _pendingApiNavigationId;
                _pendingApiNavigation = false;
            }
            else
            {
                // Check pending API nav ID by looking through tracked URLs
                var found = false;
                navigationId = Guid.Empty;
                foreach (var kvp in _apiNavIdByUrl)
                {
                    navigationId = kvp.Value;
                    _apiNavUrls.Remove(kvp.Key);
                    found = true;
                    break;
                }
                if (found)
                {
                    _apiNavIdByUrl.Remove(_apiNavIdByUrl.Keys.First());
                }
                else
                {
                    // Untracked navigation — ignore.
                    _hasActiveNavigation = false;
                    return;
                }
            }

            // Exactly-once guard.
            if (!_completedNavIds.Add(navigationId))
            {
                return;
            }

            // Clean up.
            _hasActiveNavigation = false;
            _currentNativeNavigationId = Guid.Empty;
            _activeNavigationUrl = null;
        }

        requestUri = Uri.TryCreate(url, UriKind.Absolute, out var parsed) ? parsed : new Uri("about:blank");

        // Check if an error was reported for this navigation.
        if (_navigationErrorOccurred && _navigationErrorNavId == navigationId)
        {
            _navigationErrorOccurred = false;
            var error = _navigationError;
            _navigationError = null;
            RaiseNavigationCompleted(navigationId, requestUri, NavigationCompletedStatus.Failure, error);
        }
        else
        {
            _navigationErrorOccurred = false;
            _navigationError = null;
            RaiseNavigationCompleted(navigationId, requestUri, NavigationCompletedStatus.Success, error: null);
        }
    }

    internal void OnReceivedError(AWebView? view, IWebResourceRequest? request, WebResourceError? error)
    {
        if (_detached || request is null || error is null) return;

        // Only handle main frame errors.
        if (!request.IsForMainFrame) return;

        var url = request.Url?.ToString() ?? string.Empty;
        var errorCode = (ClientError)error.ErrorCode;

        Guid navigationId;
        lock (_navLock)
        {
            // Determine the NavigationId for this error.
            if (url.Length > 0 && _apiNavIdByUrl.TryGetValue(url, out var apiNavId))
            {
                navigationId = apiNavId;
            }
            else if (_currentNativeNavigationId != Guid.Empty)
            {
                navigationId = _currentNativeNavigationId;
            }
            else if (_pendingApiNavigation)
            {
                navigationId = _pendingApiNavigationId;
            }
            else
            {
                // Try any tracked API nav
                navigationId = _apiNavIdByUrl.Values.FirstOrDefault();
                if (navigationId == Guid.Empty) return;
            }
        }

        var requestUri = Uri.TryCreate(url, UriKind.Absolute, out var parsed) ? parsed : new Uri("about:blank");
        var errorMessage = $"Navigation failed: {errorCode} - {error.Description}";
        var exception = MapErrorCode(errorCode, errorMessage, navigationId, requestUri);

        // Store the error — it will be consumed in OnPageFinished (Android calls both).
        _navigationErrorOccurred = true;
        _navigationError = exception;
        _navigationErrorNavId = navigationId;
    }

    internal void OnNewWindowRequested(AWebView? view, string? url)
    {
        if (_detached) return;

        Uri? targetUri = null;
        if (!string.IsNullOrWhiteSpace(url))
        {
            Uri.TryCreate(url, UriKind.Absolute, out targetUri);
        }

        var args = new NewWindowRequestedEventArgs(targetUri);
        SafeRaise(() => NewWindowRequested?.Invoke(this, args));
    }

    internal void OnWebMessageReceived(string body, string origin)
    {
        if (_detached) return;

        var channelId = _host?.ChannelId ?? Guid.Empty;
        SafeRaise(() => WebMessageReceived?.Invoke(this,
            new WebMessageReceivedEventArgs(body, origin, channelId, protocolVersion: 1)));
    }

    // ==================== Private helpers ====================

    private void BeginApiNavigation(Guid navigationId, string? url)
    {
        _pendingApiNavigation = true;
        _pendingApiNavigationId = navigationId;
        _hasActiveNavigation = true;
        _currentNativeNavigationId = Guid.Empty;

        if (url is not null)
        {
            _apiNavUrls.Add(url);
            _apiNavIdByUrl[url] = navigationId;
            _activeNavigationUrl = url;
        }
    }

    private void InjectBridgeScript(AWebView? view)
    {
        if (view is null) return;

        var channelId = _host?.ChannelId ?? Guid.Empty;
        var bridgeScript = $$"""
            (function() {
                window.__agibuildWebView = window.__agibuildWebView || {};
                window.__agibuildWebView.channelId = '{{channelId}}';
                window.__agibuildWebView.postMessage = function(body) {
                    if (window.__agibuildBridge) {
                        window.__agibuildBridge.postMessage(JSON.stringify({
                            channelId: '{{channelId}}',
                            protocolVersion: 1,
                            body: body
                        }));
                    }
                };
                if (!window.chrome) window.chrome = {};
                if (!window.chrome.webview) window.chrome.webview = {};
                window.chrome.webview.postMessage = window.__agibuildWebView.postMessage;
            })();
            """;

        view.EvaluateJavascript(bridgeScript, null);
    }

    private static Exception MapErrorCode(ClientError errorCode, string message, Guid navigationId, Uri requestUri)
    {
        return errorCode switch
        {
            // Timeout
            ClientError.Timeout
                => new WebViewTimeoutException(message, navigationId, requestUri),

            // Network
            ClientError.HostLookup or
            ClientError.Connect or
            ClientError.Io or
            ClientError.Unknown
                => new WebViewNetworkException(message, navigationId, requestUri),

            // SSL/TLS
            ClientError.FailedSslHandshake or
            ClientError.Authentication
                => new WebViewSslException(message, navigationId, requestUri),

            // All others
            _ => new WebViewNavigationException(message, navigationId, requestUri),
        };
    }

    private static ViewGroup ResolveParentView(IPlatformHandle parentHandle)
    {
        if (parentHandle.Handle == IntPtr.Zero)
        {
            throw new ArgumentException("Parent handle must be non-zero.", nameof(parentHandle));
        }

        // On Avalonia Android, the handle is a JNI reference to the Android View.
        var parentObj = JavaObject.GetObject<View>(parentHandle.Handle, JniHandleOwnership.DoNotTransfer);
        if (parentObj is ViewGroup viewGroup)
        {
            return viewGroup;
        }

        throw new ArgumentException(
            $"Parent handle must resolve to an Android ViewGroup, got: {parentObj?.GetType().Name ?? "null"}",
            nameof(parentHandle));
    }

    private static List<WebViewCookie> ParseCookieString(string? cookieString, Uri uri)
    {
        var result = new List<WebViewCookie>();
        if (string.IsNullOrWhiteSpace(cookieString)) return result;

        // Android CookieManager.GetCookie returns "name=value; name2=value2" format.
        var pairs = cookieString.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex <= 0) continue;

            var name = pair[..eqIndex].Trim();
            var value = pair[(eqIndex + 1)..].Trim();

            result.Add(new WebViewCookie(
                name, value,
                uri.Host,
                uri.AbsolutePath.Length > 0 ? "/" : uri.AbsolutePath,
                Expires: null,
                IsSecure: false,
                IsHttpOnly: false));
        }

        return result;
    }

    private static string FormatCookieString(WebViewCookie cookie)
    {
        var parts = new List<string>
        {
            $"{cookie.Name}={cookie.Value}",
            $"Domain={cookie.Domain}",
            $"Path={cookie.Path}"
        };

        if (cookie.Expires.HasValue)
        {
            parts.Add($"Expires={cookie.Expires.Value.UtcDateTime:R}");
        }

        if (cookie.IsSecure)
        {
            parts.Add("Secure");
        }

        if (cookie.IsHttpOnly)
        {
            parts.Add("HttpOnly");
        }

        return string.Join("; ", parts);
    }

    private void PostOnUiThread(Action action)
    {
        if (_mainHandler is not null)
        {
            _mainHandler.Post(action);
        }
        else
        {
            action();
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (Looper.MainLooper == Looper.MyLooper())
        {
            action();
        }
        else if (_mainHandler is not null)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _mainHandler.Post(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            tcs.Task.GetAwaiter().GetResult();
        }
        else
        {
            action();
        }
    }

    private void RaiseNavigationCompleted(Guid navigationId, Uri requestUri, NavigationCompletedStatus status, Exception? error)
        => SafeRaise(() => NavigationCompleted?.Invoke(this, new NavigationCompletedEventArgs(navigationId, requestUri, status, error)));

    private void SafeRaise(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // Keep platform callbacks safe.
        }
    }

    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Adapter must be initialized before use.");
        }
    }

    private void ThrowIfNotAttached()
    {
        ThrowIfNotInitialized();

        if (_detached)
        {
            throw new ObjectDisposedException(nameof(AndroidWebViewAdapter));
        }

        if (!_attached)
        {
            throw new InvalidOperationException("Adapter must be attached before use.");
        }
    }

    private void ThrowIfNotAttachedForCookies()
    {
        if (_detached)
        {
            throw new ObjectDisposedException(nameof(AndroidWebViewAdapter));
        }

        if (!_attached)
        {
            throw new InvalidOperationException("Adapter is not attached.");
        }
    }

    // ==================== Inner classes ====================

    /// <summary>
    /// Custom WebViewClient that delegates navigation events to the adapter.
    /// </summary>
    private sealed class AdapterWebViewClient : WebViewClient
    {
        private readonly AndroidWebViewAdapter _adapter;

        public AdapterWebViewClient(AndroidWebViewAdapter adapter)
        {
            _adapter = adapter;
        }

        public override bool ShouldOverrideUrlLoading(AWebView? view, IWebResourceRequest? request)
            => _adapter.HandleShouldOverrideUrlLoading(view, request);

        public override void OnPageStarted(AWebView? view, string? url, global::Android.Graphics.Bitmap? favicon)
        {
            base.OnPageStarted(view, url, favicon);
            _adapter.OnPageStarted(view, url);
        }

        public override void OnPageFinished(AWebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            _adapter.OnPageFinished(view, url);
        }

        public override void OnReceivedError(AWebView? view, IWebResourceRequest? request, WebResourceError? error)
        {
            base.OnReceivedError(view, request, error);
            _adapter.OnReceivedError(view, request, error);
        }
    }

    /// <summary>
    /// Custom WebChromeClient that handles new window requests.
    /// </summary>
    private sealed class AdapterWebChromeClient : WebChromeClient
    {
        private readonly AndroidWebViewAdapter _adapter;

        public AdapterWebChromeClient(AndroidWebViewAdapter adapter)
        {
            _adapter = adapter;
        }

        public override bool OnCreateWindow(AWebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        {
            // Try to extract the URL from the hit test result.
            var url = view?.GetHitTestResult()?.Extra;

            _adapter.OnNewWindowRequested(view, url);

            // Return false to indicate we're not providing a new WebView.
            return false;
        }
    }

    /// <summary>
    /// JavaScript interface bridge for receiving WebMessages from page scripts.
    /// </summary>
    private sealed class AndroidJsBridge : JavaObject
    {
        private readonly AndroidWebViewAdapter _adapter;
        private readonly Guid _channelId;

        public AndroidJsBridge(AndroidWebViewAdapter adapter, Guid channelId)
        {
            _adapter = adapter;
            _channelId = channelId;
        }

        [global::Android.Webkit.JavascriptInterface]
        [Export("postMessage")]
        public void PostMessage(string message)
        {
            // The message is a JSON envelope from our bridge script.
            _adapter.OnWebMessageReceived(message, string.Empty);
        }
    }

    /// <summary>
    /// IValueCallback implementation for script execution results.
    /// </summary>
    private sealed class ScriptResultCallback : JavaObject, IValueCallback
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public ScriptResultCallback(TaskCompletionSource<string?> tcs)
        {
            _tcs = tcs;
        }

        public void OnReceiveValue(JavaObject? value)
        {
            var result = value?.ToString();
            _tcs.TrySetResult(ScriptResultHelper.NormalizeJsonResult(result));
        }
    }

    /// <summary>
    /// IValueCallback for cookie operations (set/remove).
    /// </summary>
    private sealed class CookieValueCallback : JavaObject, IValueCallback
    {
        private readonly TaskCompletionSource _tcs;

        public CookieValueCallback(TaskCompletionSource tcs)
        {
            _tcs = tcs;
        }

        public void OnReceiveValue(JavaObject? value)
        {
            _tcs.TrySetResult();
        }
    }
}
