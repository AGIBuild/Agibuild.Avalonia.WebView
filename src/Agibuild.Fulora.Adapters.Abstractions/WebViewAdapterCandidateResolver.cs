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

    private sealed record RejectedProvider(string Id, string Reason);

    private sealed record ResolutionState(
        IReadOnlyList<Candidate> Candidates,
        IReadOnlyList<RejectedProvider> Rejections);

    public static bool HasCandidates(
        IEnumerable<IWebViewPlatformProvider> providers,
        IEnumerable<WebViewAdapterRegistration> legacyRegistrations)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(legacyRegistrations);

        return Evaluate(providers, legacyRegistrations).Candidates.Count > 0;
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

        var resolution = Evaluate(providers, legacyRegistrations);
        var candidate = resolution.Candidates
            .OrderByDescending(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Source)
            .ThenBy(static candidate => candidate.StableId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (candidate is null)
        {
            adapter = null;
            failureReason = FormatFailure(noCandidateReason, resolution.Rejections);
            return false;
        }

        adapter = candidate.Factory();
        failureReason = null;
        return true;
    }

    private static ResolutionState Evaluate(
        IEnumerable<IWebViewPlatformProvider> providers,
        IEnumerable<WebViewAdapterRegistration> legacyRegistrations)
    {
        var candidates = new List<Candidate>();
        var rejections = new List<RejectedProvider>();

        foreach (var provider in providers)
        {
            var probe = provider.ProbeCurrentPlatform();
            if (probe.IsAvailable)
            {
                candidates.Add(new Candidate(
                    provider.Id,
                    provider.Priority,
                    CandidateSource.Provider,
                    provider.CreateAdapter));
            }
            else
            {
                rejections.Add(new RejectedProvider(provider.Id, probe.UnavailableReason!));
            }
        }

        foreach (var registration in legacyRegistrations)
        {
            candidates.Add(new Candidate(
                registration.AdapterId,
                registration.Priority,
                CandidateSource.Legacy,
                registration.Factory));
        }

        return new ResolutionState(candidates, rejections);
    }

    private static string FormatFailure(
        string noCandidateReason,
        IReadOnlyList<RejectedProvider> rejections)
    {
        if (rejections.Count == 0)
        {
            return noCandidateReason;
        }

        var details = rejections
            .OrderBy(static rejection => rejection.Id, StringComparer.Ordinal)
            .Select(static rejection => $"{rejection.Id}: {rejection.Reason}");

        return noCandidateReason + Environment.NewLine + string.Join(Environment.NewLine, details);
    }
}
