using System;
using System.Collections.Generic;

namespace Agibuild.Avalonia.WebView.Shell;

/// <summary>
/// Input context for resolving session-permission profiles.
/// </summary>
public sealed record WebViewSessionPermissionProfileContext(
    Guid RootWindowId,
    Guid? ParentWindowId,
    Guid WindowId,
    string ScopeIdentity,
    Uri? RequestUri,
    WebViewPermissionKind? PermissionKind);

/// <summary>
/// Typed permission decision under profile governance.
/// </summary>
public readonly record struct WebViewPermissionProfileDecision(PermissionState State, bool IsExplicit)
{
    /// <summary>Use runtime fallback behavior.</summary>
    public static WebViewPermissionProfileDecision DefaultFallback()
        => new(PermissionState.Default, IsExplicit: false);

    /// <summary>Explicitly allow permission request.</summary>
    public static WebViewPermissionProfileDecision Allow()
        => new(PermissionState.Allow, IsExplicit: true);

    /// <summary>Explicitly deny permission request.</summary>
    public static WebViewPermissionProfileDecision Deny()
        => new(PermissionState.Deny, IsExplicit: true);
}

/// <summary>
/// Resolved profile model that governs session and permission behavior.
/// </summary>
public sealed class WebViewSessionPermissionProfile
{
    private static readonly IReadOnlyDictionary<WebViewPermissionKind, WebViewPermissionProfileDecision> EmptyPermissionDecisions =
        new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>();

    /// <summary>Stable profile identity for diagnostics/auditing.</summary>
    public required string ProfileIdentity { get; init; }

    /// <summary>
    /// Whether this profile inherits parent session decision when override is absent.
    /// </summary>
    public bool InheritParentSessionDecision { get; init; } = true;

    /// <summary>
    /// Optional explicit session decision override.
    /// </summary>
    public WebViewShellSessionDecision? SessionDecisionOverride { get; init; }

    /// <summary>
    /// Default permission decision used when no per-kind rule exists.
    /// </summary>
    public WebViewPermissionProfileDecision DefaultPermissionDecision { get; init; } = WebViewPermissionProfileDecision.DefaultFallback();

    /// <summary>
    /// Per-permission-kind decision overrides.
    /// </summary>
    public IReadOnlyDictionary<WebViewPermissionKind, WebViewPermissionProfileDecision> PermissionDecisions { get; init; } =
        EmptyPermissionDecisions;

    /// <summary>
    /// Resolves final session decision for current context.
    /// </summary>
    internal WebViewShellSessionDecision ResolveSessionDecision(
        WebViewShellSessionDecision? parentDecision,
        WebViewShellSessionDecision? fallbackDecision,
        string scopeIdentity)
    {
        if (SessionDecisionOverride is not null)
            return SessionDecisionOverride;
        if (InheritParentSessionDecision && parentDecision is not null)
            return parentDecision;
        if (fallbackDecision is not null)
            return fallbackDecision;

        var normalizedScope = string.IsNullOrWhiteSpace(scopeIdentity) ? "default" : scopeIdentity.Trim();
        return new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, normalizedScope);
    }

    /// <summary>
    /// Resolves permission decision for requested kind.
    /// </summary>
    public WebViewPermissionProfileDecision ResolvePermissionDecision(WebViewPermissionKind permissionKind)
    {
        return PermissionDecisions.TryGetValue(permissionKind, out var decision)
            ? decision
            : DefaultPermissionDecision;
    }
}

/// <summary>
/// Resolver for session-permission profile.
/// </summary>
public interface IWebViewSessionPermissionProfileResolver
{
    /// <summary>
    /// Resolves profile using current context and optional parent profile.
    /// </summary>
    WebViewSessionPermissionProfile Resolve(
        WebViewSessionPermissionProfileContext context,
        WebViewSessionPermissionProfile? parentProfile);
}

/// <summary>
/// Delegate-based session-permission profile resolver.
/// </summary>
public sealed class DelegateSessionPermissionProfileResolver : IWebViewSessionPermissionProfileResolver
{
    private readonly Func<WebViewSessionPermissionProfileContext, WebViewSessionPermissionProfile?, WebViewSessionPermissionProfile> _resolver;

    /// <summary>Creates resolver backed by host callback.</summary>
    public DelegateSessionPermissionProfileResolver(
        Func<WebViewSessionPermissionProfileContext, WebViewSessionPermissionProfile?, WebViewSessionPermissionProfile> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public WebViewSessionPermissionProfile Resolve(
        WebViewSessionPermissionProfileContext context,
        WebViewSessionPermissionProfile? parentProfile)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _resolver(context, parentProfile);
    }
}

/// <summary>
/// Diagnostic payload for session-permission profile evaluation.
/// </summary>
public sealed class WebViewSessionPermissionProfileDiagnosticEventArgs : EventArgs
{
    /// <summary>Create profile diagnostic args.</summary>
    public WebViewSessionPermissionProfileDiagnosticEventArgs(
        Guid windowId,
        Guid? parentWindowId,
        string profileIdentity,
        string scopeIdentity,
        WebViewShellSessionDecision sessionDecision,
        WebViewPermissionKind? permissionKind,
        WebViewPermissionProfileDecision permissionDecision)
    {
        WindowId = windowId;
        ParentWindowId = parentWindowId;
        ProfileIdentity = profileIdentity;
        ScopeIdentity = scopeIdentity;
        SessionDecision = sessionDecision;
        PermissionKind = permissionKind;
        PermissionDecision = permissionDecision;
    }

    /// <summary>Window identity associated with this evaluation.</summary>
    public Guid WindowId { get; }

    /// <summary>Optional parent window identity.</summary>
    public Guid? ParentWindowId { get; }

    /// <summary>Resolved profile identity.</summary>
    public string ProfileIdentity { get; }

    /// <summary>Scope identity at evaluation time.</summary>
    public string ScopeIdentity { get; }

    /// <summary>Resolved session decision in effect.</summary>
    public WebViewShellSessionDecision SessionDecision { get; }

    /// <summary>Permission kind for permission-path evaluations.</summary>
    public WebViewPermissionKind? PermissionKind { get; }

    /// <summary>Resolved permission profile decision.</summary>
    public WebViewPermissionProfileDecision PermissionDecision { get; }
}
