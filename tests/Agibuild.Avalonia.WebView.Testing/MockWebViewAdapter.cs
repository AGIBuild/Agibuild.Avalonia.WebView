using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Testing;

internal class MockWebViewAdapter : IWebViewAdapter
{
    private IWebViewAdapterHost? _host;
    private bool _initialized;
    private bool _attached;
    private bool _detached;

    // In-memory cookie store keyed by "name|domain|path" (used by MockWebViewAdapterWithCookies)
    protected readonly Dictionary<string, WebViewCookie> CookieStore = new();

    /// <summary>Creates a mock without cookie support. Use <see cref="CreateWithCookies"/> for cookie-enabled mock.</summary>
    public static MockWebViewAdapter Create() => new();

    /// <summary>Creates a mock that also implements <see cref="ICookieAdapter"/>.</summary>
    public static MockWebViewAdapterWithCookies CreateWithCookies() => new();

    public Guid? LastNavigationId { get; private set; }
    public Uri? LastNavigationUri { get; private set; }
    public Uri? LastBaseUrl { get; private set; }
    public int? LastNavigateThreadId { get; private set; }
    public int? LastNavigateToStringThreadId { get; private set; }
    public int? LastInvokeScriptThreadId { get; private set; }
    public string? ScriptResult { get; set; }
    public Exception? ScriptException { get; set; }

    public bool CanGoBack { get; set; }
    public bool CanGoForward { get; set; }

    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;

    public void Initialize(IWebViewAdapterHost host)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Initialize can only be called once.");
        }

        _host = host ?? throw new ArgumentNullException(nameof(host));
        _initialized = true;
    }

    public void Attach(IPlatformHandle parentHandle)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Attach requires Initialize first.");
        }

        if (_attached)
        {
            throw new InvalidOperationException("Attach can only be called once.");
        }

        _attached = true;
        AttachCallCount++;
    }

    public void Detach()
    {
        if (!_attached)
        {
            throw new InvalidOperationException("Detach requires Attach first.");
        }

        if (_detached)
        {
            throw new InvalidOperationException("Detach can only be called once.");
        }

        _detached = true;
        DetachCallCount++;
    }

    /// <summary>
    /// When true, NavigateAsync automatically raises NavigationCompleted(Success)
    /// and then calls <see cref="OnNavigationAutoCompleted"/> if set.
    /// Useful for integration-style tests like WebAuthBroker flows.
    /// </summary>
    public bool AutoCompleteNavigation { get; set; }

    /// <summary>Callback invoked after auto-completing navigation. Use to simulate post-navigation events.</summary>
    public Action? OnNavigationAutoCompleted { get; set; }

    public Task NavigateAsync(Guid navigationId, Uri uri)
    {
        LastNavigateThreadId = Environment.CurrentManagedThreadId;
        LastNavigationId = navigationId;
        LastNavigationUri = uri;

        if (AutoCompleteNavigation)
        {
            RaiseNavigationCompleted(NavigationCompletedStatus.Success);
            OnNavigationAutoCompleted?.Invoke();
        }

        return Task.CompletedTask;
    }

    public Task NavigateToStringAsync(Guid navigationId, string html)
        => NavigateToStringAsync(navigationId, html, baseUrl: null);

    public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)
    {
        LastNavigateToStringThreadId = Environment.CurrentManagedThreadId;
        LastNavigationId = navigationId;
        LastNavigationUri = null;
        LastBaseUrl = baseUrl;

        if (AutoCompleteNavigation)
        {
            RaiseNavigationCompleted(NavigationCompletedStatus.Success);
            OnNavigationAutoCompleted?.Invoke();
        }

        return Task.CompletedTask;
    }

    public Task<string?> InvokeScriptAsync(string script)
    {
        LastInvokeScriptThreadId = Environment.CurrentManagedThreadId;

        if (ScriptException is not null)
        {
            return Task.FromException<string?>(ScriptException);
        }

        return Task.FromResult(ScriptResult);
    }

    public int AttachCallCount { get; private set; }
    public int DetachCallCount { get; private set; }
    public int StopCallCount { get; private set; }

    public bool GoBackAccepted { get; set; }
    public bool GoForwardAccepted { get; set; }
    public bool RefreshAccepted { get; set; }
    public bool StopAccepted { get; set; }

    public int GoBackCallCount { get; private set; }
    public int GoForwardCallCount { get; private set; }
    public int RefreshCallCount { get; private set; }

    public Guid? LastGoBackNavigationId { get; private set; }
    public Guid? LastGoForwardNavigationId { get; private set; }
    public Guid? LastRefreshNavigationId { get; private set; }

    public bool GoBack(Guid navigationId)
    {
        GoBackCallCount++;
        LastGoBackNavigationId = navigationId;
        LastNavigationId = navigationId;
        return GoBackAccepted;
    }

    public bool GoForward(Guid navigationId)
    {
        GoForwardCallCount++;
        LastGoForwardNavigationId = navigationId;
        LastNavigationId = navigationId;
        return GoForwardAccepted;
    }

    public bool Refresh(Guid navigationId)
    {
        RefreshCallCount++;
        LastRefreshNavigationId = navigationId;
        LastNavigationId = navigationId;
        return RefreshAccepted;
    }

    public bool Stop()
    {
        StopCallCount++;
        return StopAccepted;
    }

    public ValueTask<NativeNavigationStartingDecision> SimulateNativeNavigationStartingAsync(
        Uri requestUri,
        Guid? correlationId = null,
        bool isMainFrame = true)
    {
        if (_detached)
        {
            return ValueTask.FromResult(new NativeNavigationStartingDecision(IsAllowed: false, NavigationId: Guid.Empty));
        }

        if (_host is null)
        {
            throw new InvalidOperationException("Initialize(IWebViewAdapterHost) must be called before simulating native navigation.");
        }

        var info = new NativeNavigationStartingInfo(
            CorrelationId: correlationId ?? Guid.NewGuid(),
            RequestUri: requestUri,
            IsMainFrame: isMainFrame);

        return InvokeAsync();

        async ValueTask<NativeNavigationStartingDecision> InvokeAsync()
        {
            var decision = await _host.OnNativeNavigationStartingAsync(info).ConfigureAwait(false);
            if (decision.IsAllowed && decision.NavigationId != Guid.Empty)
            {
                LastNavigationId = decision.NavigationId;
                LastNavigationUri = requestUri;
            }

            return decision;
        }
    }

    public void RaiseNavigationCompleted()
    {
        if (_detached)
        {
            return;
        }

        RaiseNavigationCompleted(NavigationCompletedStatus.Success);
    }

    public void RaiseNavigationCompleted(Guid navigationId, Uri requestUri, NavigationCompletedStatus status, Exception? error = null)
    {
        if (_detached)
        {
            return;
        }

        var args = new NavigationCompletedEventArgs(
            navigationId,
            requestUri,
            status,
            status == NavigationCompletedStatus.Failure ? error ?? new Exception("Navigation failed.") : null);
        NavigationCompleted?.Invoke(this, args);
    }

    public void RaiseNavigationCompleted(NavigationCompletedStatus status, Exception? error = null)
    {
        var navigationId = LastNavigationId ?? Guid.Empty;
        var requestUri = LastNavigationUri ?? new Uri("about:blank");
        RaiseNavigationCompleted(navigationId, requestUri, status, error);
    }

    public void RaiseNewWindowRequested(Uri? uri = null)
    {
        if (_detached) return;
        NewWindowRequested?.Invoke(this, new NewWindowRequestedEventArgs(uri));
    }

    public void RaiseWebMessage(string body, string origin, Guid channelId, int protocolVersion = 1)
    {
        if (_detached)
        {
            return;
        }

        WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs(body, origin, channelId, protocolVersion));
    }

    public void RaiseWebResourceRequested()
    {
        if (_detached) return;
        WebResourceRequested?.Invoke(this, new WebResourceRequestedEventArgs());
    }

    public void RaiseEnvironmentRequested()
    {
        if (_detached) return;
        EnvironmentRequested?.Invoke(this, new EnvironmentRequestedEventArgs());
    }

    protected static string CookieKey(string name, string domain, string path) => $"{name}|{domain}|{path}";

    /// <summary>Creates a mock that supports environment options.</summary>
    public static MockWebViewAdapterWithOptions CreateWithOptions() => new();

    /// <summary>Creates a mock that supports native handle provider.</summary>
    public static MockWebViewAdapterWithHandle CreateWithHandle() => new();
}

