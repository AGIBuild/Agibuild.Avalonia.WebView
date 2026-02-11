using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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

    /// <summary>
    /// Gets the RPC service for bidirectional JS ↔ C# method calls.
    /// Returns <c>null</c> until the WebMessage bridge is enabled.
    /// </summary>
    IWebViewRpcService? Rpc { get; }

    /// <summary>
    /// Captures a screenshot of the current viewport as a PNG byte array.
    /// Throws <see cref="NotSupportedException"/> if the adapter does not support screenshots.
    /// </summary>
    Task<byte[]> CaptureScreenshotAsync();

    /// <summary>
    /// Prints the current page to a PDF byte array.
    /// Throws <see cref="NotSupportedException"/> if the adapter does not support printing.
    /// </summary>
    Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null);

    event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;

    /// <summary>Raised when a file download is initiated. The handler can set <c>DownloadPath</c> or <c>Cancel</c>.</summary>
    event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;

    /// <summary>Raised when web content requests a permission (camera, mic, geolocation, etc.).</summary>
    event EventHandler<PermissionRequestedEventArgs>? PermissionRequested;

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

    /// <summary>
    /// Custom URI schemes to register. Must be set before WebView creation.
    /// Adapters that implement <c>ICustomSchemeAdapter</c> receive these during initialization.
    /// </summary>
    IReadOnlyList<CustomSchemeRegistration> CustomSchemes { get; }

    /// <summary>
    /// JavaScript snippets to inject at document start on every new page load.
    /// These are applied globally to all new WebView instances.
    /// </summary>
    IReadOnlyList<string> PreloadScripts { get; }
}

/// <summary>Describes a custom URI scheme to register with the WebView.</summary>
public sealed class CustomSchemeRegistration
{
    /// <summary>The scheme name (e.g., "app", "myprotocol"). Do not include "://".</summary>
    public required string SchemeName { get; init; }

    /// <summary>Whether URIs with this scheme include an authority/host component (e.g., <c>app://host/path</c>).</summary>
    public bool HasAuthorityComponent { get; init; }

