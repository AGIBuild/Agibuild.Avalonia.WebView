using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Adapters.MacOS;

[SupportedOSPlatform("macos")]
internal sealed class MacOSWebViewAdapter : IWebViewAdapter, INativeWebViewHandleProvider, ICookieAdapter, IWebViewAdapterOptions
{
    private static bool DiagnosticsEnabled
        => string.Equals(Environment.GetEnvironmentVariable("AGIBUILD_WEBVIEW_DIAG"), "1", StringComparison.Ordinal);

    private IWebViewAdapterHost? _host;

    private bool _initialized;
    private bool _attached;
    private bool _detached;

    // Native shim state
    private IntPtr _native;
    private GCHandle _selfHandle;
    private NativeMethods.AgWkCallbacks _callbacks;
    private NativeMethods.PolicyRequestCb? _policyCb;
    private NativeMethods.NavigationCompletedCb? _navCompletedCb;
    private NativeMethods.ScriptResultCb? _scriptResultCb;
    private NativeMethods.MessageCb? _messageCb;

    // Lock protects navigation state accessed from both native callbacks
    // (which may run on background threads after an await) and API methods (UI thread).
    private readonly object _navLock = new();

    // Navigation state — guarded by _navLock
    private Guid _activeNavigationId;
    private Uri? _activeRequestUri;
    private bool _activeNavigationCompleted;
    private bool _apiNavigationActive;

    // Native-initiated main-frame correlation state (redirect chain) — guarded by _navLock
    private bool _nativeCorrelationActive;
    private Guid _nativeCorrelationId;

    // Script completion
    private long _nextScriptRequestId;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<string?>> _scriptTcsById = new();

    public bool CanGoBack => _attached && !_detached && NativeMethods.CanGoBack(_native);
    public bool CanGoForward => _attached && !_detached && NativeMethods.CanGoForward(_native);

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

    public void Initialize(IWebViewAdapterHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (_initialized)
        {
            throw new InvalidOperationException($"{nameof(Initialize)} can only be called once.");
        }

        _initialized = true;
        _host = host;

        _selfHandle = GCHandle.Alloc(this);

        _policyCb = (userData, requestId, urlUtf8, isMainFrame, isNewWindow, navigationType) =>
        {
            var self = NativeMethods.FromUserData(userData);
            self?.OnPolicyRequest(requestId, NativeMethods.PtrToString(urlUtf8), isMainFrame != 0, isNewWindow != 0, navigationType);
        };

        _navCompletedCb = (userData, urlUtf8, status, errorCode, errorMessageUtf8) =>
        {
            var self = NativeMethods.FromUserData(userData);
            self?.OnNavigationCompletedNative(NativeMethods.PtrToString(urlUtf8), status, errorCode, NativeMethods.PtrToString(errorMessageUtf8));
        };

        _scriptResultCb = (userData, requestId, resultUtf8, errorMessageUtf8) =>
        {
            var self = NativeMethods.FromUserData(userData);
            self?.OnScriptResultNative(requestId, NativeMethods.PtrToStringNullable(resultUtf8), NativeMethods.PtrToStringNullable(errorMessageUtf8));
        };

        _messageCb = (userData, bodyUtf8, originUtf8) =>
        {
            var self = NativeMethods.FromUserData(userData);
            self?.OnMessageNative(NativeMethods.PtrToString(bodyUtf8), NativeMethods.PtrToString(originUtf8));
        };

        _callbacks = new NativeMethods.AgWkCallbacks
        {
            on_policy_request = Marshal.GetFunctionPointerForDelegate(_policyCb),
            on_navigation_completed = Marshal.GetFunctionPointerForDelegate(_navCompletedCb),
            on_script_result = Marshal.GetFunctionPointerForDelegate(_scriptResultCb),
            on_message = Marshal.GetFunctionPointerForDelegate(_messageCb),
        };

        _native = NativeMethods.Create(ref _callbacks, GCHandle.ToIntPtr(_selfHandle));
        if (_native == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create native WKWebView shim instance.");
        }
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

        if (parentHandle.Handle == IntPtr.Zero)
        {
            throw new ArgumentException("Parent handle must be non-zero.", nameof(parentHandle));
        }

        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("WKWebView adapter can only be used on macOS.");
        }

