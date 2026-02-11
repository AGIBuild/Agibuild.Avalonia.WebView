using Agibuild.Avalonia.WebView.Adapters.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Runtime implementation of <see cref="IWebDialog"/>.
/// Wraps a <see cref="WebViewCore"/> and delegates window management to a <see cref="IDialogHost"/>.
/// </summary>
public sealed class WebDialog : IWebDialog
{
    private readonly IDialogHost _host;
    private readonly WebViewCore _core;
    private bool _disposed;

    /// <summary>
    /// Creates a WebDialog with the given dialog host and adapter.
    /// The dialog host provides window management (Show/Close/Resize/Move).
    /// </summary>
    internal WebDialog(IDialogHost host, IWebViewAdapter adapter, IWebViewDispatcher dispatcher, ILogger<WebViewCore>? logger = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _core = new WebViewCore(adapter, dispatcher, logger ?? NullLogger<WebViewCore>.Instance);

        _host.HostClosing += OnHostClosing;
    }

    // ==== IWebDialog ====

    public string? Title
    {
        get => _host.Title;
        set => _host.Title = value;
    }

    public bool CanUserResize
    {
        get => _host.CanUserResize;
        set => _host.CanUserResize = value;
    }

    public void Show() => _host.Show();

    public bool Show(global::Avalonia.Platform.IPlatformHandle owner) => _host.ShowWithOwner(owner);

    public void Close()
    {
        _host.Close();
    }

    public bool Resize(int width, int height) => _host.Resize(width, height);

    public bool Move(int x, int y) => _host.Move(x, y);

    public event EventHandler? Closing;

    // ==== IWebView delegation ====

    public Uri Source
    {
        get => _core.Source;
        set => _core.Source = value;
    }

    public bool CanGoBack => _core.CanGoBack;
    public bool CanGoForward => _core.CanGoForward;
    public bool IsLoading => _core.IsLoading;
    public Guid ChannelId => _core.ChannelId;

    public Task NavigateAsync(Uri uri) => _core.NavigateAsync(uri);
    public Task NavigateToStringAsync(string html) => _core.NavigateToStringAsync(html);
    public Task NavigateToStringAsync(string html, Uri? baseUrl) => _core.NavigateToStringAsync(html, baseUrl);
    public Task<string?> InvokeScriptAsync(string script) => _core.InvokeScriptAsync(script);

    public bool GoBack() => _core.GoBack();
    public bool GoForward() => _core.GoForward();
    public bool Refresh() => _core.Refresh();
    public bool Stop() => _core.Stop();

    public ICookieManager? TryGetCookieManager() => _core.TryGetCookieManager();
    public ICommandManager? TryGetCommandManager() => _core.TryGetCommandManager();
    public IWebViewRpcService? Rpc => _core.Rpc;
    public Task<byte[]> CaptureScreenshotAsync() => _core.CaptureScreenshotAsync();
    public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null) => _core.PrintToPdfAsync(options);

    /// <inheritdoc cref="WebViewCore.ZoomFactor"/>
    public double ZoomFactor
    {
        get => _core.ZoomFactor;
        set => _core.ZoomFactor = value;
    }
    /// <inheritdoc cref="WebViewCore.ZoomFactorChanged"/>
    public event EventHandler<double>? ZoomFactorChanged
    {
        add => _core.ZoomFactorChanged += value;
        remove => _core.ZoomFactorChanged -= value;
    }

    /// <inheritdoc cref="WebViewCore.FindInPageAsync"/>
    public Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null) => _core.FindInPageAsync(text, options);
    /// <inheritdoc cref="WebViewCore.StopFindInPage"/>
    public void StopFindInPage(bool clearHighlights = true) => _core.StopFindInPage(clearHighlights);

    /// <inheritdoc cref="WebViewCore.AddPreloadScript"/>
    public string AddPreloadScript(string javaScript) => _core.AddPreloadScript(javaScript);
    /// <inheritdoc cref="WebViewCore.RemovePreloadScript"/>
    public void RemovePreloadScript(string scriptId) => _core.RemovePreloadScript(scriptId);

    /// <inheritdoc cref="WebViewCore.ContextMenuRequested"/>
    public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested
    {
        add => _core.ContextMenuRequested += value;
        remove => _core.ContextMenuRequested -= value;
    }

    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted
    {
        add => _core.NavigationStarted += value;
        remove => _core.NavigationStarted -= value;
    }

    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted
    {
        add => _core.NavigationCompleted += value;
        remove => _core.NavigationCompleted -= value;
    }

    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested
    {
        add => _core.NewWindowRequested += value;
        remove => _core.NewWindowRequested -= value;
    }

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived
    {
        add => _core.WebMessageReceived += value;
        remove => _core.WebMessageReceived -= value;
    }

    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested
    {
        add => _core.WebResourceRequested += value;
        remove => _core.WebResourceRequested -= value;
    }

    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested
    {
        add => _core.EnvironmentRequested += value;
        remove => _core.EnvironmentRequested -= value;
    }

    public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested
    {
        add => _core.DownloadRequested += value;
        remove => _core.DownloadRequested -= value;
    }

    public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested
    {
        add => _core.PermissionRequested += value;
        remove => _core.PermissionRequested -= value;
    }

    public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated
    {
        add => _core.AdapterCreated += value;
        remove => _core.AdapterCreated -= value;
    }

    public event EventHandler? AdapterDestroyed
    {
        add => _core.AdapterDestroyed += value;
        remove => _core.AdapterDestroyed -= value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _host.HostClosing -= OnHostClosing;
        _core.Dispose();
        _host.Close();
    }

    private void OnHostClosing(object? sender, EventArgs e)
    {
        Closing?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Abstraction for the dialog window host (platform window management).
/// Decoupled from Avalonia to enable unit testing with mocks.
/// </summary>
public interface IDialogHost
{
    string? Title { get; set; }
    bool CanUserResize { get; set; }

    void Show();
    bool ShowWithOwner(global::Avalonia.Platform.IPlatformHandle owner);
    void Close();
    bool Resize(int width, int height);
    bool Move(int x, int y);

    event EventHandler? HostClosing;
}