    /// <summary>Whether to treat this scheme as a secure context (like HTTPS). Only effective when <see cref="HasAuthorityComponent"/> is true.</summary>
    public bool TreatAsSecure { get; init; }
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

/// <summary>Options for PDF printing.</summary>
public sealed class PdfPrintOptions
{
    /// <summary>Whether to print in landscape orientation.</summary>
    public bool Landscape { get; set; }
    /// <summary>Page width in inches (default: 8.5 = US Letter).</summary>
    public double PageWidth { get; set; } = 8.5;
    /// <summary>Page height in inches (default: 11.0 = US Letter).</summary>
    public double PageHeight { get; set; } = 11.0;
    /// <summary>Top margin in inches.</summary>
    public double MarginTop { get; set; } = 0.4;
    /// <summary>Bottom margin in inches.</summary>
    public double MarginBottom { get; set; } = 0.4;
    /// <summary>Left margin in inches.</summary>
    public double MarginLeft { get; set; } = 0.4;
    /// <summary>Right margin in inches.</summary>
    public double MarginRight { get; set; } = 0.4;
    /// <summary>Scale factor (1.0 = 100%).</summary>
    public double Scale { get; set; } = 1.0;
    /// <summary>Whether to print background colors and images.</summary>
    public bool PrintBackground { get; set; } = true;
}

/// <summary>Options for in-page text search.</summary>
/// <summary>Media type at the context menu hit-test location.</summary>
public enum ContextMenuMediaType
{
    /// <summary>No media element at the location.</summary>
    None,
    /// <summary>An image element.</summary>
    Image,
    /// <summary>A video element.</summary>
    Video,
    /// <summary>An audio element.</summary>
    Audio
}

/// <summary>Event args for context menu interception.</summary>
public sealed class ContextMenuRequestedEventArgs : EventArgs
{
    /// <summary>X coordinate of the context menu trigger (CSS pixels).</summary>
    public double X { get; init; }
    /// <summary>Y coordinate of the context menu trigger (CSS pixels).</summary>
    public double Y { get; init; }
    /// <summary>The URI of the link at the location, if any.</summary>
    public Uri? LinkUri { get; init; }
    /// <summary>The selected text at the location, if any.</summary>
    public string? SelectionText { get; init; }
    /// <summary>The media type at the location.</summary>
    public ContextMenuMediaType MediaType { get; init; }
    /// <summary>The source URI of the media element, if any.</summary>
    public Uri? MediaSourceUri { get; init; }
    /// <summary>Whether the element at the location is editable.</summary>
    public bool IsEditable { get; init; }
    /// <summary>Set to true to suppress the native context menu.</summary>
    public bool Handled { get; set; }
}

public sealed class FindInPageOptions
{
    /// <summary>Whether the search is case-sensitive. Default: false.</summary>
    public bool CaseSensitive { get; init; }
    /// <summary>Search direction. True = forward (default), false = backward.</summary>
    public bool Forward { get; init; } = true;
}

/// <summary>Result of an in-page text search.</summary>
public sealed class FindInPageResult : EventArgs
{
    /// <summary>Zero-based index of the currently highlighted match.</summary>
    public int ActiveMatchIndex { get; init; }
    /// <summary>Total number of matches found on the page.</summary>
    public int TotalMatches { get; init; }
}

/// <summary>Standard editing commands supported by WebView.</summary>
public enum WebViewCommand
{
    /// <summary>Copy selected content to clipboard.</summary>
    Copy,
    /// <summary>Cut selected content to clipboard.</summary>
    Cut,
    /// <summary>Paste clipboard content.</summary>
    Paste,
    /// <summary>Select all content.</summary>
    SelectAll,
    /// <summary>Undo the last editing action.</summary>
    Undo,
    /// <summary>Redo the last undone editing action.</summary>
    Redo
}

/// <summary>Provides programmatic access to standard editing commands on a WebView.</summary>
public interface ICommandManager
{
    /// <summary>Copies the current selection to the clipboard.</summary>
    void Copy();
    /// <summary>Cuts the current selection to the clipboard.</summary>
    void Cut();
    /// <summary>Pastes clipboard content at the current position.</summary>
    void Paste();
    /// <summary>Selects all content in the WebView.</summary>
    void SelectAll();
    /// <summary>Undoes the last editing action.</summary>
    void Undo();
    /// <summary>Redoes the last undone editing action.</summary>
    void Redo();
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
/// Custom schemes must be registered via <see cref="IWebViewEnvironmentOptions.CustomSchemes"/>
/// before the WebView is created.
/// </summary>
[Experimental("AGWV004")]
public sealed class WebResourceRequestedEventArgs : EventArgs
{
    public WebResourceRequestedEventArgs() { }

    public WebResourceRequestedEventArgs(Uri requestUri, string method, IReadOnlyDictionary<string, string>? requestHeaders = null)
    {
        RequestUri = requestUri;
        Method = method;
        RequestHeaders = requestHeaders;
    }

    /// <summary>The URI of the intercepted request.</summary>
    public Uri? RequestUri { get; init; }

    /// <summary>HTTP method (GET, POST, etc.).</summary>
    public string Method { get; init; } = "GET";

    /// <summary>Request headers from the intercepted request. May be null if not available on the platform.</summary>
    public IReadOnlyDictionary<string, string>? RequestHeaders { get; init; }

    /// <summary>Set by the handler to provide a response body as a stream (supports binary content).</summary>
    public Stream? ResponseBody { get; set; }

    /// <summary>Set by the handler to provide a response content type. Default: text/html.</summary>
    public string ResponseContentType { get; set; } = "text/html";

    /// <summary>Set by the handler to provide an HTTP status code. Default: 200.</summary>
    public int ResponseStatusCode { get; set; } = 200;