        if (!NativeMethods.Attach(_native, parentHandle.Handle))
        {
            throw new InvalidOperationException("Native WKWebView shim failed to attach. Ensure the parent handle is an NSView.");
        }

        _attached = true;
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

        try
        {
            if (_native != IntPtr.Zero)
            {
                NativeMethods.Detach(_native);
                NativeMethods.Destroy(_native);
            }
        }
        finally
        {
            _native = IntPtr.Zero;

            foreach (var kvp in _scriptTcsById)
            {
                kvp.Value.TrySetException(new ObjectDisposedException(nameof(MacOSWebViewAdapter)));
            }
            _scriptTcsById.Clear();

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            ClearNavigationState();
        }
    }

    public Task NavigateAsync(Guid navigationId, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ThrowIfNotAttached();

        lock (_navLock) { BeginApiNavigation(navigationId, requestUri: uri); }
        NativeMethods.Navigate(_native, uri.AbsoluteUri);
        return Task.CompletedTask;
    }

    public Task NavigateToStringAsync(Guid navigationId, string html)
        => NavigateToStringAsync(navigationId, html, baseUrl: null);

    public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(html);
        ThrowIfNotAttached();

        lock (_navLock) { BeginApiNavigation(navigationId, requestUri: baseUrl ?? new Uri("about:blank")); }
        NativeMethods.LoadHtml(_native, html, baseUrl: baseUrl?.AbsoluteUri);
        return Task.CompletedTask;
    }

    public Task<string?> InvokeScriptAsync(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        ThrowIfNotAttached();

        var requestId = (ulong)Interlocked.Increment(ref _nextScriptRequestId);
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _scriptTcsById.TryAdd(requestId, tcs);

        NativeMethods.EvalJs(_native, requestId, script);
        return tcs.Task;
    }

    public bool GoBack(Guid navigationId)
    {
        ThrowIfNotAttached();
        lock (_navLock) { BeginApiNavigation(navigationId, requestUri: _activeRequestUri ?? new Uri("about:blank")); }
        return NativeMethods.GoBack(_native);
    }

    public bool GoForward(Guid navigationId)
    {
        ThrowIfNotAttached();
        lock (_navLock) { BeginApiNavigation(navigationId, requestUri: _activeRequestUri ?? new Uri("about:blank")); }
        return NativeMethods.GoForward(_native);
    }

    public bool Refresh(Guid navigationId)
    {
        ThrowIfNotAttached();
        lock (_navLock) { BeginApiNavigation(navigationId, requestUri: _activeRequestUri ?? new Uri("about:blank")); }
        return NativeMethods.Reload(_native);
    }

    public bool Stop()
    {
        ThrowIfNotAttached();
        NativeMethods.Stop(_native);
        return true;
    }

    public IPlatformHandle? TryGetWebViewHandle()
    {
        if (!_attached || _detached) return null;

        var ptr = NativeMethods.GetWebViewHandle(_native);
        return ptr == IntPtr.Zero ? null : new PlatformHandle(ptr, "WKWebView");
    }

    // ---------- IWebViewAdapterOptions ----------

    public void ApplyEnvironmentOptions(IWebViewEnvironmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfNotInitialized();

        if (_attached)
        {
            throw new InvalidOperationException("Environment options must be applied before Attach.");
        }

        NativeMethods.SetEnableDevTools(_native, options.EnableDevTools);
        NativeMethods.SetEphemeral(_native, options.UseEphemeralSession);

        if (options.CustomUserAgent is not null)
        {
            NativeMethods.SetUserAgent(_native, options.CustomUserAgent);
        }
    }

    public void SetCustomUserAgent(string? userAgent)
    {
        ThrowIfNotInitialized();
        NativeMethods.SetUserAgent(_native, userAgent);
    }

    // ---------- ICookieAdapter ----------

    public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
    {
        ThrowIfNotAttachedForCookies();
        var tcs = new TaskCompletionSource<IReadOnlyList<WebViewCookie>>();
        var tcsHandle = GCHandle.Alloc(tcs);

        NativeMethods.CookiesGet(_native, uri.AbsoluteUri, static (context, jsonUtf8) =>
        {
            var h = GCHandle.FromIntPtr(context);
            var t = (TaskCompletionSource<IReadOnlyList<WebViewCookie>>)h.Target!;
            h.Free();

            try
            {
                var json = NativeMethods.PtrToString(jsonUtf8);
                var cookies = ParseCookiesJson(json);
                t.TrySetResult(cookies);
            }
            catch (Exception ex)
            {
                t.TrySetException(ex);
            }
        }, GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    public Task SetCookieAsync(WebViewCookie cookie)
    {
        ThrowIfNotAttachedForCookies();
        var tcs = new TaskCompletionSource();
        var tcsHandle = GCHandle.Alloc(tcs);
        var expiresUnix = cookie.Expires.HasValue ? cookie.Expires.Value.ToUnixTimeSeconds() : -1.0;

        NativeMethods.CookieSet(_native,
            cookie.Name, cookie.Value, cookie.Domain, cookie.Path,
            expiresUnix, cookie.IsSecure, cookie.IsHttpOnly,
            static (context, success, errorUtf8) =>
            {
                var h = GCHandle.FromIntPtr(context);
                var t = (TaskCompletionSource)h.Target!;
                h.Free();

                if (success)
                    t.TrySetResult();
                else
                    t.TrySetException(new InvalidOperationException(NativeMethods.PtrToString(errorUtf8)));
            }, GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    public Task DeleteCookieAsync(WebViewCookie cookie)
    {
        ThrowIfNotAttachedForCookies();
        var tcs = new TaskCompletionSource();
        var tcsHandle = GCHandle.Alloc(tcs);

        NativeMethods.CookieDelete(_native,
            cookie.Name, cookie.Domain, cookie.Path,
            static (context, success, errorUtf8) =>
            {
                var h = GCHandle.FromIntPtr(context);
                var t = (TaskCompletionSource)h.Target!;
                h.Free();

                if (success)
                    t.TrySetResult();
                else
                    t.TrySetException(new InvalidOperationException(NativeMethods.PtrToString(errorUtf8)));
            }, GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    public Task ClearAllCookiesAsync()
    {
        ThrowIfNotAttachedForCookies();
        var tcs = new TaskCompletionSource();
        var tcsHandle = GCHandle.Alloc(tcs);

        NativeMethods.CookiesClearAll(_native,
            static (context, success, errorUtf8) =>
            {
                var h = GCHandle.FromIntPtr(context);
                var t = (TaskCompletionSource)h.Target!;
                h.Free();

                if (success)
                    t.TrySetResult();
                else
                    t.TrySetException(new InvalidOperationException(NativeMethods.PtrToString(errorUtf8)));
            }, GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    private void ThrowIfNotAttachedForCookies()
    {
        if (_detached)
            throw new ObjectDisposedException(nameof(MacOSWebViewAdapter));
        if (!_attached)
            throw new InvalidOperationException("Adapter is not attached.");
    }

    /// <summary>
    /// Parses a JSON array of cookie objects produced by the native shim.
    /// Uses simple string parsing to avoid a System.Text.Json dependency.
    /// </summary>
    private static IReadOnlyList<WebViewCookie> ParseCookiesJson(string json)
    {
        var cookies = new List<WebViewCookie>();
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return cookies;

        // Each cookie is a JSON object within the array.
        // We parse manually to avoid System.Text.Json dependency in the adapter.
        int idx = 0;
        while (idx < json.Length)
        {
            int objStart = json.IndexOf('{', idx);
            if (objStart < 0) break;
            int objEnd = json.IndexOf('}', objStart);
            if (objEnd < 0) break;

            var obj = json.Substring(objStart, objEnd - objStart + 1);
            idx = objEnd + 1;

            var name = ExtractJsonString(obj, "name");
            var value = ExtractJsonString(obj, "value");
            var domain = ExtractJsonString(obj, "domain");
            var path = ExtractJsonString(obj, "path");
            var expiresStr = ExtractJsonRaw(obj, "expires");
            var isSecure = ExtractJsonRaw(obj, "isSecure") == "true";
            var isHttpOnly = ExtractJsonRaw(obj, "isHttpOnly") == "true";

            DateTimeOffset? expires = null;
            if (double.TryParse(expiresStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var unix) && unix > 0)
            {
                expires = DateTimeOffset.FromUnixTimeSeconds((long)unix);
            }

            cookies.Add(new WebViewCookie(name, value, domain, path, expires, isSecure, isHttpOnly));
        }

        return cookies;
    }

    private static string ExtractJsonString(string json, string key)
    {
        var needle = $"\"{key}\":\"";
        var start = json.IndexOf(needle, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += needle.Length;
        var end = start;
        while (end < json.Length)
        {
            if (json[end] == '"' && (end == start || json[end - 1] != '\\')) break;
            end++;
        }
        return json.Substring(start, end - start).Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private static string ExtractJsonRaw(string json, string key)
    {
        var needle = $"\"{key}\":";
        var start = json.IndexOf(needle, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += needle.Length;
        var end = start;
        while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
        return json.Substring(start, end - start).Trim();
    }

    private void BeginApiNavigation(Guid navigationId, Uri requestUri)
    {
        _apiNavigationActive = true;
        _activeNavigationId = navigationId;
        _activeRequestUri = requestUri;
        _activeNavigationCompleted = false;
        _nativeCorrelationActive = false;
        _nativeCorrelationId = Guid.Empty;
    }

    private void RaiseNavigationCompleted(Guid navigationId, Uri requestUri, NavigationCompletedStatus status, Exception? error)
        => SafeRaise(() => NavigationCompleted?.Invoke(this, new NavigationCompletedEventArgs(navigationId, requestUri, status, error)));

    private void RaiseWebMessageReceived(string body, string origin, Guid channelId, int protocolVersion)
        => SafeRaise(() => WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs(body, origin, channelId, protocolVersion)));

    private void CompleteCanceledFromPolicyDecision(Guid navigationId, Uri requestUri)
    {
        if (_detached) return;

        lock (_navLock)
        {
            if (_activeNavigationId == navigationId && _activeNavigationCompleted)
                return;

            _activeNavigationId = navigationId;
            _activeRequestUri = requestUri;
            _activeNavigationCompleted = true;
        }

        RaiseNavigationCompleted(navigationId, requestUri, NavigationCompletedStatus.Canceled, error: null);

        lock (_navLock)
        {
            ClearNativeCorrelationIfNeeded();
            _apiNavigationActive = false;
        }
    }

    private void OnNavigationTerminal(NavigationCompletedStatus status, Exception? error, Uri? requestUriOverride)
    {
        if (_detached) return;

        Guid navId;
        Uri requestUri;

        lock (_navLock)
        {
            if (_activeNavigationId == Guid.Empty || _activeNavigationCompleted)
            {
                ClearNativeCorrelationIfNeeded();
                _apiNavigationActive = false;
                return;
            }

            _activeNavigationCompleted = true;
            navId = _activeNavigationId;
            requestUri = requestUriOverride ?? _activeRequestUri ?? new Uri("about:blank");
        }

        RaiseNavigationCompleted(navId, requestUri, status, error);

        lock (_navLock)
        {
            ClearNativeCorrelationIfNeeded();
            _apiNavigationActive = false;
        }
    }

    // Must be called while holding _navLock.
    private void ClearNativeCorrelationIfNeeded()
    {
        _nativeCorrelationActive = false;
        _nativeCorrelationId = Guid.Empty;
    }

    // Must be called while holding _navLock.
    private Guid GetOrCreateNativeCorrelationId(int navigationType, bool continuation)
    {
        if (!_nativeCorrelationActive)
        {
            _nativeCorrelationActive = true;
            _nativeCorrelationId = Guid.NewGuid();
            return _nativeCorrelationId;
        }

        // Redirects and other in-flight continuations should reuse the same correlation id.
        if (continuation)
        {
            return _nativeCorrelationId;
        }

        if (navigationType < 0)
        {
            navigationType = 5;
        }

        // LinkActivated=0, FormSubmitted=1, BackForward=2, Reload=3, FormResubmitted=4, Other=5
        if (navigationType is 0 or 1 or 4)
        {
            _nativeCorrelationId = Guid.NewGuid();
            return _nativeCorrelationId;
        }

        return _nativeCorrelationId;
    }

    private void ClearNavigationState()
    {
        lock (_navLock)
        {
            _activeNavigationId = Guid.Empty;
            _activeRequestUri = null;
            _activeNavigationCompleted = false;
            _apiNavigationActive = false;
            ClearNativeCorrelationIfNeeded();
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
            throw new ObjectDisposedException(nameof(MacOSWebViewAdapter));
        }

        if (!_attached || _native == IntPtr.Zero)
        {
            throw new InvalidOperationException("Adapter must be attached before use.");
        }
    }

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

    // ==== Native callbacks (called from native shim) ====

    private void OnPolicyRequest(ulong requestId, string? url, bool isMainFrame, bool isNewWindow, int navigationType)
    {
        _ = DecidePolicyAsync(requestId, url, isMainFrame, isNewWindow, navigationType);
    }

    private async Task DecidePolicyAsync(ulong requestId, string? url, bool isMainFrame, bool isNewWindow, int navigationType)
    {
        try
        {
            bool apiActive;
            lock (_navLock) { apiActive = _apiNavigationActive; }

            if (DiagnosticsEnabled)
            {
                Console.WriteLine($"[Agibuild.WebView] PolicyRequest id={requestId} main={isMainFrame} newWin={isNewWindow} api={apiActive} url='{url ?? "<null>"}' navType={navigationType}");
            }

            if (_detached)
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: false);
                return;
            }

            if (isNewWindow)
            {
                Uri? newWindowUri = null;
                if (url is not null) Uri.TryCreate(url, UriKind.Absolute, out newWindowUri);
                SafeRaise(() => NewWindowRequested?.Invoke(this, new NewWindowRequestedEventArgs(newWindowUri)));
                NativeMethods.PolicyDecide(_native, requestId, allow: false);
                return;
            }

            if (!isMainFrame)
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: true);
                return;
            }

            // Do not consult host for adapter-initiated navigations.
            if (apiActive)
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var requestUri))
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: true);
                return;
            }

            var host = _host;
            if (host is null)
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: false);
                return;
            }

            Guid correlationId;
            NativeNavigationStartingInfo info;
            lock (_navLock)
            {
                var continuation = _nativeCorrelationActive && _activeNavigationId != Guid.Empty && !_activeNavigationCompleted;
                correlationId = GetOrCreateNativeCorrelationId(navigationType, continuation);
                info = new NativeNavigationStartingInfo(correlationId, requestUri, IsMainFrame: true);
            }

            var decision = await host.OnNativeNavigationStartingAsync(info).ConfigureAwait(false);

            if (!decision.IsAllowed)
            {
                if (decision.NavigationId != Guid.Empty)
                {
                    CompleteCanceledFromPolicyDecision(decision.NavigationId, requestUri);
                }

                NativeMethods.PolicyDecide(_native, requestId, allow: false);
                return;
            }

            if (decision.NavigationId == Guid.Empty)
            {
                NativeMethods.PolicyDecide(_native, requestId, allow: false);
                return;
            }

            lock (_navLock)
            {
                _activeNavigationId = decision.NavigationId;
                _activeRequestUri = requestUri;
                _activeNavigationCompleted = false;
            }

            NativeMethods.PolicyDecide(_native, requestId, allow: true);
        }
        catch
        {
            NativeMethods.PolicyDecide(_native, requestId, allow: false);
        }
    }

    private void OnNavigationCompletedNative(string? url, int status, long errorCode, string? errorMessage)
    {
        // status: 0=Success, 1=Failure, 2=Canceled, 3=Timeout, 4=Network, 5=Ssl
        var requestUri = (url is not null && Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            ? parsed
            : null;

        if (status == 0)
        {
            OnNavigationTerminal(NavigationCompletedStatus.Success, error: null, requestUriOverride: requestUri);
            return;
        }

        if (status == 2)
        {
            OnNavigationTerminal(NavigationCompletedStatus.Canceled, error: null, requestUriOverride: requestUri);
            return;
        }

        var msg = string.IsNullOrWhiteSpace(errorMessage) ? $"Navigation failed (code={errorCode})." : errorMessage!;
        var navId = _activeNavigationId;
        var navUri = requestUri ?? _activeRequestUri ?? new Uri("about:blank");

        Exception error = status switch
        {
            3 => new WebViewTimeoutException(msg, navId, navUri),
            4 => new WebViewNetworkException(msg, navId, navUri),
            5 => new WebViewSslException(msg, navId, navUri),
            _ => new WebViewNavigationException(msg, navId, navUri),
        };

        OnNavigationTerminal(NavigationCompletedStatus.Failure, error: error, requestUriOverride: requestUri);
    }

    private void OnScriptResultNative(ulong requestId, string? result, string? errorMessage)
    {
        if (!_scriptTcsById.TryRemove(requestId, out var tcs))
        {
            return;
        }

        if (_detached)
        {
            tcs.TrySetException(new ObjectDisposedException(nameof(MacOSWebViewAdapter)));
            return;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            tcs.TrySetException(new WebViewScriptException(errorMessage));
            return;
        }

        // v1 semantics: normalize null/undefined/no-return to null; otherwise stable string representation.
        tcs.TrySetResult(result);
    }

    private void OnMessageNative(string? body, string? origin)
    {
        if (_detached)
        {
            return;
        }

        var channelId = _host?.ChannelId ?? Guid.Empty;
        RaiseWebMessageReceived(body ?? string.Empty, origin ?? string.Empty, channelId, protocolVersion: 1);
    }

    // ==== Native interop ====

    private static class NativeMethods
    {
        private const string LibraryName = "AgibuildWebViewWk";

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!OperatingSystem.IsMacOS())
            {
                return IntPtr.Zero;
            }

            if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            {
                return IntPtr.Zero;
            }

            var baseDir = AppContext.BaseDirectory;
            var candidate = Path.Combine(baseDir, "runtimes", "osx", "native", "libAgibuildWebViewWk.dylib");
            if (File.Exists(candidate))
            {
                return NativeLibrary.Load(candidate);
            }

            // Fallback: probe next to app.
            var flat = Path.Combine(baseDir, "libAgibuildWebViewWk.dylib");
            if (File.Exists(flat))
            {
                return NativeLibrary.Load(flat);
            }

            return IntPtr.Zero;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PolicyRequestCb(IntPtr userData, ulong requestId, IntPtr urlUtf8, byte isMainFrame, byte isNewWindow, int navigationType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void NavigationCompletedCb(IntPtr userData, IntPtr urlUtf8, int status, long errorCode, IntPtr errorMessageUtf8);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ScriptResultCb(IntPtr userData, ulong requestId, IntPtr resultUtf8, IntPtr errorMessageUtf8);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void MessageCb(IntPtr userData, IntPtr bodyUtf8, IntPtr originUtf8);

        [StructLayout(LayoutKind.Sequential)]
        internal struct AgWkCallbacks
        {
            public IntPtr on_policy_request;
            public IntPtr on_navigation_completed;
            public IntPtr on_script_result;
            public IntPtr on_message;
        }

        internal static MacOSWebViewAdapter? FromUserData(IntPtr userData)
        {
            if (userData == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var handle = GCHandle.FromIntPtr(userData);
                return handle.Target as MacOSWebViewAdapter;
            }
            catch
            {
                return null;
            }
        }

        internal static string PtrToString(IntPtr ptr)
            => ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;

        internal static string? PtrToStringNullable(IntPtr ptr)
            => ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);

        [DllImport(LibraryName, EntryPoint = "ag_wk_create", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Create(ref AgWkCallbacks callbacks, IntPtr userData);

        [DllImport(LibraryName, EntryPoint = "ag_wk_destroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Destroy(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_attach", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Attach(IntPtr handle, IntPtr nsViewPtr);

        [DllImport(LibraryName, EntryPoint = "ag_wk_detach", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Detach(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_policy_decide", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void PolicyDecide(IntPtr handle, ulong requestId, [MarshalAs(UnmanagedType.I1)] bool allow);

        [DllImport(LibraryName, EntryPoint = "ag_wk_navigate", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Navigate(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport(LibraryName, EntryPoint = "ag_wk_load_html", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void LoadHtml(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string html, [MarshalAs(UnmanagedType.LPUTF8Str)] string? baseUrl);

        [DllImport(LibraryName, EntryPoint = "ag_wk_eval_js", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EvalJs(IntPtr handle, ulong requestId, [MarshalAs(UnmanagedType.LPUTF8Str)] string script);

        [DllImport(LibraryName, EntryPoint = "ag_wk_go_back", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GoBack(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_go_forward", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GoForward(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_reload", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Reload(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_stop", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Stop(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_can_go_back", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CanGoBack(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_can_go_forward", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CanGoForward(IntPtr handle);

        [DllImport(LibraryName, EntryPoint = "ag_wk_get_webview_handle", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetWebViewHandle(IntPtr handle);

        // Cookie management
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CookiesGetCb(IntPtr context, IntPtr jsonUtf8);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CookieOpCb(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool success, IntPtr errorUtf8);

        [DllImport(LibraryName, EntryPoint = "ag_wk_cookies_get", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CookiesGet(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, CookiesGetCb callback, IntPtr context);

        [DllImport(LibraryName, EntryPoint = "ag_wk_cookie_set", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CookieSet(IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string domain,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
            double expiresUnix, [MarshalAs(UnmanagedType.I1)] bool isSecure, [MarshalAs(UnmanagedType.I1)] bool isHttpOnly,
            CookieOpCb callback, IntPtr context);

        [DllImport(LibraryName, EntryPoint = "ag_wk_cookie_delete", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CookieDelete(IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string domain,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
            CookieOpCb callback, IntPtr context);

        [DllImport(LibraryName, EntryPoint = "ag_wk_cookies_clear_all", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CookiesClearAll(IntPtr handle, CookieOpCb callback, IntPtr context);

        // M2: Environment options
        [DllImport(LibraryName, EntryPoint = "ag_wk_set_enable_dev_tools", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetEnableDevTools(IntPtr handle, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(LibraryName, EntryPoint = "ag_wk_set_ephemeral", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetEphemeral(IntPtr handle, [MarshalAs(UnmanagedType.I1)] bool ephemeral);

        [DllImport(LibraryName, EntryPoint = "ag_wk_set_user_agent", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetUserAgent(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string? userAgent);
    }
}

