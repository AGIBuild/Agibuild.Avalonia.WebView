using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora;

namespace Agibuild.Fulora.Shell;

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
    Command = 7,
    /// <summary>Policy domain for shell system integration actions.</summary>
    SystemIntegration = 8
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
/// Context for menu pruning policy evaluation.
/// </summary>
public sealed record WebViewMenuPruningPolicyContext(
    Guid RootWindowId,
    Guid? TargetWindowId,
    WebViewMenuModelRequest RequestedMenuModel,
    WebViewMenuModelRequest? CurrentEffectiveMenuModel,
    string? ProfileIdentity,
    WebViewPermissionProfileDecision? ProfilePermissionDecision);

/// <summary>
/// Decision returned by <see cref="IWebViewShellMenuPruningPolicy"/>.
/// </summary>
public sealed record WebViewMenuPruningDecision(
    bool IsAllowed,
    WebViewMenuModelRequest? EffectiveMenuModel = null,
    string? DenyReason = null)
{
    /// <summary>Create allow decision.</summary>
    public static WebViewMenuPruningDecision Allow(WebViewMenuModelRequest? effectiveMenuModel = null)
        => new(true, effectiveMenuModel);

    /// <summary>Create deny decision.</summary>
    public static WebViewMenuPruningDecision Deny(string? reason = null)
        => new(false, EffectiveMenuModel: null, reason);
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
    /// <summary>Optional policy for handling <see cref="IWebViewPopupWindows.NewWindowRequested"/>.</summary>
    public IWebViewNewWindowPolicy? NewWindowPolicy { get; init; }
    /// <summary>Optional typed host capability bridge.</summary>
    public WebViewHostCapabilityBridge? HostCapabilityBridge { get; init; }
    /// <summary>Optional factory that creates a managed child window when strategy is <c>ManagedWindow</c>.</summary>
    public Func<WebViewManagedWindowCreateContext, IWebView?>? ManagedWindowFactory { get; init; }
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
    /// <summary>Optional policy object for menu pruning governance.</summary>
    public IWebViewShellMenuPruningPolicy? MenuPruningPolicy { get; init; }
    /// <summary>Optional whitelist of allowed typed system actions.</summary>
    public IReadOnlySet<WebViewSystemAction>? SystemActionWhitelist { get; init; }
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
/// Policy for handling <see cref="IWebViewPopupWindows.NewWindowRequested"/> in a host-controlled way.
/// </summary>
public interface IWebViewNewWindowPolicy
{
    /// <summary>Resolves a strategy decision for the new-window request.</summary>
    WebViewNewWindowStrategyDecision Decide(IWebView webView, NewWindowRequestedEventArgs e, WebViewNewWindowPolicyContext context);
}

/// <summary>
/// Policy for handling <see cref="IWebViewDownloads.DownloadRequested"/>.
/// </summary>
public interface IWebViewDownloadPolicy
{
    /// <summary>Handles the download request.</summary>
    void Handle(IWebView webView, DownloadRequestedEventArgs e);
}

/// <summary>
/// Policy for handling <see cref="IWebViewPermissions.PermissionRequested"/>.
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
/// Policy for pruning menu model state in shell governance pipeline.
/// </summary>
public interface IWebViewShellMenuPruningPolicy
{
    /// <summary>Resolves allow/deny decision and effective menu model.</summary>
    WebViewMenuPruningDecision Decide(IWebView webView, WebViewMenuPruningPolicyContext context);
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
/// Menu pruning policy that delegates decision logic to host callback.
/// </summary>
public sealed class DelegateMenuPruningPolicy : IWebViewShellMenuPruningPolicy
{
    private readonly Func<IWebView, WebViewMenuPruningPolicyContext, WebViewMenuPruningDecision> _resolver;

    /// <summary>Creates a delegating menu pruning policy.</summary>
    public DelegateMenuPruningPolicy(Func<IWebView, WebViewMenuPruningPolicyContext, WebViewMenuPruningDecision> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public WebViewMenuPruningDecision Decide(IWebView webView, WebViewMenuPruningPolicyContext context)
        => _resolver(webView, context);
}
