using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Shell policy execution domains.
/// </summary>
public enum WebViewShellPolicyDomain
{
    /// <summary>Policy domain for new-window handling.</summary>
    NewWindow = 0,
    /// <summary>Policy domain for download handling.</summary>
    Download = 1,
    /// <summary>Policy domain for permission handling.</summary>
    Permission = 2,
    /// <summary>Policy domain for session resolution.</summary>
    Session = 3,
    /// <summary>Policy domain for managed window lifecycle.</summary>
    ManagedWindowLifecycle = 4,
    /// <summary>Policy domain for external URL open actions.</summary>
    ExternalOpen = 5,
    /// <summary>Policy domain for DevTools actions.</summary>
    DevTools = 6,
    /// <summary>Policy domain for shell command actions.</summary>
    Command = 7
}

/// <summary>
/// Strategy result for handling a new-window request.
/// </summary>
public enum WebViewNewWindowStrategy
{
    /// <summary>Navigate inside current WebView.</summary>
    InPlace = 0,
    /// <summary>Create and route to a managed child window.</summary>
    ManagedWindow = 1,
    /// <summary>Open in external browser.</summary>
    ExternalBrowser = 2,
    /// <summary>Delegate decision to host callback.</summary>
    Delegate = 3
}

/// <summary>
/// Lifecycle states for runtime-managed windows.
/// </summary>
public enum WebViewManagedWindowLifecycleState
{
    /// <summary>Window entry created but not attached.</summary>
    Created = 0,
    /// <summary>Window attached and active.</summary>
    Attached = 1,
    /// <summary>Window ready for interaction.</summary>
    Ready = 2,
    /// <summary>Window closing in progress.</summary>
    Closing = 3,
    /// <summary>Window fully closed.</summary>
    Closed = 4
}

/// <summary>
/// DevTools operation kinds governed by shell policy.
/// </summary>
public enum WebViewShellDevToolsAction
{
    /// <summary>Open DevTools.</summary>
    Open = 0,
    /// <summary>Close DevTools.</summary>
    Close = 1,
    /// <summary>Query DevTools state.</summary>
    Query = 2
}

/// <summary>
/// Context for DevTools policy evaluation.
/// </summary>
public sealed record WebViewShellDevToolsPolicyContext(
    Guid RootWindowId,
    Guid? TargetWindowId,
    WebViewShellDevToolsAction Action);

/// <summary>
/// Decision returned by <see cref="IWebViewShellDevToolsPolicy"/>.
/// </summary>
public sealed record WebViewShellDevToolsDecision(bool IsAllowed, string? DenyReason = null)
{
    /// <summary>Create allow decision.</summary>
    public static WebViewShellDevToolsDecision Allow()
        => new(true);

    /// <summary>Create deny decision.</summary>
    public static WebViewShellDevToolsDecision Deny(string? reason = null)
        => new(false, reason);
}

/// <summary>
/// Context for shell command policy evaluation.
/// </summary>
public sealed record WebViewShellCommandPolicyContext(
    Guid RootWindowId,
    Guid? TargetWindowId,
    WebViewCommand Command);

/// <summary>
/// Decision returned by <see cref="IWebViewShellCommandPolicy"/>.
/// </summary>
public sealed record WebViewShellCommandDecision(bool IsAllowed, string? DenyReason = null)
{
    /// <summary>Create allow decision.</summary>
    public static WebViewShellCommandDecision Allow()
        => new(true);

    /// <summary>Create deny decision.</summary>
    public static WebViewShellCommandDecision Deny(string? reason = null)
        => new(false, reason);
}

/// <summary>
/// Context for new-window policy evaluation.
/// </summary>
/// <param name="SourceWindowId">The source (parent) window id.</param>
/// <param name="CandidateWindowId">Candidate id for a potential managed child window.</param>
/// <param name="TargetUri">Target URI from new-window request.</param>
/// <param name="ScopeIdentity">Current shell scope identity.</param>
public sealed record WebViewNewWindowPolicyContext(
    Guid SourceWindowId,
    Guid CandidateWindowId,
    Uri? TargetUri,
    string ScopeIdentity);

/// <summary>
/// Strategy decision returned by <see cref="IWebViewNewWindowPolicy"/>.
/// </summary>
public sealed record WebViewNewWindowStrategyDecision(
    WebViewNewWindowStrategy Strategy,
    bool Handled = true,
    string? ScopeIdentityOverride = null)
{
    /// <summary>Create in-place strategy decision (fallback navigation).</summary>
    public static WebViewNewWindowStrategyDecision InPlace()
        => new(WebViewNewWindowStrategy.InPlace, Handled: false);

    /// <summary>Create managed-window strategy decision.</summary>
    public static WebViewNewWindowStrategyDecision ManagedWindow(string? scopeIdentityOverride = null)
        => new(WebViewNewWindowStrategy.ManagedWindow, Handled: true, ScopeIdentityOverride: scopeIdentityOverride);

    /// <summary>Create external-browser strategy decision.</summary>
    public static WebViewNewWindowStrategyDecision ExternalBrowser()
        => new(WebViewNewWindowStrategy.ExternalBrowser, Handled: true);

    /// <summary>Create delegate strategy decision.</summary>
    public static WebViewNewWindowStrategyDecision Delegate(bool handled)
        => new(WebViewNewWindowStrategy.Delegate, Handled: handled);
}

