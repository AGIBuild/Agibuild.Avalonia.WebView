using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewAdapterCandidateResolverTests
{
    [Fact]
    public void TryCreateAdapter_prefers_highest_priority_candidate_across_provider_and_legacy_sources()
    {
        var providers = new[]
        {
            new StubPlatformProvider("provider-low", priority: 10, () => new MarkerAdapter("provider-low"))
        };

        var legacyRegistrations = new[]
        {
            new WebViewAdapterRegistration(
                WebViewAdapterPlatform.Gtk,
                "legacy-high",
                () => new MarkerAdapter("legacy-high"),
                Priority: 100)
        };

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            providers,
            legacyRegistrations,
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.True(result);
        Assert.Null(failureReason);
        Assert.Equal("legacy-high", Assert.IsType<MarkerAdapter>(adapter).Id);
    }

    [Fact]
    public void TryCreateAdapter_prefers_provider_before_legacy_when_priorities_match()
    {
        var providers = new[]
        {
            new StubPlatformProvider("provider", priority: 50, () => new MarkerAdapter("provider"))
        };

        var legacyRegistrations = new[]
        {
            new WebViewAdapterRegistration(
                WebViewAdapterPlatform.Gtk,
                "legacy",
                () => new MarkerAdapter("legacy"),
                Priority: 50)
        };

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            providers,
            legacyRegistrations,
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.True(result);
        Assert.Null(failureReason);
        Assert.Equal("provider", Assert.IsType<MarkerAdapter>(adapter).Id);
    }

    [Fact]
    public void TryCreateAdapter_uses_deterministic_secondary_order_when_priorities_match_within_same_source()
    {
        var providers = new IWebViewPlatformProvider[]
        {
            new StubPlatformProvider("zeta", priority: 25, () => new MarkerAdapter("zeta")),
            new StubPlatformProvider("alpha", priority: 25, () => new MarkerAdapter("alpha"))
        };

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            providers,
            [],
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.True(result);
        Assert.Null(failureReason);
        Assert.Equal("alpha", Assert.IsType<MarkerAdapter>(adapter).Id);
    }

    [Fact]
    public void TryCreateAdapter_returns_failure_reason_when_no_candidates_exist()
    {
        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            [],
            [],
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.False(result);
        Assert.Null(adapter);
        Assert.Equal("no candidates", failureReason);
    }

    [Fact]
    public void TryCreateAdapter_skips_higher_priority_unavailable_provider()
    {
        var unavailable = new InstrumentedPlatformProvider(
            "high-unavailable",
            priority: 100,
            WebViewPlatformProbeResult.RuntimeUnavailable("runtime missing"),
            () => new MarkerAdapter("high-unavailable"));
        var available = new InstrumentedPlatformProvider(
            "low-available",
            priority: 1,
            WebViewPlatformProbeResult.Available(),
            () => new MarkerAdapter("low-available"));

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            [unavailable, available],
            [],
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.True(result);
        Assert.Null(failureReason);
        Assert.Equal("low-available", Assert.IsType<MarkerAdapter>(adapter).Id);
        Assert.Equal(0, unavailable.FactoryCount);
        Assert.Equal(1, available.FactoryCount);
    }

    [Fact]
    public void TryCreateAdapter_does_not_invoke_unavailable_provider_factory()
    {
        var unavailable = new InstrumentedPlatformProvider(
            "gone",
            priority: 50,
            WebViewPlatformProbeResult.UnsupportedPlatform("not this OS"),
            () => throw new InvalidOperationException("factory must not run"));

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            [unavailable],
            [],
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.False(result);
        Assert.Null(adapter);
        Assert.Equal(0, unavailable.FactoryCount);
        Assert.Contains("gone: not this OS", failureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void TryCreateAdapter_probes_each_provider_once()
    {
        var first = new InstrumentedPlatformProvider(
            "first",
            priority: 10,
            WebViewPlatformProbeResult.Available(),
            () => new MarkerAdapter("first"));
        var second = new InstrumentedPlatformProvider(
            "second",
            priority: 5,
            WebViewPlatformProbeResult.RuntimeUnavailable("missing runtime"),
            () => new MarkerAdapter("second"));

        WebViewAdapterCandidateResolver.TryCreateAdapter(
            [first, second],
            [],
            "no candidates",
            out _,
            out _);

        Assert.Equal(1, first.ProbeCount);
        Assert.Equal(1, second.ProbeCount);
        Assert.Equal(1, first.FactoryCount);
        Assert.Equal(0, second.FactoryCount);
    }

    [Fact]
    public void TryCreateAdapter_reports_rejection_reasons_in_ordinal_id_order()
    {
        var zeta = new InstrumentedPlatformProvider(
            "zeta",
            priority: 2,
            WebViewPlatformProbeResult.RuntimeUnavailable("zeta runtime"),
            () => new MarkerAdapter("zeta"));
        var alpha = new InstrumentedPlatformProvider(
            "alpha",
            priority: 1,
            WebViewPlatformProbeResult.UnsupportedPlatform("alpha platform"),
            () => new MarkerAdapter("alpha"));

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            [zeta, alpha],
            [],
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.False(result);
        Assert.Null(adapter);
        Assert.Equal(
            "no candidates" + Environment.NewLine + "alpha: alpha platform" + Environment.NewLine + "zeta: zeta runtime",
            failureReason);
    }

    [Fact]
    public void HasCandidates_returns_false_when_only_unavailable_providers_exist()
    {
        var unavailable = new InstrumentedPlatformProvider(
            "only",
            priority: 10,
            WebViewPlatformProbeResult.RuntimeUnavailable("no runtime"),
            () => new MarkerAdapter("only"));

        Assert.False(WebViewAdapterCandidateResolver.HasCandidates([unavailable], []));
        Assert.Equal(1, unavailable.ProbeCount);
        Assert.Equal(0, unavailable.FactoryCount);
    }

    [Fact]
    public void ProbeCurrentPlatform_default_follows_CanHandleCurrentPlatform()
    {
        IWebViewPlatformProvider supported = new StubPlatformProvider("on", priority: 1, () => new MarkerAdapter("on"));
        IWebViewPlatformProvider unsupported = new UnsupportedStubPlatformProvider("off", priority: 1, () => new MarkerAdapter("off"));

        var available = supported.ProbeCurrentPlatform();
        var rejected = unsupported.ProbeCurrentPlatform();

        Assert.True(available.IsAvailable);
        Assert.True(available.IsPlatformSupported);
        Assert.True(available.IsRuntimeAvailable);
        Assert.Null(available.UnavailableReason);

        Assert.False(rejected.IsAvailable);
        Assert.False(rejected.IsPlatformSupported);
        Assert.False(string.IsNullOrWhiteSpace(rejected.UnavailableReason));
    }

    [Fact]
    public void TryCreateAdapter_keeps_legacy_eligible_when_provider_is_unavailable()
    {
        var unavailable = new InstrumentedPlatformProvider(
            "provider",
            priority: 100,
            WebViewPlatformProbeResult.RuntimeUnavailable("runtime missing"),
            () => new MarkerAdapter("provider"));
        var legacyRegistrations = new[]
        {
            new WebViewAdapterRegistration(
                WebViewAdapterPlatform.Gtk,
                "legacy",
                () => new MarkerAdapter("legacy"),
                Priority: 1)
        };

        var result = WebViewAdapterCandidateResolver.TryCreateAdapter(
            [unavailable],
            legacyRegistrations,
            "no candidates",
            out var adapter,
            out var failureReason);

        Assert.True(result);
        Assert.Null(failureReason);
        Assert.Equal("legacy", Assert.IsType<MarkerAdapter>(adapter).Id);
        Assert.Equal(0, unavailable.FactoryCount);
    }

    private sealed class StubPlatformProvider(
        string id,
        int priority,
        Func<IWebViewAdapter> factory) : IWebViewPlatformProvider
    {
        public string Id => id;
        public int Priority => priority;
        public bool CanHandleCurrentPlatform() => true;
        public IWebViewAdapter CreateAdapter() => factory();
    }

    private sealed class UnsupportedStubPlatformProvider(
        string id,
        int priority,
        Func<IWebViewAdapter> factory) : IWebViewPlatformProvider
    {
        public string Id => id;
        public int Priority => priority;
        public bool CanHandleCurrentPlatform() => false;
        public IWebViewAdapter CreateAdapter() => factory();
    }

    private sealed class InstrumentedPlatformProvider(
        string id,
        int priority,
        WebViewPlatformProbeResult probe,
        Func<IWebViewAdapter> factory) : IWebViewPlatformProvider
    {
        public string Id => id;
        public int Priority => priority;
        public int ProbeCount { get; private set; }
        public int FactoryCount { get; private set; }

        public bool CanHandleCurrentPlatform() => probe.IsAvailable;

        public WebViewPlatformProbeResult ProbeCurrentPlatform()
        {
            ProbeCount++;
            return probe;
        }

        public IWebViewAdapter CreateAdapter()
        {
            FactoryCount++;
            return factory();
        }
    }

    private sealed class MarkerAdapter(string id) : MockWebViewAdapter
    {
        public string Id { get; } = id;
    }
}
