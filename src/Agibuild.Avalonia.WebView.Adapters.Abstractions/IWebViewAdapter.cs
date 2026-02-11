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

/// <summary>
/// Optional interface for adapters that support custom URI scheme registration.
/// Runtime checks for this via <c>adapter as ICustomSchemeAdapter</c>.
/// <see cref="RegisterCustomSchemes"/> is called before <see cref="IWebViewAdapter.Attach"/>.
/// </summary>
internal interface ICustomSchemeAdapter
{
    void RegisterCustomSchemes(IReadOnlyList<CustomSchemeRegistration> schemes);
}

/// <summary>
/// Optional interface for adapters that support download interception.
/// Runtime checks for this via <c>adapter as IDownloadAdapter</c>.
/// </summary>
internal interface IDownloadAdapter
{
    event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;
}

/// <summary>
/// Optional interface for adapters that support permission request interception.
/// Runtime checks for this via <c>adapter as IPermissionAdapter</c>.
/// </summary>
internal interface IPermissionAdapter
{
    event EventHandler<PermissionRequestedEventArgs>? PermissionRequested;
}

/// <summary>
/// Optional interface for adapters that support editing commands (copy, paste, etc.).
/// Runtime checks for this via <c>adapter as ICommandAdapter</c>.
/// </summary>
internal interface ICommandAdapter
{
    /// <summary>Executes a standard editing command on the WebView.</summary>
    void ExecuteCommand(WebViewCommand command);
}

/// <summary>
/// Optional interface for adapters that support screenshot capture.
/// Runtime checks for this via <c>adapter as IScreenshotAdapter</c>.
/// </summary>
internal interface IScreenshotAdapter
{
    /// <summary>Captures the current viewport as a PNG byte array.</summary>
    Task<byte[]> CaptureScreenshotAsync();
}

/// <summary>
/// Optional interface for adapters that support PDF printing.
/// Runtime checks for this via <c>adapter as IPrintAdapter</c>.
/// </summary>
internal interface IPrintAdapter
{
    /// <summary>Prints the current page to a PDF byte array.</summary>
    Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options);
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
