using System;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Shell policy execution domains.
/// </summary>
public enum WebViewShellPolicyDomain
{
    NewWindow = 0,
    Download = 1,
    Permission = 2,
    Session = 3
}

/// <summary>
/// Session scope used by shell policy.
/// </summary>
public enum WebViewShellSessionScope
{
    Shared = 0,
    Isolated = 1
}

/// <summary>
/// Input context for resolving shell session policy.
/// </summary>
public sealed record WebViewShellSessionContext(string ScopeIdentity = "default");

/// <summary>
/// Session resolution result produced by <see cref="IWebViewShellSessionPolicy"/>.
/// </summary>
public sealed record WebViewShellSessionDecision(WebViewShellSessionScope Scope, string ScopeIdentity);

/// <summary>
/// Error payload raised when a shell policy handler fails.
/// </summary>
public sealed class WebViewShellPolicyErrorEventArgs : EventArgs
{
    /// <summary>
    /// Creates policy error args.
    /// </summary>
    public WebViewShellPolicyErrorEventArgs(WebViewShellPolicyDomain domain, Exception exception)
    {
        Domain = domain;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    /// Failed policy domain.
    /// </summary>
    public WebViewShellPolicyDomain Domain { get; }

    /// <summary>
    /// Exception thrown by policy code.
    /// </summary>
    public Exception Exception { get; }
}

/// <summary>
/// Options for <see cref="WebViewShellExperience"/>. All features are opt-in.
/// </summary>
public sealed class WebViewShellExperienceOptions
{
    /// <summary>Optional policy for handling <see cref="IWebView.NewWindowRequested"/>.</summary>
    public IWebViewNewWindowPolicy? NewWindowPolicy { get; init; }
    /// <summary>Optional policy object for download governance.</summary>
    public IWebViewDownloadPolicy? DownloadPolicy { get; init; }
    /// <summary>Optional policy object for permission governance.</summary>
    public IWebViewPermissionPolicy? PermissionPolicy { get; init; }
    /// <summary>Optional policy object for session scope resolution.</summary>
    public IWebViewShellSessionPolicy? SessionPolicy { get; init; }
    /// <summary>Optional context used to resolve <see cref="SessionPolicy"/>.</summary>
    public WebViewShellSessionContext SessionContext { get; init; } = new();
    /// <summary>Optional handler for download requests.</summary>
    public Action<IWebView, DownloadRequestedEventArgs>? DownloadHandler { get; init; }
    /// <summary>Optional handler for permission requests.</summary>
    public Action<IWebView, PermissionRequestedEventArgs>? PermissionHandler { get; init; }
    /// <summary>
    /// Optional callback for shell policy failures.
    /// </summary>
    public Action<IWebView, WebViewShellPolicyErrorEventArgs>? PolicyErrorHandler { get; init; }
}

/// <summary>
/// Policy for handling <see cref="IWebView.NewWindowRequested"/> in a host-controlled way.
/// </summary>
public interface IWebViewNewWindowPolicy
{
    /// <summary>Handles the new-window request.</summary>
    void Handle(IWebView webView, NewWindowRequestedEventArgs e);
}

/// <summary>
/// Policy for handling <see cref="IWebView.DownloadRequested"/>.
/// </summary>
public interface IWebViewDownloadPolicy
{
    /// <summary>Handles the download request.</summary>
    void Handle(IWebView webView, DownloadRequestedEventArgs e);
}

/// <summary>
/// Policy for handling <see cref="IWebView.PermissionRequested"/>.
/// </summary>
public interface IWebViewPermissionPolicy
{
    /// <summary>Handles the permission request.</summary>
    void Handle(IWebView webView, PermissionRequestedEventArgs e);
}

/// <summary>
/// Policy for resolving shell session scope.
/// </summary>
public interface IWebViewShellSessionPolicy
{
    /// <summary>Resolves a session decision from shell context.</summary>
    WebViewShellSessionDecision Resolve(WebViewShellSessionContext context);
}

/// <summary>
/// New-window policy that preserves the v1 fallback behavior (navigate in-place when unhandled).
/// </summary>
public sealed class NavigateInPlaceNewWindowPolicy : IWebViewNewWindowPolicy
{
    public void Handle(IWebView webView, NewWindowRequestedEventArgs e)
    {
        // Intentionally rely on v1 contract fallback: when Handled == false, WebView navigates in-place.
        // This avoids async navigation work inside an event handler.
        e.Handled = false;
    }
}

/// <summary>
/// New-window policy that delegates handling to a host-provided callback.
/// </summary>
public sealed class DelegateNewWindowPolicy : IWebViewNewWindowPolicy
{
    private readonly Action<IWebView, NewWindowRequestedEventArgs> _handler;

