using Avalonia.Platform;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Adapters;

public interface IWebViewAdapter
{
    void Initialize(IWebView host);
    void Attach(IPlatformHandle parentHandle);
    void Detach();

    Task NavigateAsync(Uri uri);
    Task NavigateToStringAsync(string html);
    Task<string?> InvokeScriptAsync(string script);

    bool GoBack();
    bool GoForward();
    bool Refresh();
    bool Stop();

    bool CanGoBack { get; }
    bool CanGoForward { get; }

    event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
}