/// <summary>
/// Session scope used by shell policy.
/// </summary>
public enum WebViewShellSessionScope
{
    /// <summary>Share session state across windows in same scope.</summary>
    Shared = 0,
    /// <summary>Use isolated session state.</summary>
    Isolated = 1
}

/// <summary>
/// Input context for resolving shell session policy.
/// </summary>
public sealed record WebViewShellSessionContext(string ScopeIdentity = "default")
{
    /// <summary>Current window identity for the session decision.</summary>
    public Guid? WindowId { get; init; }
    /// <summary>Optional parent window identity.</summary>
    public Guid? ParentWindowId { get; init; }
    /// <summary>Optional request URI associated with the decision.</summary>
    public Uri? RequestUri { get; init; }
}

/// <summary>
/// Session resolution result produced by <see cref="IWebViewShellSessionPolicy"/>.
/// </summary>
public sealed record WebViewShellSessionDecision(WebViewShellSessionScope Scope, string ScopeIdentity);

/// <summary>
/// Input payload for creating a managed shell window.
/// </summary>
public sealed class WebViewManagedWindowCreateContext
{
    /// <summary>Create managed window context.</summary>
    public WebViewManagedWindowCreateContext(
        Guid windowId,
        Guid parentWindowId,
        Uri? targetUri,
        string scopeIdentity,
        WebViewShellSessionDecision? sessionDecision,
        string? profileIdentity)
    {
        WindowId = windowId;
        ParentWindowId = parentWindowId;
        TargetUri = targetUri;
        ScopeIdentity = scopeIdentity;
        SessionDecision = sessionDecision;
        ProfileIdentity = profileIdentity;
    }

    /// <summary>Managed child window id.</summary>
    public Guid WindowId { get; }
    /// <summary>Parent window id that produced this child.</summary>
    public Guid ParentWindowId { get; }
    /// <summary>Target URI from the new-window request.</summary>
    public Uri? TargetUri { get; }
    /// <summary>Resolved scope identity for child window creation.</summary>
    public string ScopeIdentity { get; }
    /// <summary>Resolved session decision for the child window.</summary>
    public WebViewShellSessionDecision? SessionDecision { get; }
    /// <summary>Resolved profile identity for this managed window.</summary>
    public string? ProfileIdentity { get; }
}

/// <summary>
/// Lifecycle event payload for managed windows.
/// </summary>
public sealed class WebViewManagedWindowLifecycleEventArgs : EventArgs
{
    /// <summary>Create lifecycle event args.</summary>
    public WebViewManagedWindowLifecycleEventArgs(
        Guid windowId,
        Guid parentWindowId,
        WebViewManagedWindowLifecycleState state,
        WebViewShellSessionDecision? sessionDecision,
        string? profileIdentity)
    {
        WindowId = windowId;
        ParentWindowId = parentWindowId;
        State = state;
        SessionDecision = sessionDecision;
        ProfileIdentity = profileIdentity;
    }

    /// <summary>Managed window identity.</summary>
    public Guid WindowId { get; }
    /// <summary>Parent window identity.</summary>
    public Guid ParentWindowId { get; }
    /// <summary>Current lifecycle state.</summary>
    public WebViewManagedWindowLifecycleState State { get; }
    /// <summary>Session decision correlated to this window.</summary>
    public WebViewShellSessionDecision? SessionDecision { get; }
    /// <summary>Resolved profile identity correlated to this window.</summary>
    public string? ProfileIdentity { get; }
}

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
    /// <summary>Optional typed host capability bridge.</summary>
    public WebViewHostCapabilityBridge? HostCapabilityBridge { get; init; }
    /// <summary>Optional factory that creates a managed child window when strategy is <c>ManagedWindow</c>.</summary>
    public Func<WebViewManagedWindowCreateContext, IWebView?>? ManagedWindowFactory { get; init; }
    /// <summary>Optional external-open handler for <c>ExternalBrowser</c> strategy.</summary>
    public Action<IWebView, Uri>? ExternalOpenHandler { get; init; }
    /// <summary>Optional managed-window close handler used by lifecycle orchestrator.</summary>
    public Func<IWebView, CancellationToken, Task>? ManagedWindowCloseAsync { get; init; }
    /// <summary>Timeout budget for managed-window close operations.</summary>
    public TimeSpan ManagedWindowCloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
    /// <summary>Optional policy object for download governance.</summary>
    public IWebViewDownloadPolicy? DownloadPolicy { get; init; }
    /// <summary>Optional policy object for permission governance.</summary>
    public IWebViewPermissionPolicy? PermissionPolicy { get; init; }
    /// <summary>Optional policy object for DevTools governance.</summary>
    public IWebViewShellDevToolsPolicy? DevToolsPolicy { get; init; }
    /// <summary>Optional policy object for command/shortcut governance.</summary>
    public IWebViewShellCommandPolicy? CommandPolicy { get; init; }
    /// <summary>Optional policy object for session scope resolution.</summary>
    public IWebViewShellSessionPolicy? SessionPolicy { get; init; }
    /// <summary>Optional resolver for session-permission profiles.</summary>
    public IWebViewSessionPermissionProfileResolver? SessionPermissionProfileResolver { get; init; }
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
    /// <summary>Resolves a strategy decision for the new-window request.</summary>
    WebViewNewWindowStrategyDecision Decide(IWebView webView, NewWindowRequestedEventArgs e, WebViewNewWindowPolicyContext context);
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
/// Policy for governing DevTools operations in shell experience.
/// </summary>
public interface IWebViewShellDevToolsPolicy
{
    /// <summary>Resolves allow/deny decision for a DevTools action.</summary>
    WebViewShellDevToolsDecision Decide(IWebView webView, WebViewShellDevToolsPolicyContext context);
}