    /// <summary>Creates a delegating policy.</summary>
    public DelegateNewWindowPolicy(Action<IWebView, NewWindowRequestedEventArgs> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Handle(IWebView webView, NewWindowRequestedEventArgs e)
        => _handler(webView, e);
}

/// <summary>
/// Download policy that delegates handling to a host callback.
/// </summary>
public sealed class DelegateDownloadPolicy : IWebViewDownloadPolicy
{
    private readonly Action<IWebView, DownloadRequestedEventArgs> _handler;

    /// <summary>Creates a delegating policy.</summary>
    public DelegateDownloadPolicy(Action<IWebView, DownloadRequestedEventArgs> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Handle(IWebView webView, DownloadRequestedEventArgs e)
        => _handler(webView, e);
}

/// <summary>
/// Permission policy that delegates handling to a host callback.
/// </summary>
public sealed class DelegatePermissionPolicy : IWebViewPermissionPolicy
{
    private readonly Action<IWebView, PermissionRequestedEventArgs> _handler;

    /// <summary>Creates a delegating policy.</summary>
    public DelegatePermissionPolicy(Action<IWebView, PermissionRequestedEventArgs> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Handle(IWebView webView, PermissionRequestedEventArgs e)
        => _handler(webView, e);
}

/// <summary>
/// Session policy that always resolves to a shared scope.
/// </summary>
public sealed class SharedSessionPolicy : IWebViewShellSessionPolicy
{
    public WebViewShellSessionDecision Resolve(WebViewShellSessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WebViewShellSessionDecision(
            WebViewShellSessionScope.Shared,
            NormalizeScopeIdentity(context.ScopeIdentity, "shared"));
    }

    private static string NormalizeScopeIdentity(string? identity, string fallback)
        => string.IsNullOrWhiteSpace(identity) ? fallback : identity.Trim();
}

/// <summary>
/// Session policy that always resolves to an isolated scope.
/// </summary>
public sealed class IsolatedSessionPolicy : IWebViewShellSessionPolicy
{
    public WebViewShellSessionDecision Resolve(WebViewShellSessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var normalized = string.IsNullOrWhiteSpace(context.ScopeIdentity)
            ? "default"
            : context.ScopeIdentity.Trim();
        return new WebViewShellSessionDecision(
            WebViewShellSessionScope.Isolated,
            $"isolated:{normalized}");
    }
}

/// <summary>
/// Session policy that delegates resolution to a host callback.
/// </summary>
public sealed class DelegateSessionPolicy : IWebViewShellSessionPolicy
{
    private readonly Func<WebViewShellSessionContext, WebViewShellSessionDecision> _resolver;

    /// <summary>Creates a delegating policy.</summary>
    public DelegateSessionPolicy(Func<WebViewShellSessionContext, WebViewShellSessionDecision> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public WebViewShellSessionDecision Resolve(WebViewShellSessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _resolver(context);
    }
}

/// <summary>
/// Opt-in runtime helper that wires common host policies (new window, downloads, permissions)
/// onto an <see cref="IWebView"/> instance.
/// </summary>
public sealed class WebViewShellExperience : IDisposable
{
    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private readonly WebViewShellSessionDecision? _sessionDecision;
    private bool _disposed;

