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
    bool EnableDevTools { get; set; }
}

public interface INativeWebViewHandleProvider
{
    IPlatformHandle? TryGetWebViewHandle();
}

/// <summary>Placeholder — cookie management is not yet implemented.</summary>
[Experimental("AGWV001")]
public interface ICookieManager
{
}

/// <summary>Placeholder — command management is not yet implemented.</summary>
[Experimental("AGWV002")]
public interface ICommandManager
{
}

/// <summary>Placeholder — top-level window abstraction is not yet implemented.</summary>
[Experimental("AGWV003")]
public interface ITopLevelWindow
{
}

public sealed class AuthOptions
{
    public Uri? CallbackUri { get; set; }
    public bool UseEphemeralSession { get; set; } = true;
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

/// <summary>Placeholder — web resource request interception is not yet implemented.</summary>
[Experimental("AGWV004")]
public sealed class WebResourceRequestedEventArgs : EventArgs
{
}

/// <summary>Placeholder — environment requested event is not yet implemented.</summary>
[Experimental("AGWV005")]
public sealed class EnvironmentRequestedEventArgs : EventArgs
{
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

public class WebViewScriptException : Exception
{
    public WebViewScriptException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
