using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Avalonia.Platform;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Status of a navigation completion.
/// </summary>
public enum NavigationCompletedStatus
{
    /// <summary>Navigation completed successfully.</summary>
    Success,
    /// <summary>Navigation failed.</summary>
    Failure,
    /// <summary>Navigation was canceled.</summary>
    Canceled,
    /// <summary>Navigation was superseded by a newer navigation.</summary>
    Superseded
}

/// <summary>
/// Result status for a web authentication operation.
/// </summary>
public enum WebAuthStatus
{
    /// <summary>Authentication succeeded.</summary>
    Success,
    /// <summary>Authentication was canceled by the user.</summary>
    UserCancel,
    /// <summary>Authentication timed out.</summary>
    Timeout,
    /// <summary>Authentication failed due to an error.</summary>
    Error
}

/// <summary>
/// Reason why an incoming WebMessage was dropped.
/// </summary>
public enum WebMessageDropReason
{
    /// <summary>The message origin is not allowed by policy.</summary>
    OriginNotAllowed,
    /// <summary>The message protocol version does not match.</summary>
    ProtocolMismatch,
    /// <summary>The message channel id does not match.</summary>
    ChannelMismatch
}

/// <summary>
/// Categorizes operational failures to enable consistent error handling across adapters.
/// </summary>
public enum WebViewOperationFailureCategory
{
    /// <summary>The operation failed because the WebView was disposed/detached.</summary>
    Disposed,
    /// <summary>The operation failed because the WebView was not yet ready.</summary>
    NotReady,
    /// <summary>The operation failed because dispatching to the UI thread failed.</summary>
    DispatchFailed,
    /// <summary>The operation failed due to an adapter/platform failure.</summary>
    AdapterFailed
}

/// <summary>
/// Helpers to annotate exceptions with <see cref="WebViewOperationFailureCategory"/> for governance and diagnostics.
/// </summary>
public static class WebViewOperationFailure
{
    private const string CategoryDataKey = "Agibuild.WebView.OperationFailureCategory";

    /// <summary>Associates a failure category with an exception.</summary>
    /// <param name="exception">The exception to annotate.</param>
    /// <param name="category">The failure category.</param>
    public static void SetCategory(Exception exception, WebViewOperationFailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(exception);
        exception.Data[CategoryDataKey] = category;
    }

    /// <summary>Attempts to read a failure category from an exception.</summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <param name="category">When true is returned, receives the parsed category.</param>
    /// <returns>True when a category is present and can be parsed; otherwise false.</returns>
    public static bool TryGetCategory(Exception exception, out WebViewOperationFailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.Data[CategoryDataKey] is WebViewOperationFailureCategory typed)
        {
            category = typed;
            return true;
        }

        if (exception.Data[CategoryDataKey] is string text &&
            Enum.TryParse(text, ignoreCase: true, out WebViewOperationFailureCategory parsed))
        {
            category = parsed;
            return true;
        }

        category = default;
        return false;
    }
}

/// <summary>
/// Public WebView contract surface for navigation, scripting, events, and feature adapters.
/// </summary>
public interface IWebView : IDisposable
{
    /// <summary>Gets or sets the current navigation URI.</summary>
    Uri Source { get; set; }
    /// <summary>Gets whether the WebView can navigate back in history.</summary>
    bool CanGoBack { get; }
    /// <summary>Gets whether the WebView can navigate forward in history.</summary>
    bool CanGoForward { get; }
    /// <summary>Gets whether a navigation is currently in progress.</summary>
    bool IsLoading { get; }

    /// <summary>Gets the channel id used to isolate WebMessage traffic.</summary>
    Guid ChannelId { get; }