    /// <summary>Creates a new shell experience instance for the given WebView.</summary>
    public WebViewShellExperience(IWebView webView, WebViewShellExperienceOptions? options = null)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? new WebViewShellExperienceOptions();

        if (_options.NewWindowPolicy is not null)
            _webView.NewWindowRequested += OnNewWindowRequested;
        if (_options.DownloadPolicy is not null || _options.DownloadHandler is not null)
            _webView.DownloadRequested += OnDownloadRequested;
        if (_options.PermissionPolicy is not null || _options.PermissionHandler is not null)
            _webView.PermissionRequested += OnPermissionRequested;

        if (_options.SessionPolicy is not null)
        {
            _sessionDecision = ExecutePolicyDomain(
                WebViewShellPolicyDomain.Session,
                () => _options.SessionPolicy.Resolve(_options.SessionContext));
        }
    }

    /// <summary>
    /// Raised when policy execution fails in any shell domain.
    /// </summary>
    public event EventHandler<WebViewShellPolicyErrorEventArgs>? PolicyError;

    /// <summary>
    /// Gets the session decision resolved at construction time when <see cref="WebViewShellExperienceOptions.SessionPolicy"/> is configured.
    /// </summary>
    public WebViewShellSessionDecision? SessionDecision => _sessionDecision;

    private void OnNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        if (_disposed) return;
        ExecutePolicyDomain(
            WebViewShellPolicyDomain.NewWindow,
            () => _options.NewWindowPolicy?.Handle(_webView, e));
    }

    private void OnDownloadRequested(object? sender, DownloadRequestedEventArgs e)
    {
        if (_disposed) return;

        // Deterministic execution order:
        // 1) policy object
        // 2) delegate handler
        ExecutePolicyDomain(
            WebViewShellPolicyDomain.Download,
            () => _options.DownloadPolicy?.Handle(_webView, e));
        ExecutePolicyDomain(
            WebViewShellPolicyDomain.Download,
            () => _options.DownloadHandler?.Invoke(_webView, e));
    }

    private void OnPermissionRequested(object? sender, PermissionRequestedEventArgs e)
    {
        if (_disposed) return;

        // Deterministic execution order:
        // 1) policy object
        // 2) delegate handler
        ExecutePolicyDomain(
            WebViewShellPolicyDomain.Permission,
            () => _options.PermissionPolicy?.Handle(_webView, e));
        ExecutePolicyDomain(
            WebViewShellPolicyDomain.Permission,
            () => _options.PermissionHandler?.Invoke(_webView, e));
    }

    private void ExecutePolicyDomain(WebViewShellPolicyDomain domain, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
        }
        catch (Exception ex)
        {
            ReportPolicyFailure(domain, ex);
        }
    }

    private T? ExecutePolicyDomain<T>(WebViewShellPolicyDomain domain, Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return action();
        }
        catch (Exception ex)
        {
            ReportPolicyFailure(domain, ex);
            return default;
        }
    }

    private void ReportPolicyFailure(WebViewShellPolicyDomain domain, Exception ex)
    {
        if (!WebViewOperationFailure.TryGetCategory(ex, out _))
            WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);

        var errorArgs = new WebViewShellPolicyErrorEventArgs(domain, ex);
        PolicyError?.Invoke(this, errorArgs);

        try
        {
            _options.PolicyErrorHandler?.Invoke(_webView, errorArgs);
        }
        catch
        {
            // Policy error reporting is best-effort and must not crash event flow.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _webView.NewWindowRequested -= OnNewWindowRequested;
        _webView.DownloadRequested -= OnDownloadRequested;
        _webView.PermissionRequested -= OnPermissionRequested;
    }
}

