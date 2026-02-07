using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Testing;

public sealed class TestWebViewHost : IWebView
{
    public Uri Source { get; set; } = new Uri("about:blank");
    public bool CanGoBack => false;
    public bool CanGoForward => false;
    public bool IsLoading => false;
    public Guid ChannelId { get; } = Guid.NewGuid();

    // Empty accessors — test stub never raises these interface-required events.
    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted { add { } remove { } }
    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted { add { } remove { } }
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested { add { } remove { } }
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived { add { } remove { } }
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested { add { } remove { } }
    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested { add { } remove { } }

    public Task NavigateAsync(Uri uri) => Task.CompletedTask;
    public Task NavigateToStringAsync(string html) => Task.CompletedTask;
    public Task NavigateToStringAsync(string html, Uri? baseUrl) => Task.CompletedTask;
    public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);

    public bool GoBack() => false;
    public bool GoForward() => false;
    public bool Refresh() => false;
    public bool Stop() => false;

    public ICookieManager? TryGetCookieManager() => null;
    public ICommandManager? TryGetCommandManager() => null;

    public void Dispose() { /* No-op for test stub. */ }
}
