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

    private sealed class MarkerAdapter(string id) : MockWebViewAdapter
    {
        public string Id { get; } = id;
    }
}
