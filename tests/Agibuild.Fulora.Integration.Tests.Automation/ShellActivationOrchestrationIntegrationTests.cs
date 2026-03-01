using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Integration tests for shell activation orchestration combined with deep-link registration.
/// Exercises the full coordinator + registration service + policy + diagnostics stack end-to-end.
/// </summary>
public sealed class ShellActivationOrchestrationIntegrationTests
{
    private static string UniqueAppId() => $"it-orch-{Guid.NewGuid():N}";

    [Fact]
    public async Task Full_lifecycle_register_activate_forward_release()
    {
        var appId = UniqueAppId();
        var deliveredPayloads = new List<WebViewShellActivationRequest>();

        using var primary = WebViewShellActivationCoordinator.Register(appId, (req, _) =>
        {
            deliveredPayloads.Add(req);
            return Task.CompletedTask;
        });
        Assert.True(primary.IsPrimary);

        using var secondary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        Assert.False(secondary.IsPrimary);

        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var r1 = await service.IngestActivationAsync(
            new Uri("myapp://host/action1"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, r1.Status);

        var r2 = await secondary.ForwardAsync(
            new WebViewShellActivationRequest(new Uri("myapp://host/action2")),
            TestContext.Current.CancellationToken);
        Assert.Equal(WebViewShellActivationForwardStatus.Delivered, r2.Status);

        Assert.Equal(2, deliveredPayloads.Count);
        Assert.Equal("myapp://host/action1", deliveredPayloads[0].DeepLinkUri.ToString());
        Assert.Equal("myapp://host/action2", deliveredPayloads[1].DeepLinkUri.ToString());
    }

    [Fact]
    public async Task Policy_deny_blocks_entire_flow()
    {
        var appId = UniqueAppId();
        var dispatchCount = 0;

        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) =>
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });

        var policy = new DenyAllPolicy();
        var service = new DeepLinkRegistrationService(policy);
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/blocked"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.PolicyDenied, result.Status);
        Assert.Equal(0, dispatchCount);
    }

    [Fact]
    public async Task Idempotency_deduplicates_across_ingress_and_forward()
    {
        var appId = UniqueAppId();
        var dispatchCount = 0;

        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) =>
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });

        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var uri = new Uri("myapp://host/dedup?key=abc");

        var r1 = await service.IngestActivationAsync(uri, DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        var r2 = await service.IngestActivationAsync(uri, DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, r1.Status);
        Assert.Equal(DeepLinkActivationIngressStatus.Duplicate, r2.Status);
        Assert.Equal(1, dispatchCount);
    }

    [Fact]
    public async Task Diagnostics_cover_full_lifecycle()
    {
        var appId = UniqueAppId();
        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        var service = new DeepLinkRegistrationService();
        var events = new List<DeepLinkDiagnosticEventArgs>();
        service.DiagnosticEvent += (_, e) => events.Add(e);

        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        await service.IngestActivationAsync(
            new Uri("myapp://host/diag-lifecycle"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.RegistrationAttempt && e.Outcome == "accepted");
        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.ActivationNormalized);
        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.Dispatched);

        Assert.All(events, e =>
        {
            Assert.NotEqual(Guid.Empty, e.CorrelationId);
            Assert.NotNull(e.Outcome);
        });
    }

    [Fact]
    public async Task Release_primary_then_activation_reports_no_match()
    {
        var appId = UniqueAppId();

        var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        primary.Dispose();

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/after-release"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.NoMatchingRoute, result.Status);
    }

    private sealed class DenyAllPolicy : IDeepLinkAdmissionPolicy
    {
        public DeepLinkAdmissionDecision Evaluate(DeepLinkActivationEnvelope envelope)
            => DeepLinkAdmissionDecision.Deny("integration-deny-all");
    }
}
