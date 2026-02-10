using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView;

public enum NavigationCompletedStatus
{
    Success,
    Failure,
    Canceled,
    Superseded
}

public enum WebAuthStatus
{
    Success,
    UserCancel,
    Timeout,
    Error
}

public enum WebMessageDropReason
{
    OriginNotAllowed,
    ProtocolMismatch,
    ChannelMismatch
}

public interface IWebView : IDisposable
{
    Uri Source { get; set; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool IsLoading { get; }

    Guid ChannelId { get; }

    Task NavigateAsync(Uri uri);
    Task NavigateToStringAsync(string html);
    Task NavigateToStringAsync(string html, Uri? baseUrl);
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

    /// <summary>Raised after the native adapter is attached and ready. The event args carry the typed platform handle.</summary>
    event EventHandler<AdapterCreatedEventArgs>? AdapterCreated;

    /// <summary>Raised before the native adapter is detached/destroyed. After this event, <see cref="INativeWebViewHandleProvider.TryGetWebViewHandle"/> returns <c>null</c>.</summary>
    event EventHandler? AdapterDestroyed;
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

public interface IWebViewDispatcher
{
    bool CheckAccess();

    Task InvokeAsync(Action action);
    Task<T> InvokeAsync<T>(Func<T> func);
    Task InvokeAsync(Func<Task> func);
    Task<T> InvokeAsync<T>(Func<Task<T>> func);
}

internal readonly record struct NativeNavigationStartingInfo(
    Guid CorrelationId,
    Uri RequestUri,
    bool IsMainFrame);

internal readonly record struct NativeNavigationStartingDecision(
    bool IsAllowed,
    Guid NavigationId);

internal interface IWebViewAdapterHost
{
    Guid ChannelId { get; }

    ValueTask<NativeNavigationStartingDecision> OnNativeNavigationStartingAsync(NativeNavigationStartingInfo info);
}

public readonly record struct WebMessageEnvelope(
    string Body,
    string Origin,
    Guid ChannelId,
    int ProtocolVersion);

public readonly record struct WebMessagePolicyDecision(bool IsAllowed, WebMessageDropReason? DropReason)
{
    public static WebMessagePolicyDecision Allow() => new(true, null);

    public static WebMessagePolicyDecision Deny(WebMessageDropReason reason) => new(false, reason);
}

public interface IWebMessagePolicy
{
    WebMessagePolicyDecision Evaluate(in WebMessageEnvelope envelope);
}

public readonly record struct WebMessageDropDiagnostic(WebMessageDropReason Reason, string Origin, Guid ChannelId);

public interface IWebMessageDropDiagnosticsSink
{
    void OnMessageDropped(in WebMessageDropDiagnostic diagnostic);
}

public interface IWebViewEnvironmentOptions
{
    /// <summary>Enable browser developer tools (Inspector). Platform-specific: macOS requires 13.3+.</summary>
    bool EnableDevTools { get; set; }

    /// <summary>Override the default User-Agent string. Null means use the platform default.</summary>
    string? CustomUserAgent { get; set; }

    /// <summary>Use an ephemeral (non-persistent) data store. Cookies and storage are discarded when the WebView is disposed.</summary>
    bool UseEphemeralSession { get; set; }
}

public interface INativeWebViewHandleProvider
{
    IPlatformHandle? TryGetWebViewHandle();
}

/// <summary>Typed platform handle for Windows WebView2. Cast from <see cref="IPlatformHandle"/> returned by <see cref="INativeWebViewHandleProvider"/>.</summary>
public interface IWindowsWebView2PlatformHandle : IPlatformHandle
{
    /// <summary>Pointer to the <c>ICoreWebView2</c> COM object.</summary>
    nint CoreWebView2Handle { get; }

    /// <summary>Pointer to the <c>ICoreWebView2Controller</c> COM object.</summary>
    nint CoreWebView2ControllerHandle { get; }
}

/// <summary>Typed platform handle for Apple WKWebView (macOS and iOS).</summary>
public interface IAppleWKWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>Objective-C pointer to the <c>WKWebView</c> instance.</summary>
    nint WKWebViewHandle { get; }
}

/// <summary>Typed platform handle for GTK WebKitWebView (Linux).</summary>
public interface IGtkWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>Pointer to the <c>WebKitWebView</c> GObject instance.</summary>
    nint WebKitWebViewHandle { get; }
}