/// <summary>
/// Policy for governing command execution in shell experience.
/// </summary>
public interface IWebViewShellCommandPolicy
{
    /// <summary>Resolves allow/deny decision for a command action.</summary>
    WebViewShellCommandDecision Decide(IWebView webView, WebViewShellCommandPolicyContext context);
}

/// <summary>
/// New-window policy that preserves the v1 fallback behavior (navigate in-place when unhandled).
/// </summary>
public sealed class NavigateInPlaceNewWindowPolicy : IWebViewNewWindowPolicy
{
    /// <inheritdoc />
    public WebViewNewWindowStrategyDecision Decide(IWebView webView, NewWindowRequestedEventArgs e, WebViewNewWindowPolicyContext context)
        => WebViewNewWindowStrategyDecision.InPlace();
}

/// <summary>
/// New-window policy that delegates handling to a host-provided callback.
/// </summary>
public sealed class DelegateNewWindowPolicy : IWebViewNewWindowPolicy
{
    private readonly Func<IWebView, NewWindowRequestedEventArgs, WebViewNewWindowPolicyContext, WebViewNewWindowStrategyDecision> _decider;

    /// <summary>Creates a delegating policy with strategy result.</summary>
    public DelegateNewWindowPolicy(
        Func<IWebView, NewWindowRequestedEventArgs, WebViewNewWindowPolicyContext, WebViewNewWindowStrategyDecision> decider)
    {
        _decider = decider ?? throw new ArgumentNullException(nameof(decider));
    }

    /// <summary>Creates a delegating policy from a legacy callback that controls only <see cref="NewWindowRequestedEventArgs.Handled"/>.</summary>
    public DelegateNewWindowPolicy(Action<IWebView, NewWindowRequestedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _decider = (webView, args, _) =>
        {
            handler(webView, args);
            return WebViewNewWindowStrategyDecision.Delegate(args.Handled);
        };
    }

    /// <inheritdoc />
    public WebViewNewWindowStrategyDecision Decide(IWebView webView, NewWindowRequestedEventArgs e, WebViewNewWindowPolicyContext context)
        => _decider(webView, e, context);
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Handle(IWebView webView, PermissionRequestedEventArgs e)
        => _handler(webView, e);
}

/// <summary>
/// Session policy that always resolves to a shared scope.
/// </summary>
public sealed class SharedSessionPolicy : IWebViewShellSessionPolicy
{
    /// <inheritdoc />
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
    /// <inheritdoc />
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

    /// <inheritdoc />
    public WebViewShellSessionDecision Resolve(WebViewShellSessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _resolver(context);
    }
}

/// <summary>
/// DevTools policy that delegates decision logic to host callback.
/// </summary>
public sealed class DelegateDevToolsPolicy : IWebViewShellDevToolsPolicy
{
    private readonly Func<IWebView, WebViewShellDevToolsPolicyContext, WebViewShellDevToolsDecision> _resolver;

    /// <summary>Creates a delegating DevTools policy.</summary>
    public DelegateDevToolsPolicy(Func<IWebView, WebViewShellDevToolsPolicyContext, WebViewShellDevToolsDecision> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public WebViewShellDevToolsDecision Decide(IWebView webView, WebViewShellDevToolsPolicyContext context)
        => _resolver(webView, context);
}

/// <summary>
/// Command policy that delegates decision logic to host callback.
/// </summary>
public sealed class DelegateCommandPolicy : IWebViewShellCommandPolicy
{
    private readonly Func<IWebView, WebViewShellCommandPolicyContext, WebViewShellCommandDecision> _resolver;

    /// <summary>Creates a delegating command policy.</summary>
    public DelegateCommandPolicy(Func<IWebView, WebViewShellCommandPolicyContext, WebViewShellCommandDecision> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public WebViewShellCommandDecision Decide(IWebView webView, WebViewShellCommandPolicyContext context)
        => _resolver(webView, context);
}

/// <summary>
/// Opt-in runtime helper that wires common host policies (new window, downloads, permissions)
/// onto an <see cref="IWebView"/> instance.
/// </summary>
public sealed class WebViewShellExperience : IDisposable
{
    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private readonly object _managedWindowsLock = new();
    private readonly Dictionary<Guid, ManagedWindowEntry> _managedWindows = new();
    private readonly Guid _rootWindowId;
    private readonly WebViewShellSessionDecision? _sessionDecision;
    private readonly WebViewSessionPermissionProfile? _rootProfile;
    private bool _disposed;