    /// <summary>Navigates to the specified URI.</summary>
    /// <param name="uri">The target URI.</param>
    Task NavigateAsync(Uri uri);
    /// <summary>Navigates to a string of HTML.</summary>
    /// <param name="html">The HTML content.</param>
    Task NavigateToStringAsync(string html);
    /// <summary>Navigates to a string of HTML with an optional base URL.</summary>
    /// <param name="html">The HTML content.</param>
    /// <param name="baseUrl">Optional base URI for relative URL resolution.</param>
    Task NavigateToStringAsync(string html, Uri? baseUrl);
    /// <summary>Executes JavaScript in the context of the current document.</summary>
    /// <param name="script">The JavaScript source.</param>
    Task<string?> InvokeScriptAsync(string script);

    /// <summary>Attempts to navigate back.</summary>
    Task<bool> GoBackAsync();
    /// <summary>Attempts to navigate forward.</summary>
    Task<bool> GoForwardAsync();
    /// <summary>Reloads the current page.</summary>
    Task<bool> RefreshAsync();
    /// <summary>Stops an active navigation, if any.</summary>
    Task<bool> StopAsync();

    /// <summary>Gets the cookie manager if supported by the adapter; otherwise null.</summary>
    ICookieManager? TryGetCookieManager();
    /// <summary>Gets the command manager if supported by the adapter; otherwise null.</summary>
    ICommandManager? TryGetCommandManager();
    /// <summary>Gets the typed native WebView handle if supported; otherwise null.</summary>
    Task<IPlatformHandle?> TryGetWebViewHandleAsync();

    /// <summary>
    /// Gets the RPC service for bidirectional JS ↔ C# method calls.
    /// Returns <c>null</c> until the WebMessage bridge is enabled.
    /// </summary>
    IWebViewRpcService? Rpc { get; }

    /// <summary>
    /// Gets the type-safe bridge service for exposing C# services to JS (<see cref="JsExportAttribute"/>)
    /// and importing JS services into C# (<see cref="JsImportAttribute"/>).
    /// <para>
    /// Accessing this property auto-enables the WebMessage bridge if it is not already enabled.
    /// </para>
    /// </summary>
    IBridgeService Bridge { get; }

    /// <summary>
    /// Opens the browser developer tools (inspector) at runtime.
    /// No-op if the platform adapter does not support runtime DevTools toggling.
    /// </summary>
    Task OpenDevToolsAsync();

    /// <summary>
    /// Closes the browser developer tools.
    /// No-op if the platform adapter does not support runtime DevTools toggling.
    /// </summary>
    Task CloseDevToolsAsync();

    /// <summary>
    /// Returns whether developer tools are currently open.
    /// Always returns false if the platform adapter does not support this check.
    /// </summary>
    Task<bool> IsDevToolsOpenAsync();

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

    /// <summary>Raised when a navigation starts. Handlers may cancel the navigation.</summary>
    event EventHandler<NavigationStartingEventArgs>? NavigationStarted;
    /// <summary>Raised when a navigation completes (success, failure, canceled, superseded).</summary>
    event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
    /// <summary>Raised when content requests opening a new window.</summary>
    event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
    /// <summary>Raised when a WebMessage is received from the page.</summary>
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    /// <summary>Raised when a custom-scheme request is intercepted.</summary>
    event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    /// <summary>Raised when the environment is requested (placeholder).</summary>
    event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;

    /// <summary>Raised when a file download is initiated. The handler can set <c>DownloadPath</c> or <c>Cancel</c>.</summary>
    event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;

    /// <summary>Raised when web content requests a permission (camera, mic, geolocation, etc.).</summary>
    event EventHandler<PermissionRequestedEventArgs>? PermissionRequested;

    /// <summary>Raised after the native adapter is attached and ready. The event args carry the typed platform handle.</summary>
    event EventHandler<AdapterCreatedEventArgs>? AdapterCreated;

    /// <summary>Raised before the native adapter is detached/destroyed. After this event, <see cref="INativeWebViewHandleProvider.TryGetWebViewHandle"/> returns <c>null</c>.</summary>
    event EventHandler? AdapterDestroyed;

    // ==================== Zoom ====================

