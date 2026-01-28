using Avalonia.Platform;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Adapters;

namespace Agibuild.Avalonia.WebView.Adapters.Windows;

public sealed class WindowsWebViewAdapter : IWebViewAdapter
{
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

    public void Initialize(IWebView host) => throw new NotSupportedException();
    public void Attach(IPlatformHandle parentHandle) => throw new NotSupportedException();
    public void Detach() => throw new NotSupportedException();

    public Task NavigateAsync(Uri uri) => throw new NotSupportedException();
    public Task NavigateToStringAsync(string html) => throw new NotSupportedException();
    public Task<string?> InvokeScriptAsync(string script) => throw new NotSupportedException();

    public bool GoBack() => throw new NotSupportedException();
    public bool GoForward() => throw new NotSupportedException();
    public bool Refresh() => throw new NotSupportedException();
    public bool Stop() => throw new NotSupportedException();
}
