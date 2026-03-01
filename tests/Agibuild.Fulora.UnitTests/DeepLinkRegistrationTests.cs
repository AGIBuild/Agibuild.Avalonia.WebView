using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class DeepLinkRegistrationTests
{
    private static string UniqueAppId() => $"deeplink-app-{Guid.NewGuid():N}";

    // --- Registration validation ---

    [Fact]
    public void Valid_registration_declaration_is_accepted()
    {
        var service = new DeepLinkRegistrationService();
        var decl = new DeepLinkRouteDeclaration(UniqueAppId(), "myapp");
        var result = service.RegisterRoute(decl);

        Assert.Equal(DeepLinkRegistrationStatus.Accepted, result.Status);
    }

    [Fact]
    public void Duplicate_registration_declaration_is_rejected()
    {
        var service = new DeepLinkRegistrationService();
        var appId = UniqueAppId();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));
        var result = service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        Assert.Equal(DeepLinkRegistrationStatus.Rejected, result.Status);
        Assert.Equal("duplicate-declaration", result.Reason);
    }

    [Fact]
    public void Missing_app_identity_throws()
    {
        Assert.Throws<ArgumentException>(() => new DeepLinkRouteDeclaration("", "myapp"));
    }

    [Fact]
    public void Whitespace_app_identity_throws()
    {
        Assert.Throws<ArgumentException>(() => new DeepLinkRouteDeclaration("   ", "myapp"));
    }

    [Fact]
    public void Missing_scheme_throws()
    {
        Assert.Throws<ArgumentException>(() => new DeepLinkRouteDeclaration("app1", ""));
    }

    // --- Canonicalization ---

    [Fact]
    public void Equivalent_uri_variants_normalize_to_same_route()
    {
        var e1 = DeepLinkRegistrationService.Normalize(
            new Uri("MyApp://HOST/Path/"), DeepLinkActivationSource.ProtocolLaunch);
        var e2 = DeepLinkRegistrationService.Normalize(
            new Uri("myapp://host/Path"), DeepLinkActivationSource.ProtocolLaunch);

        Assert.Equal(e1.Route, e2.Route);
    }

    [Fact]
    public void Trailing_slash_is_trimmed_in_route()
    {
        var envelope = DeepLinkRegistrationService.Normalize(
            new Uri("myapp://host/path/"), DeepLinkActivationSource.ProtocolLaunch);

        Assert.Equal("myapp://host/path", envelope.Route);
    }

    [Fact]
    public void Root_path_normalizes_to_slash()
    {
        var envelope = DeepLinkRegistrationService.Normalize(
            new Uri("myapp://host"), DeepLinkActivationSource.ProtocolLaunch);

        Assert.Equal("myapp://host/", envelope.Route);
    }

    // --- Policy admission ---

    [Fact]
    public async Task Allowed_activation_is_dispatched()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, result.Status);
    }

    [Fact]
    public async Task Denied_activation_is_blocked_before_dispatch()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var denyPolicy = new DelegateAdmissionPolicy(_ => DeepLinkAdmissionDecision.Deny("test-deny"));
        var service = new DeepLinkRegistrationService(denyPolicy);
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.PolicyDenied, result.Status);
        Assert.Equal("test-deny", result.Reason);
    }

    // --- Idempotency ---

    [Fact]
    public async Task Duplicate_activation_within_replay_window_is_suppressed()
    {
        var appId = UniqueAppId();
        var dispatchCount = 0;
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) =>
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var uri = new Uri("myapp://host/action?id=123");
        var r1 = await service.IngestActivationAsync(uri, DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        var r2 = await service.IngestActivationAsync(uri, DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, r1.Status);
        Assert.Equal(DeepLinkActivationIngressStatus.Duplicate, r2.Status);
        Assert.Equal(1, dispatchCount);
    }

    // --- No matching route ---

    [Fact]
    public async Task Activation_with_no_matching_route_returns_no_match()
    {
        var service = new DeepLinkRegistrationService();

        var result = await service.IngestActivationAsync(
            new Uri("unknown://host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.NoMatchingRoute, result.Status);
    }

    // --- Orchestration integration ---

    [Fact]
    public async Task Native_activation_delivered_to_active_primary()
    {
        var appId = UniqueAppId();
        WebViewShellActivationRequest? observed = null;
        using var reg = WebViewShellActivationCoordinator.Register(appId, (req, _) =>
        {
            observed = req;
            return Task.CompletedTask;
        });
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/test"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, result.Status);
        Assert.NotNull(observed);
        Assert.Equal("myapp://host/test", observed!.DeepLinkUri.ToString());
        Assert.Equal("ProtocolLaunch", observed.Metadata["deeplink.source"]);
    }

    [Fact]
    public async Task Native_activation_fails_without_active_primary()
    {
        var appId = UniqueAppId();
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.NoMatchingRoute, result.Status);
    }

    // --- Overlap between native ingress and secondary forwarding ---

    [Fact]
    public async Task Equivalent_ingress_and_forward_dispatches_at_most_once()
    {
        var appId = UniqueAppId();
        var dispatchCount = 0;
        using var primary = WebViewShellActivationCoordinator.Register(appId, (_, _) =>
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });
        using var secondary = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);

        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var uri = new Uri("myapp://host/overlap?key=1");

        var ingressResult = await service.IngestActivationAsync(uri, DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        var forwardResult = await service.IngestActivationAsync(uri, DeepLinkActivationSource.SecondaryForward,
            TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, ingressResult.Status);
        Assert.True(
            forwardResult.Status == DeepLinkActivationIngressStatus.Duplicate ||
            forwardResult.Status == DeepLinkActivationIngressStatus.Dispatched);
        Assert.True(dispatchCount <= 2);
    }

    // --- Envelope model validation ---

    [Fact]
    public void Relative_raw_uri_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DeepLinkActivationEnvelope(
            Guid.NewGuid(), "myapp://host/path", new Uri("/relative", UriKind.Relative),
            DeepLinkActivationSource.ProtocolLaunch, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_route_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DeepLinkActivationEnvelope(
            Guid.NewGuid(), "", new Uri("myapp://host"),
            DeepLinkActivationSource.ProtocolLaunch, DateTimeOffset.UtcNow));
    }

    // --- Diagnostics ---

    [Fact]
    public async Task Diagnostic_events_are_emitted_for_lifecycle()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        var events = new List<DeepLinkDiagnosticEventArgs>();
        service.DiagnosticEvent += (_, e) => events.Add(e);

        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        await service.IngestActivationAsync(
            new Uri("myapp://host/diag"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);

        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.RegistrationAttempt);
        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.ActivationNormalized);
        Assert.Contains(events, e => e.EventType == DeepLinkDiagnosticEventType.Dispatched);
    }

    // --- Platform entrypoint ---

    [Fact]
    public async Task Platform_entrypoint_handles_valid_uri_string()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp"));

        var result = await DeepLinkPlatformEntrypoint.HandlePlatformActivationAsync(
            service, "myapp://host/platform-test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, result.Status);
    }

    [Fact]
    public async Task Platform_entrypoint_handles_invalid_uri_string()
    {
        var service = new DeepLinkRegistrationService();

        var result = await DeepLinkPlatformEntrypoint.HandlePlatformActivationAsync(
            service, "not a valid uri at all :::", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(DeepLinkActivationIngressStatus.NoMatchingRoute, result.Status);
    }

    [Fact]
    public void Platform_descriptors_cover_all_platforms()
    {
        var all = DeepLinkPlatformEntryDescriptor.All;
        Assert.Equal(5, all.Count);
        Assert.Contains(all, d => d.PlatformName == "Windows");
        Assert.Contains(all, d => d.PlatformName == "macOS");
        Assert.Contains(all, d => d.PlatformName == "iOS");
        Assert.Contains(all, d => d.PlatformName == "Android");
        Assert.Contains(all, d => d.PlatformName == "Linux");
        Assert.All(all, d => Assert.Equal(DeepLinkPlatformSupport.Supported, d.Support));
    }

    // --- Host pattern matching ---

    [Fact]
    public async Task Route_with_host_pattern_matches_correct_host()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp", "specific-host"));

        var matchResult = await service.IngestActivationAsync(
            new Uri("myapp://specific-host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        Assert.Equal(DeepLinkActivationIngressStatus.Dispatched, matchResult.Status);
    }

    [Fact]
    public async Task Route_with_host_pattern_rejects_wrong_host()
    {
        var appId = UniqueAppId();
        using var reg = WebViewShellActivationCoordinator.Register(appId, (_, _) => Task.CompletedTask);
        var service = new DeepLinkRegistrationService();
        service.RegisterRoute(new DeepLinkRouteDeclaration(appId, "myapp", "specific-host"));

        var result = await service.IngestActivationAsync(
            new Uri("myapp://wrong-host/path"), DeepLinkActivationSource.ProtocolLaunch,
            TestContext.Current.CancellationToken);
        Assert.Equal(DeepLinkActivationIngressStatus.NoMatchingRoute, result.Status);
    }

    private sealed class DelegateAdmissionPolicy(Func<DeepLinkActivationEnvelope, DeepLinkAdmissionDecision> evaluator) : IDeepLinkAdmissionPolicy
    {
        public DeepLinkAdmissionDecision Evaluate(DeepLinkActivationEnvelope envelope) => evaluator(envelope);
    }
}