/// <summary>Typed platform handle for Android WebView.</summary>
public interface IAndroidWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>JNI handle to the Android <c>WebView</c> instance.</summary>
    nint AndroidWebViewHandle { get; }
}

/// <summary>Cookie management for the WebView instance.</summary>
[Experimental("AGWV001")]
public interface ICookieManager
{
    Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri);
    Task SetCookieAsync(WebViewCookie cookie);
    Task DeleteCookieAsync(WebViewCookie cookie);
    Task ClearAllCookiesAsync();
}

/// <summary>Placeholder — command management is not yet implemented.</summary>
[Experimental("AGWV002")]
public interface ICommandManager
{
}

/// <summary>Abstraction for a top-level window that can serve as an owner for dialogs.</summary>
public interface ITopLevelWindow
{
    /// <summary>The underlying platform handle for the window.</summary>
    IPlatformHandle? PlatformHandle { get; }
}

/// <summary>Factory for creating <see cref="IWebDialog"/> instances.</summary>
public interface IWebDialogFactory
{
    /// <summary>
    /// Creates a new WebDialog with optional environment options.
    /// The dialog is not shown until <see cref="IWebDialog.Show()"/> is called.
    /// </summary>
    IWebDialog Create(IWebViewEnvironmentOptions? options = null);
}

public sealed class AuthOptions
{
    /// <summary>The OAuth authorization URL to navigate to.</summary>
    public Uri? AuthorizeUri { get; set; }

    /// <summary>The expected callback/redirect URI. Navigation to this URI completes the flow.</summary>
    public Uri? CallbackUri { get; set; }

    /// <summary>Use an ephemeral (non-persistent) data store for the authentication dialog.</summary>
    public bool UseEphemeralSession { get; set; } = true;

    /// <summary>Optional timeout for the authentication flow. Default: no timeout.</summary>
    public TimeSpan? Timeout { get; set; }
}

public sealed class WebAuthResult
{
    public WebAuthStatus Status { get; init; }
    public Uri? CallbackUri { get; init; }
    public string? Error { get; init; }
}

public sealed class NavigationStartingEventArgs : EventArgs
{
    public NavigationStartingEventArgs(Uri requestUri)
    {
        NavigationId = Guid.Empty;
        RequestUri = requestUri;
    }

    public NavigationStartingEventArgs(Guid navigationId, Uri requestUri)
    {
        NavigationId = navigationId;
        RequestUri = requestUri;
    }

    public Guid NavigationId { get; }
    public Uri RequestUri { get; }
    public bool Cancel { get; set; }
}

public sealed class NavigationCompletedEventArgs : EventArgs
{
    public NavigationCompletedEventArgs()
    {
        NavigationId = Guid.Empty;
        RequestUri = new Uri("about:blank");
        Status = NavigationCompletedStatus.Success;
        Error = null;
    }

    public NavigationCompletedEventArgs(
        Guid navigationId,
        Uri requestUri,
        NavigationCompletedStatus status,
        Exception? error)
    {
        if (status == NavigationCompletedStatus.Failure && error is null)
        {
            throw new ArgumentNullException(nameof(error), "Error is required when Status=Failure.");
        }

        if (status != NavigationCompletedStatus.Failure && error is not null)
        {
            throw new ArgumentException("Error must be null when Status is not Failure.", nameof(error));
        }

        NavigationId = navigationId;
        RequestUri = requestUri;
        Status = status;
        Error = error;
    }

    public Guid NavigationId { get; }
    public Uri RequestUri { get; }
    public NavigationCompletedStatus Status { get; }
    public Exception? Error { get; }
}

public sealed class NewWindowRequestedEventArgs : EventArgs
{
    public NewWindowRequestedEventArgs(Uri? uri = null)
    {
        Uri = uri;
    }

    /// <summary>The URI that was requested to open in a new window.</summary>
    public Uri? Uri { get; }

    /// <summary>
    /// Set to <c>true</c> to indicate the event has been handled.
    /// When unhandled, the <see cref="WebView"/> control will navigate
    /// to the URI in the current view instead of opening a new window.
    /// </summary>
    public bool Handled { get; set; }
}

