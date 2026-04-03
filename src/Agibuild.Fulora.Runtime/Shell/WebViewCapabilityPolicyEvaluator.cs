using System;
using System.Collections.Generic;
using System.Linq;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Maps host capability operations onto stable capability descriptors and effective policy decisions.
/// </summary>
public sealed class WebViewCapabilityPolicyEvaluator
{
    private const string HostBridgeSourceComponent = "host-bridge";

    private static readonly IReadOnlyDictionary<WebViewHostCapabilityOperation, WebViewCapabilityDescriptor> s_descriptors =
        new Dictionary<WebViewHostCapabilityOperation, WebViewCapabilityDescriptor>
        {
            [WebViewHostCapabilityOperation.ClipboardReadText] = new("clipboard.read", HostBridgeSourceComponent, WebViewHostCapabilityOperation.ClipboardReadText),
            [WebViewHostCapabilityOperation.ClipboardWriteText] = new("clipboard.write", HostBridgeSourceComponent, WebViewHostCapabilityOperation.ClipboardWriteText),
            [WebViewHostCapabilityOperation.FileDialogOpen] = new("filesystem.pick", HostBridgeSourceComponent, WebViewHostCapabilityOperation.FileDialogOpen),
            [WebViewHostCapabilityOperation.FileDialogSave] = new("filesystem.pick", HostBridgeSourceComponent, WebViewHostCapabilityOperation.FileDialogSave),
            [WebViewHostCapabilityOperation.ExternalOpen] = new("shell.external_open", HostBridgeSourceComponent, WebViewHostCapabilityOperation.ExternalOpen),
            [WebViewHostCapabilityOperation.NotificationShow] = new("notification.post", HostBridgeSourceComponent, WebViewHostCapabilityOperation.NotificationShow),
            [WebViewHostCapabilityOperation.MenuApplyModel] = new("window.chrome.modify", HostBridgeSourceComponent, WebViewHostCapabilityOperation.MenuApplyModel),
            [WebViewHostCapabilityOperation.TrayUpdateState] = new("window.chrome.modify", HostBridgeSourceComponent, WebViewHostCapabilityOperation.TrayUpdateState),
            [WebViewHostCapabilityOperation.SystemActionExecute] = new("shell.system_action.execute", HostBridgeSourceComponent, WebViewHostCapabilityOperation.SystemActionExecute),
            [WebViewHostCapabilityOperation.TrayInteractionEventDispatch] = new("shell.integration.event.dispatch", HostBridgeSourceComponent, WebViewHostCapabilityOperation.TrayInteractionEventDispatch),
            [WebViewHostCapabilityOperation.MenuInteractionEventDispatch] = new("shell.integration.event.dispatch", HostBridgeSourceComponent, WebViewHostCapabilityOperation.MenuInteractionEventDispatch),
            [WebViewHostCapabilityOperation.GlobalShortcutRegister] = new("shell.shortcut.register", HostBridgeSourceComponent, WebViewHostCapabilityOperation.GlobalShortcutRegister)
        };

    /// <summary>Returns the stable capability descriptor for the given operation.</summary>
    public static WebViewCapabilityDescriptor Describe(WebViewHostCapabilityOperation operation)
    {
        if (s_descriptors.TryGetValue(operation, out var descriptor))
            return descriptor;

        throw new ArgumentOutOfRangeException(nameof(operation), operation, "No capability descriptor is registered for the requested host capability operation.");
    }

    /// <summary>Returns the stable capability descriptor for the given context.</summary>
    public WebViewCapabilityDescriptor Describe(in WebViewHostCapabilityRequestContext context)
        => Describe(context.Operation);

    /// <summary>Builds a rich authorization context for policy v2 and diagnostics.</summary>
    public WebViewCapabilityAuthorizationContext CreateAuthorizationContext(
        in WebViewHostCapabilityRequestContext context,
        string? requestedAction = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        var descriptor = Describe(context);
        return new WebViewCapabilityAuthorizationContext(
            context.RootWindowId,
            context.ParentWindowId,
            context.TargetWindowId,
            context.Operation,
            descriptor.CapabilityId,
            descriptor.SourceComponent,
            context.RequestUri,
            requestedAction,
            attributes);
    }

