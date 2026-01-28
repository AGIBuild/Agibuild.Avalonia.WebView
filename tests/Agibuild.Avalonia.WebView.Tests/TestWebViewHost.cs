using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Tests;

internal sealed class TestWebViewHost : IWebView
{
    public Uri Source { get; set; } = new Uri("about:blank");
    public bool CanGoBack => false;
    public bool CanGoForward => false;

#pragma warning disable CS0067
    public event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
#pragma warning restore CS0067

    public Task NavigateAsync(Uri uri) => Task.CompletedTask;
    public Task NavigateToStringAsync(string html) => Task.CompletedTask;
    public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);

    public bool GoBack() => false;
    public bool GoForward() => false;
    public bool Refresh() => false;
    public bool Stop() => false;

    public ICookieManager? TryGetCookieManager() => null;
    public ICommandManager? TryGetCommandManager() => null;
}