    /// <summary>
    /// Gets or sets the zoom factor. 1.0 = 100%. Returns 1.0 if zoom is not supported.
    /// </summary>
    Task<double> GetZoomFactorAsync();

    /// <summary>Sets the zoom factor. 1.0 = 100%.</summary>
    /// <param name="zoomFactor">The zoom factor to set.</param>
    Task SetZoomFactorAsync(double zoomFactor);

    // ==================== Find in Page ====================

    /// <summary>
    /// Searches the current page for <paramref name="text"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">The adapter does not implement find-in-page.</exception>
    Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null);

    /// <summary>
    /// Stops an active find session and optionally clears highlights.
    /// </summary>
    /// <exception cref="NotSupportedException">The adapter does not implement find-in-page.</exception>
    Task StopFindInPageAsync(bool clearHighlights = true);

    // ==================== Preload Scripts ====================

    /// <summary>
    /// Adds a JavaScript snippet that runs before every page load.
    /// </summary>
    /// <returns>An opaque script ID for later removal.</returns>
    /// <exception cref="NotSupportedException">The adapter does not implement preload scripts.</exception>
    Task<string> AddPreloadScriptAsync(string javaScript);

    /// <summary>
    /// Removes a previously added preload script.
    /// </summary>
    /// <exception cref="NotSupportedException">The adapter does not implement preload scripts.</exception>
    Task RemovePreloadScriptAsync(string scriptId);

    // ==================== Context Menu ====================

    /// <summary>Raised when the user triggers a context menu (right-click, long-press).</summary>
    event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested;
}

/// <summary>
/// Dialog-style WebView contract for displaying web content in a separate top-level window.
/// </summary>
public interface IWebDialog : IWebView
{
    /// <summary>Gets or sets the dialog title.</summary>
    string? Title { get; set; }
    /// <summary>Gets or sets whether the user can resize the dialog.</summary>
    bool CanUserResize { get; set; }

    /// <summary>Shows the dialog without an owner.</summary>
    void Show();
    /// <summary>Shows the dialog with the specified owner.</summary>
    /// <param name="owner">Owner platform handle.</param>
    bool Show(IPlatformHandle owner);
    /// <summary>Closes the dialog.</summary>
    void Close();
    /// <summary>Resizes the dialog.</summary>
    /// <param name="width">New width in device-independent pixels.</param>
    /// <param name="height">New height in device-independent pixels.</param>
    bool Resize(int width, int height);
    /// <summary>Moves the dialog.</summary>
    /// <param name="x">New X position.</param>
    /// <param name="y">New Y position.</param>
    bool Move(int x, int y);

    /// <summary>Raised when the dialog is closing.</summary>
    event EventHandler? Closing;
}

/// <summary>
/// Web authentication broker used for platform-specific auth flows (e.g. OAuth via external browser or embedded WebView).
/// </summary>
public interface IWebAuthBroker
{
    /// <summary>Starts an authentication flow and returns the result.</summary>
    /// <param name="owner">Owner top-level window.</param>
    /// <param name="options">Authentication options.</param>
    Task<WebAuthResult> AuthenticateAsync(ITopLevelWindow owner, AuthOptions options);
}

/// <summary>
/// Dispatcher abstraction used by WebViewCore to marshal work to the UI thread.
/// </summary>
public interface IWebViewDispatcher
{
    /// <summary>Returns true when called from the UI thread.</summary>
    bool CheckAccess();

