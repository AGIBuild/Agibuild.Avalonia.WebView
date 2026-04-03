using System;
using System.Collections.Generic;
using System.Linq;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Thin runtime façade for shell menu pruning, normalization, and effective menu state snapshots.
/// </summary>
internal sealed class ShellMenuGovernanceRuntime
{
    private readonly IWebView _webView;
    private readonly WebViewShellExperienceOptions _options;
    private readonly Guid _rootWindowId;
    private readonly Func<WebViewShellSessionDecision?> _getSessionDecision;
    private readonly Func<WebViewSessionPermissionProfile?> _getRootProfile;
    private readonly Action<Guid, Guid?, string, WebViewSessionPermissionProfile, WebViewShellSessionDecision, WebViewPermissionKind?, WebViewPermissionProfileDecision> _raiseSessionPermissionProfileDiagnostic;
    private readonly Action<WebViewHostCapabilityCallResult<object?>, string> _reportSystemIntegrationOutcome;
    private readonly Action<WebViewShellPolicyDomain, Exception> _reportPolicyFailure;

    private WebViewMenuModelRequest? _effectiveMenuModel;

    public ShellMenuGovernanceRuntime(
        IWebView webView,
        WebViewShellExperienceOptions options,
        Guid rootWindowId,
        Func<WebViewShellSessionDecision?> getSessionDecision,
        Func<WebViewSessionPermissionProfile?> getRootProfile,
        Action<Guid, Guid?, string, WebViewSessionPermissionProfile, WebViewShellSessionDecision, WebViewPermissionKind?, WebViewPermissionProfileDecision> raiseSessionPermissionProfileDiagnostic,
        Action<WebViewHostCapabilityCallResult<object?>, string> reportSystemIntegrationOutcome,
        Action<WebViewShellPolicyDomain, Exception> reportPolicyFailure)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rootWindowId = rootWindowId;
        _getSessionDecision = getSessionDecision ?? throw new ArgumentNullException(nameof(getSessionDecision));
        _getRootProfile = getRootProfile ?? throw new ArgumentNullException(nameof(getRootProfile));
        _raiseSessionPermissionProfileDiagnostic = raiseSessionPermissionProfileDiagnostic
                                                  ?? throw new ArgumentNullException(nameof(raiseSessionPermissionProfileDiagnostic));
        _reportSystemIntegrationOutcome = reportSystemIntegrationOutcome
                                          ?? throw new ArgumentNullException(nameof(reportSystemIntegrationOutcome));
        _reportPolicyFailure = reportPolicyFailure ?? throw new ArgumentNullException(nameof(reportPolicyFailure));
    }

    public WebViewMenuModelRequest? GetEffectiveMenuModelSnapshot()
        => _effectiveMenuModel is null ? null : CloneMenuModel(_effectiveMenuModel);

    public void UpdateEffectiveMenuModel(WebViewMenuModelRequest effectiveMenuModel)
    {
        ArgumentNullException.ThrowIfNull(effectiveMenuModel);
        _effectiveMenuModel = CloneMenuModel(effectiveMenuModel);
    }

    public WebViewMenuModelRequest NormalizeMenuModel(WebViewMenuModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new WebViewMenuModelRequest
        {
            Items = NormalizeMenuItems(request.Items)
        };
    }

    public (WebViewMenuModelRequest? EffectiveMenuModel, WebViewHostCapabilityCallResult<object?>? Result) TryPruneMenuModel(
        WebViewMenuModelRequest requestedMenuModel)
    {
        ArgumentNullException.ThrowIfNull(requestedMenuModel);

        if (_options.MenuPruningPolicy is null)
            return (requestedMenuModel, null);

        var profileDecision = EvaluateMenuPruningProfileDecision();
        if (profileDecision.Result is not null)
            return (null, profileDecision.Result);

        var context = new WebViewMenuPruningPolicyContext(
            RootWindowId: _rootWindowId,
            TargetWindowId: _rootWindowId,
            RequestedMenuModel: requestedMenuModel,
            CurrentEffectiveMenuModel: GetEffectiveMenuModelSnapshot(),
            ProfileIdentity: profileDecision.ProfileIdentity,
            ProfilePermissionDecision: profileDecision.ProfilePermissionDecision);

        WebViewMenuPruningDecision decision;
        try
        {
            decision = _options.MenuPruningPolicy.Decide(_webView, context);
        }
        catch (Exception ex)
        {
            if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
            var failed = WebViewHostCapabilityCallResult<object?>.Failure(
                WebViewCapabilityPolicyEvaluator.Describe(WebViewHostCapabilityOperation.MenuApplyModel),
                WebViewCapabilityPolicyDecision.Deny("menu-pruning-policy-failed"),
                ex,
                wasAuthorized: false);
            ReportSystemIntegrationOutcome(
                failed,
                "Menu pruning policy failed.");
            return (null, failed);
        }

        if (!decision.IsAllowed)
        {
            var denied = WebViewHostCapabilityCallResult<object?>.Denied(
                WebViewCapabilityPolicyEvaluator.Describe(WebViewHostCapabilityOperation.MenuApplyModel),
                WebViewCapabilityPolicyDecision.Deny(decision.DenyReason ?? "menu-pruning-policy-denied"),
                decision.DenyReason ?? "menu-pruning-policy-denied");
            ReportSystemIntegrationOutcome(
                denied,
                "Menu pruning was denied by shell policy stage.");
            return (null, denied);
        }

        var effective = NormalizeMenuModel(decision.EffectiveMenuModel ?? requestedMenuModel);
        return (effective, null);
    }

    private (WebViewHostCapabilityCallResult<object?>? Result, string? ProfileIdentity, WebViewPermissionProfileDecision? ProfilePermissionDecision)
        EvaluateMenuPruningProfileDecision()
    {
        if (_options.SessionPermissionProfileResolver is null)
            return (null, null, null);

        var sessionDecision = _getSessionDecision();
        var scopeIdentity = sessionDecision?.ScopeIdentity ?? _options.SessionContext.ScopeIdentity;
        var profileContext = new WebViewSessionPermissionProfileContext(
            _rootWindowId,
            ParentWindowId: null,
            WindowId: _rootWindowId,
            ScopeIdentity: scopeIdentity,
            RequestUri: _options.SessionContext.RequestUri,
            PermissionKind: WebViewPermissionKind.Other);

        WebViewSessionPermissionProfile resolvedProfile;
        try
        {
            resolvedProfile = _options.SessionPermissionProfileResolver.Resolve(profileContext, _getRootProfile());
        }
        catch (Exception ex)
        {
            if (!WebViewOperationFailure.TryGetCategory(ex, out _))
                WebViewOperationFailure.SetCategory(ex, WebViewOperationFailureCategory.AdapterFailed);
            var failed = WebViewHostCapabilityCallResult<object?>.Failure(
                WebViewCapabilityPolicyEvaluator.Describe(WebViewHostCapabilityOperation.MenuApplyModel),
                WebViewCapabilityPolicyDecision.Deny("menu-pruning-profile-resolution-failed"),
                ex,
                wasAuthorized: false);
            ReportSystemIntegrationOutcome(
                failed,
                "Menu pruning profile resolution failed.");
            return (failed, null, null);
        }

        if (resolvedProfile is null)
        {
            var nullProfile = new InvalidOperationException("Session permission profile resolver returned null for menu pruning.");
            WebViewOperationFailure.SetCategory(nullProfile, WebViewOperationFailureCategory.AdapterFailed);
            var failed = WebViewHostCapabilityCallResult<object?>.Failure(
                WebViewCapabilityPolicyEvaluator.Describe(WebViewHostCapabilityOperation.MenuApplyModel),
                WebViewCapabilityPolicyDecision.Deny("menu-pruning-profile-resolution-failed"),
                nullProfile,
                wasAuthorized: false);
            ReportSystemIntegrationOutcome(
                failed,
                "Menu pruning profile resolution failed.");
            return (failed, null, null);
        }

        var effectiveSessionDecision = resolvedProfile.ResolveSessionDecision(
            parentDecision: null,
            fallbackDecision: sessionDecision,
            scopeIdentity: scopeIdentity);
        var profilePermissionDecision = resolvedProfile.ResolvePermissionDecision(WebViewPermissionKind.Other);

        _raiseSessionPermissionProfileDiagnostic(
            _rootWindowId,
            null,
            scopeIdentity,
            resolvedProfile,
            effectiveSessionDecision,
            WebViewPermissionKind.Other,
            profilePermissionDecision);

        if (profilePermissionDecision.IsExplicit && profilePermissionDecision.State == PermissionState.Deny)
        {
            var denied = WebViewHostCapabilityCallResult<object?>.Denied(
                WebViewCapabilityPolicyEvaluator.Describe(WebViewHostCapabilityOperation.MenuApplyModel),
                WebViewCapabilityPolicyDecision.Deny($"menu-pruning-profile-denied:{resolvedProfile.ProfileIdentity}"),
                $"menu-pruning-profile-denied:{resolvedProfile.ProfileIdentity}");
            ReportSystemIntegrationOutcome(
                denied,
                "Menu pruning was denied by session permission profile stage.");
            return (denied, resolvedProfile.ProfileIdentity, profilePermissionDecision);
        }

        return (null, resolvedProfile.ProfileIdentity, profilePermissionDecision);
    }

    private void ReportSystemIntegrationOutcome(
        WebViewHostCapabilityCallResult<object?> result,
        string defaultDenyReason)
    {
        try
        {
            _reportSystemIntegrationOutcome(result, defaultDenyReason);
        }
        catch (Exception ex)
        {
            _reportPolicyFailure(WebViewShellPolicyDomain.SystemIntegration, ex);
        }
    }

    private static List<WebViewMenuItemModel> NormalizeMenuItems(IReadOnlyList<WebViewMenuItemModel> items)
    {
        var normalized = new List<WebViewMenuItemModel>(items.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (item is null)
                continue;

            var id = item.Id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (!seen.Add(id))
                continue;

            normalized.Add(new WebViewMenuItemModel
            {
                Id = id,
                Label = item.Label,
                IsEnabled = item.IsEnabled,
                Children = NormalizeMenuItems(item.Children)
            });
        }

        return normalized;
    }

    private static WebViewMenuModelRequest CloneMenuModel(WebViewMenuModelRequest model)
        => new()
        {
            Items = CloneMenuItems(model.Items)
        };

    private static WebViewMenuItemModel[] CloneMenuItems(IReadOnlyList<WebViewMenuItemModel> items)
        => items.Select(item => new WebViewMenuItemModel
        {
            Id = item.Id,
            Label = item.Label,
            IsEnabled = item.IsEnabled,
            Children = CloneMenuItems(item.Children)
        }).ToArray();
}
