using Avalonia.Platform;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Adapters.Abstractions;

internal interface ICookieAdapter
{
    Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri);
    Task SetCookieAsync(WebViewCookie cookie);
    Task DeleteCookieAsync(WebViewCookie cookie);
    Task ClearAllCookiesAsync();
}

/// <summary>
/// Optional interface for adapters that support environment options (DevTools, UserAgent, Ephemeral).
/// Runtime checks for this via <c>adapter as IWebViewAdapterOptions</c>.
/// Must be called before <see cref="IWebViewAdapter.Attach"/>.
/// </summary>
internal interface IWebViewAdapterOptions
{
    void ApplyEnvironmentOptions(IWebViewEnvironmentOptions options);
    void SetCustomUserAgent(string? userAgent);
}

internal interface IWebViewAdapter
{
    void Initialize(IWebViewAdapterHost host);
    void Attach(IPlatformHandle parentHandle);
    void Detach();

    Task NavigateAsync(Guid navigationId, Uri uri);
    Task NavigateToStringAsync(Guid navigationId, string html);
    Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl);
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