    /// <summary>Invokes an action on the UI thread.</summary>
    Task InvokeAsync(Action action);
    /// <summary>Invokes a function on the UI thread.</summary>
    Task<T> InvokeAsync<T>(Func<T> func);
    /// <summary>Invokes an async function on the UI thread.</summary>
    Task InvokeAsync(Func<Task> func);
    /// <summary>Invokes an async function on the UI thread.</summary>
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

/// <summary>
/// Envelope for a WebMessage bridge message.
/// </summary>
/// <param name="Body">The raw message body.</param>
/// <param name="Origin">The message origin.</param>
/// <param name="ChannelId">The channel id.</param>
/// <param name="ProtocolVersion">The protocol version.</param>
public readonly record struct WebMessageEnvelope(
    string Body,
    string Origin,
    Guid ChannelId,
    int ProtocolVersion);

/// <summary>
/// Result of evaluating a <see cref="WebMessageEnvelope"/> against an <see cref="IWebMessagePolicy"/>.
/// </summary>
/// <param name="IsAllowed">Whether the message is allowed.</param>
/// <param name="DropReason">Reason for denial, when not allowed.</param>
public readonly record struct WebMessagePolicyDecision(bool IsAllowed, WebMessageDropReason? DropReason)
{
    /// <summary>Creates an allow decision.</summary>
    public static WebMessagePolicyDecision Allow() => new(true, null);

    /// <summary>Creates a deny decision.</summary>
    /// <param name="reason">Drop reason.</param>
    public static WebMessagePolicyDecision Deny(WebMessageDropReason reason) => new(false, reason);
}

/// <summary>
/// Evaluates incoming WebMessage envelopes to determine whether they should be processed or dropped.
/// </summary>
public interface IWebMessagePolicy
{
    /// <summary>Evaluates an envelope and returns the decision.</summary>
    WebMessagePolicyDecision Evaluate(in WebMessageEnvelope envelope);
}

/// <summary>
/// Diagnostic payload describing a dropped WebMessage.
/// </summary>
/// <param name="Reason">Drop reason.</param>
/// <param name="Origin">Message origin.</param>
/// <param name="ChannelId">Message channel id.</param>
public readonly record struct WebMessageDropDiagnostic(WebMessageDropReason Reason, string Origin, Guid ChannelId);

/// <summary>
/// Sink for dropped WebMessage diagnostics.
/// </summary>
public interface IWebMessageDropDiagnosticsSink
{
    /// <summary>Called when a message is dropped.</summary>
    void OnMessageDropped(in WebMessageDropDiagnostic diagnostic);
}

/// <summary>
/// Options applied to the WebView runtime environment (devtools, user agent, storage, custom schemes, preload scripts).
/// </summary>
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

/// <summary>
/// Provides access to the native WebView handle for adapter-specific interop scenarios.
/// </summary>
public interface INativeWebViewHandleProvider
{
    /// <summary>Returns the platform handle when available; otherwise null.</summary>
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
    /// <summary>Gets cookies for the given URI.</summary>
    /// <param name="uri">The URI to retrieve cookies for.</param>
    Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri);
    /// <summary>Adds or updates a cookie.</summary>
    /// <param name="cookie">The cookie to set.</param>
    Task SetCookieAsync(WebViewCookie cookie);
    /// <summary>Deletes a cookie.</summary>
    /// <param name="cookie">The cookie to delete.</param>
    Task DeleteCookieAsync(WebViewCookie cookie);
    /// <summary>Clears all cookies.</summary>
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

/// <summary>Options for <see cref="IWebView.FindInPageAsync"/>.</summary>
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
    Task CopyAsync();
    /// <summary>Cuts the current selection to the clipboard.</summary>
    Task CutAsync();
    /// <summary>Pastes clipboard content at the current position.</summary>
    Task PasteAsync();
    /// <summary>Selects all content in the WebView.</summary>
    Task SelectAllAsync();
    /// <summary>Undoes the last editing action.</summary>
    Task UndoAsync();
    /// <summary>Redoes the last undone editing action.</summary>
    Task RedoAsync();
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

/// <summary>Options for <see cref="IWebAuthBroker.AuthenticateAsync"/>.</summary>
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

/// <summary>Result returned by <see cref="IWebAuthBroker.AuthenticateAsync"/>.</summary>
public sealed class WebAuthResult
{
    /// <summary>Authentication status.</summary>
    public WebAuthStatus Status { get; init; }
    /// <summary>Callback URI when available (e.g. redirect URI).</summary>
    public Uri? CallbackUri { get; init; }
    /// <summary>Optional error string when <see cref="Status"/> is <see cref="WebAuthStatus.Error"/>.</summary>
    public string? Error { get; init; }
}

/// <summary>Event args raised when a navigation starts.</summary>
public sealed class NavigationStartingEventArgs : EventArgs
{
    /// <summary>Creates new args with an empty navigation id.</summary>
    /// <param name="requestUri">The requested URI.</param>
    public NavigationStartingEventArgs(Uri requestUri)
    {
        NavigationId = Guid.Empty;
        RequestUri = requestUri;
    }