    /// <summary>Set by the handler to provide custom response headers.</summary>
    public IDictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>Set to true to indicate the request has been handled and a response is provided.</summary>
    public bool Handled { get; set; }
}

/// <summary>Placeholder — environment requested event is not yet implemented.</summary>
[Experimental("AGWV005")]
public sealed class EnvironmentRequestedEventArgs : EventArgs
{
}

/// <summary>Raised when a file download is initiated by the web content.</summary>
public sealed class DownloadRequestedEventArgs : EventArgs
{
    public DownloadRequestedEventArgs(Uri downloadUri, string? suggestedFileName = null, string? contentType = null, long? contentLength = null)
    {
        DownloadUri = downloadUri;
        SuggestedFileName = suggestedFileName;
        ContentType = contentType;
        ContentLength = contentLength;
    }

    /// <summary>The URL of the resource being downloaded.</summary>
    public Uri DownloadUri { get; }

    /// <summary>Suggested filename from Content-Disposition header or URL path.</summary>
    public string? SuggestedFileName { get; }

    /// <summary>MIME type of the download content.</summary>
    public string? ContentType { get; }

    /// <summary>Content length in bytes, or null if unknown.</summary>
    public long? ContentLength { get; }

    /// <summary>Set by consumer to specify the save file path.</summary>
    public string? DownloadPath { get; set; }

    /// <summary>Set to true by consumer to cancel the download.</summary>
    public bool Cancel { get; set; }

    /// <summary>Set to true by consumer to indicate the download is fully handled externally.</summary>
    public bool Handled { get; set; }
}

/// <summary>The type of permission being requested by web content.</summary>
public enum WebViewPermissionKind
{
    Unknown = 0,
    Camera,
    Microphone,
    Geolocation,
    Notifications,
    ClipboardRead,
    ClipboardWrite,
    Midi,
    Sensors,
    Other
}

/// <summary>The decision for a permission request.</summary>
public enum PermissionState
{
    /// <summary>Let the platform handle it (show native dialog or apply default policy).</summary>
    Default = 0,
    /// <summary>Grant the permission.</summary>
    Allow,
    /// <summary>Deny the permission.</summary>
    Deny
}

/// <summary>Raised when web content requests a permission (camera, microphone, geolocation, etc.).</summary>
public sealed class PermissionRequestedEventArgs : EventArgs
{
    public PermissionRequestedEventArgs(WebViewPermissionKind permissionKind, Uri? origin = null)
    {
        PermissionKind = permissionKind;
        Origin = origin;
    }

    /// <summary>The type of permission being requested.</summary>
    public WebViewPermissionKind PermissionKind { get; }

    /// <summary>The origin (scheme + host) of the page requesting the permission.</summary>
    public Uri? Origin { get; }

    /// <summary>Set by consumer to Allow, Deny, or leave as Default for platform behavior.</summary>
    public PermissionState State { get; set; } = PermissionState.Default;
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

/// <summary>
/// Bidirectional JSON-RPC 2.0 service for JS ↔ C# method calls over the WebMessage bridge.
/// </summary>
public interface IWebViewRpcService
{
    /// <summary>Registers an async C# handler callable from JS.</summary>
    void Handle(string method, Func<JsonElement?, Task<object?>> handler);

    /// <summary>Registers a synchronous C# handler callable from JS.</summary>
    void Handle(string method, Func<JsonElement?, object?> handler);

    /// <summary>Removes a previously registered handler.</summary>
    void RemoveHandler(string method);

    /// <summary>Calls a JS-side handler and returns the raw result.</summary>
    Task<JsonElement> InvokeAsync(string method, object? args = null);

    /// <summary>Calls a JS-side handler and deserializes the result.</summary>
    Task<T?> InvokeAsync<T>(string method, object? args = null);
}

/// <summary>Exception thrown when an RPC call fails.</summary>
public class WebViewRpcException : Exception
{
    public WebViewRpcException(int code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>JSON-RPC error code.</summary>
    public int Code { get; }
}
