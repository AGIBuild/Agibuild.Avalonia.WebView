namespace Agibuild.Fulora.Adapters.Abstractions;

internal static class WebViewAdapterCandidateResolver
{
    private enum CandidateSource
    {
        Provider = 0,
        Legacy = 1
    }

    private sealed record Candidate(
        string StableId,
        int Priority,
        CandidateSource Source,
        Func<IWebViewAdapter> Factory);

    public static bool HasCandidates(
        IEnumerable<IWebViewPlatformProvider> providers,
        IEnumerable<WebViewAdapterRegistration> legacyRegistrations)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(legacyRegistrations);

        return BuildCandidates(providers, legacyRegistrations).Any();
    }

    public static bool TryCreateAdapter(
        IEnumerable<IWebViewPlatformProvider> providers,
        IEnumerable<WebViewAdapterRegistration> legacyRegistrations,
        string noCandidateReason,
        out IWebViewAdapter? adapter,
        out string? failureReason)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(legacyRegistrations);
        ArgumentException.ThrowIfNullOrWhiteSpace(noCandidateReason);

        var candidate = BuildCandidates(providers, legacyRegistrations)
            .OrderByDescending(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Source)
            .ThenBy(static candidate => candidate.StableId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (candidate is null)
        {
            adapter = null;
            failureReason = noCandidateReason;
            return false;
        }

        adapter = candidate.Factory();
        failureReason = null;
        return true;
    }

    private static IEnumerable<Candidate> BuildCandidates(
        IEnumerable<IWebViewPlatformProvider> providers,
        IEnumerable<WebViewAdapterRegistration> legacyRegistrations)
    {
        foreach (var provider in providers)
        {
            yield return new Candidate(
                provider.Id,
                provider.Priority,
                CandidateSource.Provider,
                provider.CreateAdapter);
        }

        foreach (var registration in legacyRegistrations)
        {
            yield return new Candidate(
                registration.AdapterId,
                registration.Priority,
                CandidateSource.Legacy,
                registration.Factory);
        }
    }
}