    /// <summary>Creates a new shell experience instance for the given WebView.</summary>
    public WebViewShellExperience(IWebView webView, WebViewShellExperienceOptions? options = null)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? new WebViewShellExperienceOptions();
        _rootWindowId = Guid.NewGuid();

        if (_options.NewWindowPolicy is not null)
            _webView.NewWindowRequested += OnNewWindowRequested;
        if (_options.DownloadPolicy is not null || _options.DownloadHandler is not null)
            _webView.DownloadRequested += OnDownloadRequested;
        if (_options.PermissionPolicy is not null ||
            _options.PermissionHandler is not null ||
            _options.SessionPermissionProfileResolver is not null)
            _webView.PermissionRequested += OnPermissionRequested;

        var rootSessionContext = _options.SessionContext with
        {
            WindowId = _rootWindowId,
            ParentWindowId = null
        };

        WebViewShellSessionDecision? resolvedRootSessionDecision = null;
        if (_options.SessionPolicy is not null)
        {
            resolvedRootSessionDecision = ExecutePolicyDomain(
                WebViewShellPolicyDomain.Session,
                () => _options.SessionPolicy.Resolve(rootSessionContext));
        }

        WebViewSessionPermissionProfile? resolvedRootProfile = null;
        if (_options.SessionPermissionProfileResolver is not null)
        {
            var rootProfileContext = new WebViewSessionPermissionProfileContext(
                _rootWindowId,
                ParentWindowId: null,
                WindowId: _rootWindowId,
                ScopeIdentity: rootSessionContext.ScopeIdentity,
                RequestUri: rootSessionContext.RequestUri,
                PermissionKind: null);

            resolvedRootProfile = ExecutePolicyDomain(
                WebViewShellPolicyDomain.Session,
                () => _options.SessionPermissionProfileResolver.Resolve(rootProfileContext, parentProfile: null));

            if (resolvedRootProfile is not null)
            {
                resolvedRootSessionDecision = resolvedRootProfile.ResolveSessionDecision(
                    parentDecision: null,
                    fallbackDecision: resolvedRootSessionDecision,
                    scopeIdentity: rootSessionContext.ScopeIdentity);

                RaiseSessionPermissionProfileDiagnostic(
                    _rootWindowId,
                    parentWindowId: null,
                    scopeIdentity: rootSessionContext.ScopeIdentity,
                    profile: resolvedRootProfile,
                    sessionDecision: resolvedRootSessionDecision,
                    permissionKind: null,
                    permissionDecision: WebViewPermissionProfileDecision.DefaultFallback());
            }
        }

        _sessionDecision = resolvedRootSessionDecision;
        _rootProfile = resolvedRootProfile;
    }

    /// <summary>
    /// Raised when policy execution fails in any shell domain.
    /// </summary>
    public event EventHandler<WebViewShellPolicyErrorEventArgs>? PolicyError;
    /// <summary>
    /// Raised whenever a managed window lifecycle state changes.
    /// </summary>
    public event EventHandler<WebViewManagedWindowLifecycleEventArgs>? ManagedWindowLifecycleChanged;
    /// <summary>
    /// Raised when profile resolution/evaluation completes for session or permission paths.
    /// </summary>
    public event EventHandler<WebViewSessionPermissionProfileDiagnosticEventArgs>? SessionPermissionProfileEvaluated;

    /// <summary>
    /// Gets the session decision resolved at construction time when <see cref="WebViewShellExperienceOptions.SessionPolicy"/> is configured.
    /// </summary>
    public WebViewShellSessionDecision? SessionDecision => _sessionDecision;
    /// <summary>
    /// Gets root profile identity when profile resolver is configured.
    /// </summary>
    public string? RootProfileIdentity => _rootProfile?.ProfileIdentity;

    /// <summary>
    /// Stable identity of the root window associated with this shell experience.
    /// </summary>
    public Guid RootWindowId => _rootWindowId;

    /// <summary>
    /// Number of managed windows currently tracked by runtime.
    /// </summary>
    public int ManagedWindowCount
    {
        get
        {
            lock (_managedWindowsLock)
                return _managedWindows.Count;
        }
    }

    /// <summary>
    /// Returns a snapshot of active managed window ids.
    /// </summary>
    public IReadOnlyList<Guid> GetManagedWindowIds()
    {
        lock (_managedWindowsLock)
            return [.. _managedWindows.Keys];
    }

    /// <summary>
    /// Attempts to get a managed child window by id.
    /// </summary>
    public bool TryGetManagedWindow(Guid windowId, out IWebView? managedWindow)
    {
        lock (_managedWindowsLock)
        {
            if (_managedWindows.TryGetValue(windowId, out var entry))
            {
                managedWindow = entry.Window;
                return true;
            }
        }

        managedWindow = null;
        return false;
    }