    /// <summary>
    /// Evaluates the effective policy decision for the given legacy host capability context.
    /// Preserved for compatibility and forwarded through the richer authorization path.
    /// </summary>
    public WebViewCapabilityPolicyDecision Evaluate(
        IWebViewHostCapabilityPolicy? policy,
        in WebViewHostCapabilityRequestContext context)
    {
        var authorizationContext = CreateAuthorizationContext(context);
        return Evaluate(policy, policyV2: null, authorizationContext);
    }

    /// <summary>Evaluates the effective policy decision for the given host capability context.</summary>
    public WebViewCapabilityPolicyDecision Evaluate(
        IWebViewHostCapabilityPolicy? policy,
        IWebViewHostCapabilityPolicyV2? policyV2,
        in WebViewCapabilityAuthorizationContext context)
    {
        WebViewCapabilityPolicyDecision decision;
        if (policyV2 is not null)
        {
            decision = policyV2.Evaluate(context);
        }
        else if (policy is not null)
        {
            var legacyContext = new WebViewHostCapabilityRequestContext(
                context.RootWindowId,
                context.ParentWindowId,
                context.TargetWindowId,
                context.Operation,
                context.RequestUri);
            var legacyDecision = policy.Evaluate(legacyContext);
            decision = legacyDecision.Kind switch
            {
                WebViewHostCapabilityDecisionKind.Allow => WebViewCapabilityPolicyDecision.Allow(),
                WebViewHostCapabilityDecisionKind.Deny => WebViewCapabilityPolicyDecision.Deny(legacyDecision.Reason),
                _ => throw new ArgumentOutOfRangeException(nameof(legacyDecision), legacyDecision.Kind, "Unsupported host capability decision kind.")
            };
        }
        else
        {
            decision = WebViewCapabilityPolicyDecision.Allow();
        }

        return EnforceConstraints(context, decision);
    }

    private static WebViewCapabilityPolicyDecision EnforceConstraints(
        in WebViewCapabilityAuthorizationContext context,
        WebViewCapabilityPolicyDecision decision)
    {
        if (decision.Kind != WebViewCapabilityPolicyDecisionKind.AllowWithConstraint ||
            decision.Constraints is null ||
            decision.Constraints.Count == 0)
        {
            return decision;
        }

        if (decision.Constraints.TryGetValue("allowedSchemes", out var allowedSchemes))
        {
            if (context.RequestUri is null || !ValueListContains(allowedSchemes, context.RequestUri.Scheme))
                return WebViewCapabilityPolicyDecision.Deny("capability-constraint-scheme-denied");
        }

        if (decision.Constraints.TryGetValue("allowedHosts", out var allowedHosts))
        {
            if (context.RequestUri is null || string.IsNullOrWhiteSpace(context.RequestUri.Host) || !ValueListContains(allowedHosts, context.RequestUri.Host))
                return WebViewCapabilityPolicyDecision.Deny("capability-constraint-host-denied");
        }

        if (decision.Constraints.TryGetValue("allowedActions", out var allowedActions))
        {
            if (string.IsNullOrWhiteSpace(context.RequestedAction) || !ValueListContains(allowedActions, context.RequestedAction))
                return WebViewCapabilityPolicyDecision.Deny("capability-constraint-action-denied");
        }

        if (decision.Constraints.TryGetValue("allowMenu", out var allowMenu) &&
            context.Attributes is not null &&
            context.Attributes.TryGetValue("target", out var target) &&
            string.Equals(target, "menu", StringComparison.Ordinal) &&
            !IsEnabled(allowMenu))
        {
            return WebViewCapabilityPolicyDecision.Deny("capability-constraint-menu-denied");
        }

        if (decision.Constraints.TryGetValue("allowTray", out var allowTray) &&
            context.Attributes is not null &&
            context.Attributes.TryGetValue("target", out target) &&
            string.Equals(target, "tray", StringComparison.Ordinal) &&
            !IsEnabled(allowTray))
        {
            return WebViewCapabilityPolicyDecision.Deny("capability-constraint-tray-denied");
        }

        return decision;
    }

    private static bool ValueListContains(string csv, string candidate)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(item => string.Equals(item, candidate, StringComparison.OrdinalIgnoreCase));

    private static bool IsEnabled(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