    /// <summary>Creates new args with the specified navigation id.</summary>
    /// <param name="navigationId">Navigation correlation id.</param>
    /// <param name="requestUri">The requested URI.</param>
    public NavigationStartingEventArgs(Guid navigationId, Uri requestUri)
    {
        NavigationId = navigationId;
        RequestUri = requestUri;
    }

    /// <summary>Navigation correlation id (may be empty when not available).</summary>
    public Guid NavigationId { get; }
    /// <summary>The requested URI.</summary>
    public Uri RequestUri { get; }
    /// <summary>Set to true to cancel the navigation.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Event args raised when a navigation completes.</summary>
public sealed class NavigationCompletedEventArgs : EventArgs
{
    /// <summary>Creates default event args.</summary>
    public NavigationCompletedEventArgs()
    {
        NavigationId = Guid.Empty;
        RequestUri = new Uri("about:blank");
        Status = NavigationCompletedStatus.Success;
        Error = null;
    }

    /// <summary>Creates event args for a completed navigation.</summary>
    /// <param name="navigationId">Navigation correlation id.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="status">Completion status.</param>
    /// <param name="error">Failure exception when <paramref name="status"/> is <see cref="NavigationCompletedStatus.Failure"/>.</param>
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

    /// <summary>Navigation correlation id.</summary>
    public Guid NavigationId { get; }
    /// <summary>The requested URI.</summary>
    public Uri RequestUri { get; }
    /// <summary>Completion status.</summary>
    public NavigationCompletedStatus Status { get; }
    /// <summary>Failure exception when <see cref="Status"/> is <see cref="NavigationCompletedStatus.Failure"/>.</summary>
    public Exception? Error { get; }
}

/// <summary>Event args raised when content requests opening a new window.</summary>
public sealed class NewWindowRequestedEventArgs : EventArgs
{
    /// <summary>Creates new event args.</summary>
    /// <param name="uri">The requested URI.</param>
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

/// <summary>Event args raised when a WebMessage is received from the page.</summary>
public sealed class WebMessageReceivedEventArgs : EventArgs
{
    /// <summary>Creates a default WebMessage event args.</summary>
    public WebMessageReceivedEventArgs()
    {
        Body = string.Empty;
        Origin = string.Empty;
        ChannelId = Guid.Empty;
        ProtocolVersion = 1;
    }

    /// <summary>Creates event args for a WebMessage.</summary>
    /// <param name="body">Message body.</param>
    /// <param name="origin">Message origin.</param>
    /// <param name="channelId">Message channel id.</param>
    public WebMessageReceivedEventArgs(string body, string origin, Guid channelId)
    {
        Body = body;
        Origin = origin;
        ChannelId = channelId;
        ProtocolVersion = 1;
    }

    /// <summary>Creates event args for a WebMessage.</summary>
    /// <param name="body">Message body.</param>
    /// <param name="origin">Message origin.</param>
    /// <param name="channelId">Message channel id.</param>
    /// <param name="protocolVersion">Protocol version.</param>
    public WebMessageReceivedEventArgs(string body, string origin, Guid channelId, int protocolVersion)
    {
        Body = body;
        Origin = origin;
        ChannelId = channelId;
        ProtocolVersion = protocolVersion;
    }