    /// <summary>
    /// Reads host clipboard text via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<string?> ReadClipboardText()
    {
        if (_options.HostCapabilityBridge is null)
            return WebViewHostCapabilityCallResult<string?>.Denied("Host capability bridge is not configured.");
        return _options.HostCapabilityBridge.ReadClipboardText(_rootWindowId, parentWindowId: null, targetWindowId: _rootWindowId);
    }

    /// <summary>
    /// Writes host clipboard text via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> WriteClipboardText(string text)
    {
        if (_options.HostCapabilityBridge is null)
            return WebViewHostCapabilityCallResult<object?>.Denied("Host capability bridge is not configured.");
        return _options.HostCapabilityBridge.WriteClipboardText(text, _rootWindowId, parentWindowId: null, targetWindowId: _rootWindowId);
    }

    /// <summary>
    /// Shows host open-file dialog via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
    {
        if (_options.HostCapabilityBridge is null)
            return WebViewHostCapabilityCallResult<WebViewFileDialogResult>.Denied("Host capability bridge is not configured.");
        return _options.HostCapabilityBridge.ShowOpenFileDialog(request, _rootWindowId, parentWindowId: null, targetWindowId: _rootWindowId);
    }

    /// <summary>
    /// Shows host save-file dialog via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<WebViewFileDialogResult> ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
    {
        if (_options.HostCapabilityBridge is null)
            return WebViewHostCapabilityCallResult<WebViewFileDialogResult>.Denied("Host capability bridge is not configured.");
        return _options.HostCapabilityBridge.ShowSaveFileDialog(request, _rootWindowId, parentWindowId: null, targetWindowId: _rootWindowId);
    }

    /// <summary>
    /// Shows host notification via typed capability bridge.
    /// Returns denied result when bridge is not configured.
    /// </summary>
    public WebViewHostCapabilityCallResult<object?> ShowNotification(WebViewNotificationRequest request)
    {
        if (_options.HostCapabilityBridge is null)
            return WebViewHostCapabilityCallResult<object?>.Denied("Host capability bridge is not configured.");
        return _options.HostCapabilityBridge.ShowNotification(request, _rootWindowId, parentWindowId: null, targetWindowId: _rootWindowId);
    }

    /// <summary>
    /// Opens DevTools through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> OpenDevToolsAsync()
    {
        if (_disposed)
            return Task.FromResult(false);

        var decision = EvaluateDevToolsPolicy(WebViewShellDevToolsAction.Open);
        if (decision is null || !decision.IsAllowed)
        {
            ReportDevToolsDenied(decision?.DenyReason, WebViewShellDevToolsAction.Open);
            return Task.FromResult(false);
        }

        return ExecuteDevToolsOperation(() => _webView.OpenDevToolsAsync());
    }

    /// <summary>
    /// Closes DevTools through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> CloseDevToolsAsync()
    {
        if (_disposed)
            return Task.FromResult(false);

        var decision = EvaluateDevToolsPolicy(WebViewShellDevToolsAction.Close);
        if (decision is null || !decision.IsAllowed)
        {
            ReportDevToolsDenied(decision?.DenyReason, WebViewShellDevToolsAction.Close);
            return Task.FromResult(false);
        }

        return ExecuteDevToolsOperation(() => _webView.CloseDevToolsAsync());
    }

    /// <summary>
    /// Queries DevTools open state through shell policy governance.
    /// Returns false when blocked by policy or when execution fails.
    /// </summary>
    public Task<bool> IsDevToolsOpenAsync()
    {
        if (_disposed)
            return Task.FromResult(false);

        var decision = EvaluateDevToolsPolicy(WebViewShellDevToolsAction.Query);
        if (decision is null || !decision.IsAllowed)
        {
            ReportDevToolsDenied(decision?.DenyReason, WebViewShellDevToolsAction.Query);
            return Task.FromResult(false);
        }

        return ExecuteDevToolsQueryOperation(() => _webView.IsDevToolsOpenAsync());
    }

    /// <summary>
    /// Executes a standard command through shell policy governance.
    /// Returns false when blocked, unsupported, or execution fails.
    /// </summary>
    public Task<bool> ExecuteCommandAsync(WebViewCommand command)
    {
        if (_disposed)
            return Task.FromResult(false);

        var decision = EvaluateCommandPolicy(command);
        if (decision is null || !decision.IsAllowed)
        {
            ReportCommandDenied(command, decision?.DenyReason);
            return Task.FromResult(false);
        }

        var commandManager = _webView.TryGetCommandManager();
        if (commandManager is null)
        {
            ReportPolicyFailure(
                WebViewShellPolicyDomain.Command,
                new NotSupportedException("Command manager is not available for this WebView instance."));
            return Task.FromResult(false);
        }

        return ExecuteCommandOperation(commandManager, command);
    }