/// <summary>Mock adapter that also implements <see cref="IWebViewAdapterOptions"/> for environment options testing.</summary>
internal sealed class MockWebViewAdapterWithOptions : MockWebViewAdapter, IWebViewAdapterOptions
{
    public IWebViewEnvironmentOptions? AppliedOptions { get; private set; }
    public string? AppliedUserAgent { get; private set; }
    public int ApplyOptionsCallCount { get; private set; }
    public int SetUserAgentCallCount { get; private set; }

    public void ApplyEnvironmentOptions(IWebViewEnvironmentOptions options)
    {
        ApplyOptionsCallCount++;
        AppliedOptions = options;
    }

    public void SetCustomUserAgent(string? userAgent)
    {
        SetUserAgentCallCount++;
        AppliedUserAgent = userAgent;
    }
}

/// <summary>Mock adapter that also implements <see cref="INativeWebViewHandleProvider"/>.</summary>
internal sealed class MockWebViewAdapterWithHandle : MockWebViewAdapter, INativeWebViewHandleProvider
{
    public IPlatformHandle? HandleToReturn { get; set; }
    public int TryGetHandleCallCount { get; private set; }

    public IPlatformHandle? TryGetWebViewHandle()
    {
        TryGetHandleCallCount++;
        return HandleToReturn;
    }
}

/// <summary>Mock adapter that also implements <see cref="ICookieAdapter"/> for cookie management testing.</summary>
internal sealed class MockWebViewAdapterWithCookies : MockWebViewAdapter, ICookieAdapter
{
    public Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)
    {
        var host = uri.Host;
        var result = CookieStore.Values
            .Where(c => c.Domain.EndsWith(host, StringComparison.OrdinalIgnoreCase) || host.EndsWith(c.Domain, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<WebViewCookie>>(result);
    }

    public Task SetCookieAsync(WebViewCookie cookie)
    {
        CookieStore[CookieKey(cookie.Name, cookie.Domain, cookie.Path)] = cookie;
        return Task.CompletedTask;
    }

    public Task DeleteCookieAsync(WebViewCookie cookie)
    {
        CookieStore.Remove(CookieKey(cookie.Name, cookie.Domain, cookie.Path));
        return Task.CompletedTask;
    }

    public Task ClearAllCookiesAsync()
    {
        CookieStore.Clear();
        return Task.CompletedTask;
    }
}
