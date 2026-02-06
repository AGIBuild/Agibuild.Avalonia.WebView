using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters.Abstractions;

namespace Agibuild.Avalonia.WebView.Testing;

internal sealed class MockWebViewAdapter : IWebViewAdapter
{
    private IWebViewAdapterHost? _host;
    private bool _initialized;
    private bool _attached;
    private bool _detached;

    public Guid? LastNavigationId { get; private set; }
    public Uri? LastNavigationUri { get; private set; }
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

    public Task NavigateAsync(Guid navigationId, Uri uri)
    {
        LastNavigateThreadId = Environment.CurrentManagedThreadId;
        LastNavigationId = navigationId;
        LastNavigationUri = uri;
        return Task.CompletedTask;
    }

    public Task NavigateToStringAsync(Guid navigationId, string html)
    {
        LastNavigateToStringThreadId = Environment.CurrentManagedThreadId;
        LastNavigationId = navigationId;
        LastNavigationUri = null;
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
}