    private void OnNewWindowRequested(object? sender, NewWindowRequestedEventArgs e)
    {
        if (_disposed) return;

        var candidateWindowId = Guid.NewGuid();
        var policyContext = new WebViewNewWindowPolicyContext(
            SourceWindowId: _rootWindowId,
            CandidateWindowId: candidateWindowId,
            TargetUri: e.Uri,
            ScopeIdentity: _options.SessionContext.ScopeIdentity);

        var decision = ExecutePolicyDomain(
                WebViewShellPolicyDomain.NewWindow,
                () => _options.NewWindowPolicy?.Decide(_webView, e, policyContext)
                      ?? WebViewNewWindowStrategyDecision.InPlace())
            ?? WebViewNewWindowStrategyDecision.InPlace();

        ExecuteStrategyDecision(decision, candidateWindowId, e);
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

        var appliedProfileDecision = TryApplyProfilePermissionDecision(e);
        if (appliedProfileDecision)
            return;

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

    private bool TryApplyProfilePermissionDecision(PermissionRequestedEventArgs e)
    {
        if (_options.SessionPermissionProfileResolver is null)
            return false;

        var scopeIdentity = _sessionDecision?.ScopeIdentity ?? _options.SessionContext.ScopeIdentity;
        var profileContext = new WebViewSessionPermissionProfileContext(
            _rootWindowId,
            ParentWindowId: null,
            WindowId: _rootWindowId,
            ScopeIdentity: scopeIdentity,
            RequestUri: e.Origin,
            PermissionKind: e.PermissionKind);

        var resolvedProfile = ExecutePolicyDomain(
            WebViewShellPolicyDomain.Permission,
            () => _options.SessionPermissionProfileResolver.Resolve(profileContext, _rootProfile));

        if (resolvedProfile is null)
            return false;

        var effectiveSessionDecision = resolvedProfile.ResolveSessionDecision(
            parentDecision: null,
            fallbackDecision: _sessionDecision,
            scopeIdentity: scopeIdentity);
        var profileDecision = resolvedProfile.ResolvePermissionDecision(e.PermissionKind);

        RaiseSessionPermissionProfileDiagnostic(
            windowId: _rootWindowId,
            parentWindowId: null,
            scopeIdentity: scopeIdentity,
            profile: resolvedProfile,
            sessionDecision: effectiveSessionDecision,
            permissionKind: e.PermissionKind,
            permissionDecision: profileDecision);

        if (!profileDecision.IsExplicit || profileDecision.State == PermissionState.Default)
            return false;

        e.State = profileDecision.State;
        return true;
    }

    private WebViewShellDevToolsDecision? EvaluateDevToolsPolicy(WebViewShellDevToolsAction action)
    {
        if (_options.DevToolsPolicy is null)
            return WebViewShellDevToolsDecision.Allow();

        var context = new WebViewShellDevToolsPolicyContext(
            RootWindowId: _rootWindowId,
            TargetWindowId: _rootWindowId,
            Action: action);

        return ExecutePolicyDomain(
            WebViewShellPolicyDomain.DevTools,
            () => _options.DevToolsPolicy.Decide(_webView, context));
    }

    private WebViewShellCommandDecision? EvaluateCommandPolicy(WebViewCommand command)
    {
        if (_options.CommandPolicy is null)
            return WebViewShellCommandDecision.Allow();

        var context = new WebViewShellCommandPolicyContext(
            RootWindowId: _rootWindowId,
            TargetWindowId: _rootWindowId,
            Command: command);

        return ExecutePolicyDomain(
            WebViewShellPolicyDomain.Command,
            () => _options.CommandPolicy.Decide(_webView, context));
    }

    private async Task<bool> ExecuteDevToolsOperation(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            ReportPolicyFailure(WebViewShellPolicyDomain.DevTools, ex);
            return false;
        }
    }

    private async Task<bool> ExecuteDevToolsQueryOperation(Func<Task<bool>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReportPolicyFailure(WebViewShellPolicyDomain.DevTools, ex);
            return false;
        }
    }

    private void ReportDevToolsDenied(string? denyReason, WebViewShellDevToolsAction action)
    {
        ReportPolicyFailure(
            WebViewShellPolicyDomain.DevTools,
            new UnauthorizedAccessException(
                denyReason ?? $"DevTools action '{action}' was denied by shell policy."));
    }

