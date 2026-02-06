using Avalonia.Platform;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Adapters.Abstractions;

internal interface IWebViewAdapter
{
    void Initialize(IWebViewAdapterHost host);
    void Attach(IPlatformHandle parentHandle);
    void Detach();

    Task NavigateAsync(Guid navigationId, Uri uri);
    Task NavigateToStringAsync(Guid navigationId, string html);
    Task<string?> InvokeScriptAsync(string script);

    bool GoBack(Guid navigationId);
    bool GoForward(Guid navigationId);
    bool Refresh(Guid navigationId);
    bool Stop();

    bool CanGoBack { get; }
    bool CanGoForward { get; }

    event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
}
