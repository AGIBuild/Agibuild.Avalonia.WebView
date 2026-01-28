using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView;

public interface IWebView
{
    Uri Source { get; set; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    Task NavigateAsync(Uri uri);
    Task NavigateToStringAsync(string html);
    Task<string?> InvokeScriptAsync(string script);

    bool GoBack();
    bool GoForward();
    bool Refresh();
    bool Stop();

    ICookieManager? TryGetCookieManager();
    ICommandManager? TryGetCommandManager();

    event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;
}

public interface IWebDialog : IWebView
{
    string? Title { get; set; }
    bool CanUserResize { get; set; }

    void Show();
    bool Show(IPlatformHandle owner);
    void Close();
    bool Resize(int width, int height);
    bool Move(int x, int y);

    event EventHandler? Closing;
}

public interface IWebAuthBroker
{
    Task<WebAuthResult> AuthenticateAsync(ITopLevelWindow owner, AuthOptions options);
}

public interface IWebViewEnvironmentOptions
{
    bool EnableDevTools { get; set; }
}

public interface INativeWebViewHandleProvider
{
    IPlatformHandle? TryGetWebViewHandle();
}

public interface ICookieManager
{
}

public interface ICommandManager
{
}

public interface ITopLevelWindow
{
}

public sealed class AuthOptions
{
}

public sealed class WebAuthResult
{
}

public sealed class NavigationStartingEventArgs : EventArgs
{
    public NavigationStartingEventArgs(Uri requestUri)
    {
        RequestUri = requestUri;
    }

    public Uri RequestUri { get; }
    public bool Cancel { get; set; }
}

public sealed class NavigationCompletedEventArgs : EventArgs
{
}

public sealed class NewWindowRequestedEventArgs : EventArgs
{
}

public sealed class WebMessageReceivedEventArgs : EventArgs
{
}

public sealed class WebResourceRequestedEventArgs : EventArgs
{
}

public sealed class EnvironmentRequestedEventArgs : EventArgs
{
}