    private async Task<bool> ExecuteCommandOperation(ICommandManager commandManager, WebViewCommand command)
    {
        try
        {
            switch (command)
            {
                case WebViewCommand.Copy:
                    await commandManager.CopyAsync().ConfigureAwait(false);
                    break;
                case WebViewCommand.Cut:
                    await commandManager.CutAsync().ConfigureAwait(false);
                    break;
                case WebViewCommand.Paste:
                    await commandManager.PasteAsync().ConfigureAwait(false);
                    break;
                case WebViewCommand.SelectAll:
                    await commandManager.SelectAllAsync().ConfigureAwait(false);
                    break;
                case WebViewCommand.Undo:
                    await commandManager.UndoAsync().ConfigureAwait(false);
                    break;
                case WebViewCommand.Redo:
                    await commandManager.RedoAsync().ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, "Unsupported command action.");
            }

            return true;
        }
        catch (Exception ex)
        {
            ReportPolicyFailure(WebViewShellPolicyDomain.Command, ex);
            return false;
        }
    }

    private void ReportCommandDenied(WebViewCommand command, string? denyReason)
    {
        ReportPolicyFailure(
            WebViewShellPolicyDomain.Command,
            new UnauthorizedAccessException(
                denyReason ?? $"Command '{command}' was denied by shell policy."));
    }

    /// <summary>
    /// Closes a managed window by id and waits for bounded teardown completion.
    /// </summary>
    public async Task<bool> CloseManagedWindowAsync(
        Guid windowId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        ManagedWindowEntry? entry;
        lock (_managedWindowsLock)
        {
            if (!_managedWindows.TryGetValue(windowId, out entry))
                return false;
        }

        if (entry is null)
            return false;

        if (!TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Closing))
            return false;

        var closeHandler = _options.ManagedWindowCloseAsync ?? DefaultManagedWindowCloseAsync;
        var closeTimeout = timeout ?? _options.ManagedWindowCloseTimeout;
        var closeSucceeded = true;

        using var timeoutCts = new CancellationTokenSource(closeTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
        try
        {
            await closeHandler(entry.Window, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            closeSucceeded = false;
            ReportPolicyFailure(WebViewShellPolicyDomain.ManagedWindowLifecycle, ex);
        }
        catch (Exception ex)
        {
            closeSucceeded = false;
            ReportPolicyFailure(WebViewShellPolicyDomain.ManagedWindowLifecycle, ex);
        }
        finally
        {
            lock (_managedWindowsLock)
                _managedWindows.Remove(windowId);

            TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Closed);
        }

        return closeSucceeded;
    }

    private static Task DefaultManagedWindowCloseAsync(IWebView webView, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        webView.Dispose();
        return Task.CompletedTask;
    }

    private void ExecuteStrategyDecision(WebViewNewWindowStrategyDecision decision, Guid candidateWindowId, NewWindowRequestedEventArgs args)
    {
        switch (decision.Strategy)
        {
            case WebViewNewWindowStrategy.InPlace:
                args.Handled = false;
                return;
            case WebViewNewWindowStrategy.ManagedWindow:
            {
                var created = TryCreateManagedWindow(candidateWindowId, args.Uri, decision.ScopeIdentityOverride);
                args.Handled = created;
                if (!created)
                    args.Handled = false; // fallback to baseline in-view navigation
                return;
            }
            case WebViewNewWindowStrategy.ExternalBrowser:
            {
                if (args.Uri is null)
                {
                    args.Handled = false;
                    return;
                }

                if (_options.HostCapabilityBridge is not null)
                {
                    var openResult = _options.HostCapabilityBridge.OpenExternal(
                        args.Uri,
                        _rootWindowId,
                        parentWindowId: _rootWindowId,
                        targetWindowId: null);

                    args.Handled = true;
                    if (!openResult.IsAllowed)
                    {
                        ReportPolicyFailure(
                            WebViewShellPolicyDomain.ExternalOpen,
                            new UnauthorizedAccessException(openResult.DenyReason ?? "External open was denied by host capability policy."));
                        return;
                    }

                    if (!openResult.IsSuccess && openResult.Error is not null)
                    {
                        ReportPolicyFailure(WebViewShellPolicyDomain.ExternalOpen, openResult.Error);
                    }
                    return;
                }

                if (_options.ExternalOpenHandler is null)
                {
                    args.Handled = false;
                    return;
                }

                args.Handled = true;
                ExecutePolicyDomain(
                    WebViewShellPolicyDomain.ExternalOpen,
                    () => _options.ExternalOpenHandler.Invoke(_webView, args.Uri));
                return;
            }
            case WebViewNewWindowStrategy.Delegate:
                args.Handled = decision.Handled;
                return;
            default:
                args.Handled = false;
                return;
        }
    }

    private bool TryCreateManagedWindow(Guid windowId, Uri? targetUri, string? scopeIdentityOverride)
    {
        if (_options.ManagedWindowFactory is null)
            return false;

        var scopeIdentity = string.IsNullOrWhiteSpace(scopeIdentityOverride)
            ? _options.SessionContext.ScopeIdentity
            : scopeIdentityOverride.Trim();

        var sessionContext = _options.SessionContext with
        {
            ScopeIdentity = scopeIdentity,
            WindowId = windowId,
            ParentWindowId = _rootWindowId,
            RequestUri = targetUri
        };

        var sessionDecision = _options.SessionPolicy is null
            ? _sessionDecision
            : ExecutePolicyDomain(WebViewShellPolicyDomain.Session, () => _options.SessionPolicy.Resolve(sessionContext));

        WebViewSessionPermissionProfile? resolvedProfile = null;
        var profileIdentity = _rootProfile?.ProfileIdentity;
        if (_options.SessionPermissionProfileResolver is not null)
        {
            var profileContext = new WebViewSessionPermissionProfileContext(
                _rootWindowId,
                ParentWindowId: _rootWindowId,
                WindowId: windowId,
                ScopeIdentity: scopeIdentity,
                RequestUri: targetUri,
                PermissionKind: null);

            resolvedProfile = ExecutePolicyDomain(
                WebViewShellPolicyDomain.Session,
                () => _options.SessionPermissionProfileResolver.Resolve(profileContext, _rootProfile));

            if (resolvedProfile is not null)
            {
                sessionDecision = resolvedProfile.ResolveSessionDecision(
                    parentDecision: _sessionDecision,
                    fallbackDecision: sessionDecision,
                    scopeIdentity: scopeIdentity);
                profileIdentity = resolvedProfile.ProfileIdentity;

                if (sessionDecision is not null)
                {
                    RaiseSessionPermissionProfileDiagnostic(
                        windowId,
                        _rootWindowId,
                        scopeIdentity,
                        resolvedProfile,
                        sessionDecision,
                        permissionKind: null,
                        permissionDecision: WebViewPermissionProfileDecision.DefaultFallback());
                }
            }
        }

        var createContext = new WebViewManagedWindowCreateContext(
            windowId,
            _rootWindowId,
            targetUri,
            scopeIdentity,
            sessionDecision,
            profileIdentity);

        var managedWindow = ExecutePolicyDomain(
            WebViewShellPolicyDomain.ManagedWindowLifecycle,
            () => _options.ManagedWindowFactory?.Invoke(createContext));

        if (managedWindow is null)
            return false;

        var entry = new ManagedWindowEntry(windowId, _rootWindowId, managedWindow, sessionDecision, profileIdentity);
        lock (_managedWindowsLock)
            _managedWindows[windowId] = entry;

        if (!TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Created))
            return false;
        if (!TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Attached))
            return false;
        if (!TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Ready))
            return false;

        return true;
    }

    private bool TryTransitionManagedWindowState(ManagedWindowEntry entry, WebViewManagedWindowLifecycleState nextState)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!IsTransitionAllowed(entry.State, nextState))
        {
            ReportPolicyFailure(
                WebViewShellPolicyDomain.ManagedWindowLifecycle,
                new InvalidOperationException($"Invalid managed window lifecycle transition '{entry.State?.ToString() ?? "None"}' -> '{nextState}'."));
            return false;
        }

        entry.State = nextState;
        ManagedWindowLifecycleChanged?.Invoke(
            this,
            new WebViewManagedWindowLifecycleEventArgs(
                entry.WindowId,
                entry.ParentWindowId,
                nextState,
                entry.SessionDecision,
                entry.ProfileIdentity));
        return true;
    }

    private static bool IsTransitionAllowed(
        WebViewManagedWindowLifecycleState? currentState,
        WebViewManagedWindowLifecycleState nextState)
    {
        return currentState switch
        {
            null => nextState == WebViewManagedWindowLifecycleState.Created,
            WebViewManagedWindowLifecycleState.Created => nextState is WebViewManagedWindowLifecycleState.Attached or WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Attached => nextState is WebViewManagedWindowLifecycleState.Ready or WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Ready => nextState == WebViewManagedWindowLifecycleState.Closing,
            WebViewManagedWindowLifecycleState.Closing => nextState == WebViewManagedWindowLifecycleState.Closed,
            WebViewManagedWindowLifecycleState.Closed => false,
            _ => false
        };
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

    private void RaiseSessionPermissionProfileDiagnostic(
        Guid windowId,
        Guid? parentWindowId,
        string scopeIdentity,
        WebViewSessionPermissionProfile profile,
        WebViewShellSessionDecision sessionDecision,
        WebViewPermissionKind? permissionKind,
        WebViewPermissionProfileDecision permissionDecision)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sessionDecision);

        SessionPermissionProfileEvaluated?.Invoke(
            this,
            new WebViewSessionPermissionProfileDiagnosticEventArgs(
                windowId,
                parentWindowId,
                profile.ProfileIdentity,
                scopeIdentity,
                sessionDecision,
                permissionKind,
                permissionDecision));
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        List<ManagedWindowEntry> entries;
        lock (_managedWindowsLock)
        {
            entries = [.. _managedWindows.Values];
            _managedWindows.Clear();
        }

        foreach (var entry in entries)
        {
            if (entry.State is not WebViewManagedWindowLifecycleState.Closing and not WebViewManagedWindowLifecycleState.Closed)
                TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Closing);
            try
            {
                entry.Window.Dispose();
            }
            catch (Exception ex)
            {
                ReportPolicyFailure(WebViewShellPolicyDomain.ManagedWindowLifecycle, ex);
            }
            TryTransitionManagedWindowState(entry, WebViewManagedWindowLifecycleState.Closed);
        }

        _webView.NewWindowRequested -= OnNewWindowRequested;
        _webView.DownloadRequested -= OnDownloadRequested;
        _webView.PermissionRequested -= OnPermissionRequested;
    }

    private sealed class ManagedWindowEntry
    {
        public ManagedWindowEntry(
            Guid windowId,
            Guid parentWindowId,
            IWebView window,
            WebViewShellSessionDecision? sessionDecision,
            string? profileIdentity)
        {
            WindowId = windowId;
            ParentWindowId = parentWindowId;
            Window = window;
            SessionDecision = sessionDecision;
            ProfileIdentity = profileIdentity;
        }

        public Guid WindowId { get; }
        public Guid ParentWindowId { get; }
        public IWebView Window { get; }
        public WebViewShellSessionDecision? SessionDecision { get; }
        public string? ProfileIdentity { get; }
        public WebViewManagedWindowLifecycleState? State { get; set; }
    }
}

