using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters;

namespace Agibuild.Avalonia.WebView.Tests;

public sealed class MockWebViewAdapter : IWebViewAdapter
{
#pragma warning disable CS0067
    private bool _initialized;
    private bool _attached;
    private bool _detached;

    public Uri? LastNavigationUri { get; private set; }
    public string? ScriptResult { get; set; }

    public bool CanGoBack { get; set; }
    public bool CanGoForward { get; set; }

    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
#pragma warning restore CS0067

    public void Initialize(IWebView host)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Initialize can only be called once.");
        }

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
    }

    public Task NavigateAsync(Uri uri)
    {
        LastNavigationUri = uri;
        return Task.CompletedTask;
    }

    public Task NavigateToStringAsync(string html)
    {
        LastNavigationUri = null;
        return Task.CompletedTask;
    }

    public Task<string?> InvokeScriptAsync(string script)
    {
        return Task.FromResult(ScriptResult);
    }

    public bool GoBack() => false;
    public bool GoForward() => false;
    public bool Refresh() => false;
    public bool Stop() => false;

    public NavigationStartingEventArgs? RaiseNavigationStarted(Uri requestUri)
    {
        if (_detached)
        {
            return null;
        }

        var args = new NavigationStartingEventArgs(requestUri);
        NavigationStarted?.Invoke(this, args);
        return args;
    }

    public void RaiseNavigationCompleted()
    {
        if (_detached)
        {
            return;
        }

        NavigationCompleted?.Invoke(this, new NavigationCompletedEventArgs());
    }
}
