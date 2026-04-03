using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class CapabilityPolicyEvaluatorTests
{
    [Fact]
    public void CreateAuthorizationContext_copies_descriptor_and_attributes()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.ExternalOpen,
            new Uri("https://example.com/docs"));

        var authorizationContext = evaluator.CreateAuthorizationContext(
            requestContext,
            requestedAction: "open-external",
            attributes: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["scheme"] = "https"
            });

        Assert.Equal("shell.external_open", authorizationContext.CapabilityId);
        Assert.Equal("host-bridge", authorizationContext.SourceComponent);
        Assert.Equal("open-external", authorizationContext.RequestedAction);
        Assert.Equal("https", authorizationContext.Attributes!["scheme"]);
    }

    [Fact]
    public void Evaluate_without_policy_defaults_to_allow()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.NotificationShow);
        var authorizationContext = evaluator.CreateAuthorizationContext(requestContext);

        var decision = evaluator.Evaluate(policy: null, policyV2: null, authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Allow, decision.Kind);
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_legacy_overload_preserves_compatibility()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.NotificationShow);

        var decision = evaluator.Evaluate(
            policy: new InlineLegacyPolicy(WebViewHostCapabilityDecision.Deny("legacy-overload-denied")),
            requestContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Deny, decision.Kind);
        Assert.Equal("legacy-overload-denied", decision.Reason);
    }

    [Fact]
    public void Evaluate_legacy_policy_allow_maps_to_allow()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.NotificationShow);
        var authorizationContext = evaluator.CreateAuthorizationContext(requestContext);

        var decision = evaluator.Evaluate(
            policy: new InlineLegacyPolicy(WebViewHostCapabilityDecision.Allow()),
            policyV2: null,
            authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Allow, decision.Kind);
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_legacy_policy_deny_maps_reason()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.NotificationShow);
        var authorizationContext = evaluator.CreateAuthorizationContext(requestContext);

        var decision = evaluator.Evaluate(
            policy: new InlineLegacyPolicy(WebViewHostCapabilityDecision.Deny("blocked-by-legacy-policy")),
            policyV2: null,
            authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Deny, decision.Kind);
        Assert.Equal("blocked-by-legacy-policy", decision.Reason);
    }

    [Fact]
    public void Evaluate_v2_scheme_constraint_denies_mismatched_scheme()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.ExternalOpen,
            new Uri("http://example.com/docs"));
        var authorizationContext = evaluator.CreateAuthorizationContext(requestContext);

        var decision = evaluator.Evaluate(
            policy: null,
            policyV2: new InlineV2Policy(WebViewCapabilityPolicyDecision.AllowWithConstraint(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["allowedSchemes"] = "https"
                })),
            authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Deny, decision.Kind);
        Assert.Equal("capability-constraint-scheme-denied", decision.Reason);
    }

    [Fact]
    public void Evaluate_v2_menu_constraint_denies_menu_target_when_disabled()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.MenuApplyModel);
        var authorizationContext = evaluator.CreateAuthorizationContext(
            requestContext,
            requestedAction: "apply-menu-model",
            attributes: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["target"] = "menu"
            });

        var decision = evaluator.Evaluate(
            policy: null,
            policyV2: new InlineV2Policy(WebViewCapabilityPolicyDecision.AllowWithConstraint(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["allowMenu"] = "false"
                })),
            authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Deny, decision.Kind);
        Assert.Equal("capability-constraint-menu-denied", decision.Reason);
    }

    [Fact]
    public void Evaluate_v2_tray_constraint_denies_tray_target_when_disabled()
    {
        var evaluator = new WebViewCapabilityPolicyEvaluator();
        var requestContext = new WebViewHostCapabilityRequestContext(
            Guid.NewGuid(),
            null,
            null,
            WebViewHostCapabilityOperation.TrayUpdateState);
        var authorizationContext = evaluator.CreateAuthorizationContext(
            requestContext,
            requestedAction: "update-tray-state",
            attributes: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["target"] = "tray"
            });

        var decision = evaluator.Evaluate(
            policy: null,
            policyV2: new InlineV2Policy(WebViewCapabilityPolicyDecision.AllowWithConstraint(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["allowTray"] = "false"
                })),
            authorizationContext);

        Assert.Equal(WebViewCapabilityPolicyDecisionKind.Deny, decision.Kind);
        Assert.Equal("capability-constraint-tray-denied", decision.Reason);
    }

    [Fact]
    public void ShellSystemIntegrationRuntime_ctor_throws_on_null_executor()
    {
        Assert.Throws<ArgumentNullException>(() => new ShellSystemIntegrationRuntime(null!));
    }

    [Fact]
    public void Describe_throws_for_unregistered_operation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebViewCapabilityPolicyEvaluator.Describe((WebViewHostCapabilityOperation)999));
    }

    private sealed class InlineV2Policy : IWebViewHostCapabilityPolicyV2
    {
        private readonly WebViewCapabilityPolicyDecision _decision;

        public InlineV2Policy(WebViewCapabilityPolicyDecision decision)
        {
            _decision = decision;
        }

        public WebViewCapabilityPolicyDecision Evaluate(in WebViewCapabilityAuthorizationContext context)
            => _decision;
    }

    private sealed class InlineLegacyPolicy : IWebViewHostCapabilityPolicy
    {
        private readonly WebViewHostCapabilityDecision _decision;

        public InlineLegacyPolicy(WebViewHostCapabilityDecision decision)
        {
            _decision = decision;
        }

        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => _decision;
    }
}