public sealed class WebMessageReceivedEventArgs : EventArgs
{
    public WebMessageReceivedEventArgs()
    {
        Body = string.Empty;
        Origin = string.Empty;
        ChannelId = Guid.Empty;
        ProtocolVersion = 1;
    }

    public WebMessageReceivedEventArgs(string body, string origin, Guid channelId)
    {
        Body = body;
        Origin = origin;
        ChannelId = channelId;
        ProtocolVersion = 1;
    }

    public WebMessageReceivedEventArgs(string body, string origin, Guid channelId, int protocolVersion)
    {
        Body = body;
        Origin = origin;
        ChannelId = channelId;
        ProtocolVersion = protocolVersion;
    }

    public string Body { get; }
    public string Origin { get; }
    public Guid ChannelId { get; }
    public int ProtocolVersion { get; }
}

/// <summary>
/// Raised when a registered custom-scheme request is intercepted.
/// The handler can supply a response body, content type, and status code.
/// Only custom schemes registered via <see cref="IWebView"/> are intercepted;
/// standard http/https requests cannot be intercepted on all platforms.
/// </summary>
[Experimental("AGWV004")]
public sealed class WebResourceRequestedEventArgs : EventArgs
{
    public WebResourceRequestedEventArgs() { }

    public WebResourceRequestedEventArgs(Uri requestUri, string method)
    {
        RequestUri = requestUri;
        Method = method;
    }

    /// <summary>The URI of the intercepted request.</summary>
    public Uri? RequestUri { get; init; }

    /// <summary>HTTP method (GET, POST, etc.).</summary>
    public string Method { get; init; } = "GET";

    /// <summary>Set by the handler to provide a response body (UTF-8 string).</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Set by the handler to provide a response content type. Default: text/html.</summary>
    public string ResponseContentType { get; set; } = "text/html";

    /// <summary>Set by the handler to provide an HTTP status code. Default: 200.</summary>
    public int ResponseStatusCode { get; set; } = 200;

    /// <summary>Set to true to indicate the request has been handled and a response is provided.</summary>
    public bool Handled { get; set; }
}

/// <summary>Placeholder — environment requested event is not yet implemented.</summary>
[Experimental("AGWV005")]
public sealed class EnvironmentRequestedEventArgs : EventArgs
{
}

/// <summary>Event args for the <see cref="IWebView.AdapterCreated"/> event, carrying the typed native platform handle.</summary>
public sealed class AdapterCreatedEventArgs : EventArgs
{
    public AdapterCreatedEventArgs(IPlatformHandle? platformHandle)
    {
        PlatformHandle = platformHandle;
    }

    /// <summary>
    /// The typed native WebView handle, or <c>null</c> if the adapter does not support handle exposure.
    /// Cast to a platform-specific interface (e.g. <see cref="IWindowsWebView2PlatformHandle"/>) for typed access.
    /// </summary>
    public IPlatformHandle? PlatformHandle { get; }
}

public class WebViewNavigationException : Exception
{
    public WebViewNavigationException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, innerException)
    {
        NavigationId = navigationId;
        RequestUri = requestUri;
    }

    public Guid NavigationId { get; }
    public Uri RequestUri { get; }
}

/// <summary>Navigation failed due to a network connectivity issue (DNS, unreachable host, connection lost, no internet).</summary>
public class WebViewNetworkException : WebViewNavigationException
{
    public WebViewNetworkException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, navigationId, requestUri, innerException)
    {
    }
}

/// <summary>Navigation failed due to a TLS/certificate issue.</summary>
public class WebViewSslException : WebViewNavigationException
{
    public WebViewSslException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, navigationId, requestUri, innerException)
    {
    }
}

/// <summary>Navigation failed due to a request timeout.</summary>
public class WebViewTimeoutException : WebViewNavigationException
{
    public WebViewTimeoutException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, navigationId, requestUri, innerException)
    {
    }
}

/// <summary>Represents a cookie associated with a WebView instance.</summary>
public sealed record WebViewCookie(
    string Name,
    string Value,
    string Domain,
    string Path,
    DateTimeOffset? Expires,
    bool IsSecure,
    bool IsHttpOnly);

public class WebViewScriptException : Exception
{
    public WebViewScriptException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
