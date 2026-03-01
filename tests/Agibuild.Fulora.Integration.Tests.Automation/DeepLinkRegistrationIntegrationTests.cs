using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Integration smoke tests for deep-link native registration pipeline.
/// These verify end-to-end flow from platform entrypoint through normalization,
/// policy, idempotency, and orchestration dispatch.
/// </summary>
public sealed class DeepLinkRegistrationIntegrationTests
{
    private static string UniqueAppId() => $"it-deeplink-{Guid.NewGuid():N}";

    [Fact]
    public async Task Protocol_activation_end_to_end_dispatches_to_primary()
    {
        var appId = UniqueAppId();
        WebViewShellActivationRequest? delivered = null;

        using var primary = WebViewShellActivationCoordinator.Register(appId, (req, _) =>
        {
            delivered = req;
            return Task.CompletedTask;
        });

        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await DeepLinkPlatformEntrypoint.HandlePlatformActivationAsync(
            service, "myapp://host/integration-test",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, result.Status);
        Assert.NotNull(delivered);
        Assert.Equal("myapp", delivered!.DeepLinkUri.Scheme);
        Assert.Contains("deeplink.route", delivered.Metadata.Keys);
    }

    [Fact]
    public async Task Policy_deny_blocks_dispatch_in_integration_flow()
    {
        var appId = UniqueAppId();
        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        var policy = new TestDenyPolicy();
        var service = new DeepLinkRegistrationService(policy);
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "blocked"));

        var result = await DeepLinkPlatformEntrypoint.HandlePlatformActivationAsync(
            service, "blocked://host/test",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.PolicyDenied, result.Status);
    }

    [Fact]
    public async Task Duplicate_ingress_suppresses_second_dispatch()
    {
        var appId = UniqueAppId();
        var dispatchCount = 0;

        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) =>
        {
            System.Threading.Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });

        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var r1 = await service.IngestActivationAsync(
            new Uri("myapp://host/dedup?x=1"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        var r2 = await service.IngestActivationAsync(
            new Uri("myapp://host/dedup?x=1"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, r1.Status);
        Assert.Equal(DeepLinkActivationIngressStatus.Duplicate, r2.Status);
        Assert.Equal(1, dispatchCount);
    }

    [Fact]
    public async Task Diagnostics_emitted_during_integration_flow()
    {
        var appId = UniqueAppId();
        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        var service = new DeepLinkRegistrationService();
        var events = new List<DeepLinkDiagnosticEventArgs>();
        service.DiagnosticEvent += (_, e) => events.Add(e);
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        await service.IngestActivationAsync(
            new Uri("myapp://host/diag-it"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.True(events.Count >= 3);
        Assert.All(events, e => Assert.NotEqual(Guid.Empty, e.CorrelationId));
    }

    [Fact]
    public void All_platform_descriptors_report_supported()
    {
        foreach (var descriptor in DeepLinkPlatformEntryDescriptor.All)
        {
            Assert.Equal(DeepLinkPlatformSupport.Supported, descriptor.Support);
            Assert.NotNull(descriptor.EntrypointDescription);
        }
    }

    private sealed class TestDenyPolicy : IDeepLinkAdmissionPolicy
    {
        public DeepLinkAdmissionDecision Evaluate(DeepLinkActivationEnvelope envelope)
            => DeepLinkAdmissionDecision.Deny("integration-test-deny");
    }
}
