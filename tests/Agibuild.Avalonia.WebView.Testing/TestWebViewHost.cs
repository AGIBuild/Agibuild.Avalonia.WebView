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
    public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested { add { } remove { } }
    public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested { add { } remove { } }
    public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated { add { } remove { } }
    public event EventHandler? AdapterDestroyed { add { } remove { } }

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
    public IWebViewRpcService? Rpc => null;
    public IBridgeService Bridge => throw new NotSupportedException("TestWebViewHost does not support Bridge. Use WebViewCore with MockWebViewAdapter instead.");
    public void OpenDevTools() { /* No-op for test stub. */ }
    public void CloseDevTools() { /* No-op for test stub. */ }
    public bool IsDevToolsOpen => false;
    public Task<byte[]> CaptureScreenshotAsync() => Task.FromException<byte[]>(new NotSupportedException());
    public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null) => Task.FromException<byte[]>(new NotSupportedException());

    // Zoom
    public double ZoomFactor { get; set; } = 1.0;
    public event EventHandler<double>? ZoomFactorChanged { add { } remove { } }

    // Find in Page
    public Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null)
        => Task.FromException<FindInPageResult>(new NotSupportedException());
    public void StopFindInPage(bool clearHighlights = true) { }

    // Preload Scripts
    public string AddPreloadScript(string javaScript) => throw new NotSupportedException();
    public void RemovePreloadScript(string scriptId) => throw new NotSupportedException();

    // Context Menu
    public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested { add { } remove { } }

    public void Dispose() { /* No-op for test stub. */ }
}