    /// <summary>Message body.</summary>
    public string Body { get; }
    /// <summary>Message origin.</summary>
    public string Origin { get; }
    /// <summary>Message channel id.</summary>
    public Guid ChannelId { get; }
    /// <summary>Message protocol version.</summary>
    public int ProtocolVersion { get; }
}

/// <summary>
/// Raised when a registered custom-scheme request is intercepted.
/// The handler can supply a response body, content type, and status code.
/// Custom schemes must be registered via <see cref="IWebViewEnvironmentOptions.CustomSchemes"/>
/// before the WebView is created.
/// </summary>
public sealed class WebResourceRequestedEventArgs : EventArgs
{
    /// <summary>Creates a new instance.</summary>
    public WebResourceRequestedEventArgs() { }

    /// <summary>Creates a new instance.</summary>
    /// <param name="requestUri">Intercepted request URI.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="requestHeaders">Optional request headers.</param>
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
    /// <summary>Creates a new instance.</summary>
    /// <param name="downloadUri">The download URI.</param>
    /// <param name="suggestedFileName">Suggested file name.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="contentLength">Content length in bytes.</param>
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
    /// <summary>Unknown permission kind.</summary>
    Unknown = 0,
    /// <summary>Camera access.</summary>
    Camera,
    /// <summary>Microphone access.</summary>
    Microphone,
    /// <summary>Geolocation access.</summary>
    Geolocation,
    /// <summary>Notifications.</summary>
    Notifications,
    /// <summary>Clipboard read.</summary>
    ClipboardRead,
    /// <summary>Clipboard write.</summary>
    ClipboardWrite,
    /// <summary>MIDI access.</summary>
    Midi,
    /// <summary>Sensors access.</summary>
    Sensors,
    /// <summary>Other / platform-specific permission.</summary>
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
    /// <summary>Creates a new instance.</summary>
    /// <param name="permissionKind">The permission kind being requested.</param>
    /// <param name="origin">The origin requesting the permission.</param>
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
    /// <summary>Creates a new instance.</summary>
    /// <param name="platformHandle">The typed native platform handle, when available.</param>
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

/// <summary>Base exception for navigation failures with correlation metadata.</summary>
public class WebViewNavigationException : Exception
{
    /// <summary>Creates a new navigation exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="navigationId">The navigation correlation id.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public WebViewNavigationException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, innerException)
    {
        NavigationId = navigationId;
        RequestUri = requestUri;
    }

    /// <summary>Navigation correlation id.</summary>
    public Guid NavigationId { get; }
    /// <summary>The request URI.</summary>
    public Uri RequestUri { get; }
}

/// <summary>Navigation failed due to a network connectivity issue (DNS, unreachable host, connection lost, no internet).</summary>
public class WebViewNetworkException : WebViewNavigationException
{
    /// <summary>Creates a new instance.</summary>
    public WebViewNetworkException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, navigationId, requestUri, innerException)
    {
    }
}

/// <summary>Navigation failed due to a TLS/certificate issue.</summary>
public class WebViewSslException : WebViewNavigationException
{
    /// <summary>Creates a new instance.</summary>
    public WebViewSslException(string message, Guid navigationId, Uri requestUri, Exception? innerException = null)
        : base(message, navigationId, requestUri, innerException)
    {
    }
}

/// <summary>Navigation failed due to a request timeout.</summary>
public class WebViewTimeoutException : WebViewNavigationException
{
    /// <summary>
    /// Initializes a new instance of <see cref="WebViewTimeoutException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="navigationId">The navigation id associated with the timed out request.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="innerException">The optional inner exception.</param>
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

/// <summary>Exception thrown when script execution fails.</summary>
public class WebViewScriptException : Exception
{
    /// <summary>Creates a new instance.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Optional inner exception.</param>
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
    /// <summary>Creates a new instance.</summary>
    /// <param name="code">JSON-RPC error code.</param>
    /// <param name="message">Error message.</param>
    public WebViewRpcException(int code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>JSON-RPC error code.</summary>
    public int Code { get; }
}
