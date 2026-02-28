using System.Text.Json;

namespace Agibuild.Fulora.Testing;

/// <summary>
/// Shared semantic assertion helpers for governance tests. Validates structured invariants
/// from JSON artifacts, source evidence linkage, and set consistency.
/// All failures throw <see cref="GovernanceInvariantViolationException"/> with deterministic diagnostics.
/// </summary>
public static class GovernanceAssertionHelper
{
    public static JsonDocument LoadJsonArtifact(string path, string invariantId)
    {
        if (!File.Exists(path))
            throw new GovernanceInvariantViolationException(invariantId, path, "file exists", "file not found");

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    public static JsonElement RequireProperty(JsonElement element, string propertyName, string invariantId, string artifactPath)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw new GovernanceInvariantViolationException(
                invariantId, artifactPath,
                $"property '{propertyName}' present",
                "property missing");
        }

        return value;
    }

    public static int RequireVersionField(JsonElement root, string invariantId, string artifactPath, int? minimumVersion = null)
    {
        var versionElement = RequireProperty(root, "version", invariantId, artifactPath);
        var version = versionElement.GetInt32();
        if (minimumVersion.HasValue && version < minimumVersion.Value)
        {
            throw new GovernanceInvariantViolationException(
                invariantId, artifactPath,
                $"version >= {minimumVersion.Value}",
                $"version = {version}");
        }

        return version;
    }

    public static HashSet<string> ExtractStringIds(JsonElement array, string idPropertyName)
    {
        return array.EnumerateArray()
            .Select(x => x.GetProperty(idPropertyName).GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    public static void AssertContainsAll(
        IReadOnlySet<string> actualSet,
        IEnumerable<string> requiredIds,
        string invariantId,
        string artifactPath)
    {
        var missing = requiredIds.Where(id => !actualSet.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            throw new GovernanceInvariantViolationException(
                invariantId, artifactPath,
                $"all required IDs present",
                $"missing: [{string.Join(", ", missing)}]");
        }
    }

    public static void AssertBidirectionalSync(
        IReadOnlySet<string> setA, string nameA,
        IReadOnlySet<string> setB, string nameB,
        string invariantId,
        Func<string, bool>? filter = null)
    {
        var filteredA = filter is null ? setA : setA.Where(filter).ToHashSet(StringComparer.Ordinal);
        var filteredB = filter is null ? setB : setB.Where(filter).ToHashSet(StringComparer.Ordinal);

        var onlyInA = filteredA.Except(filteredB, StringComparer.Ordinal).ToList();
        var onlyInB = filteredB.Except(filteredA, StringComparer.Ordinal).ToList();

        if (onlyInA.Count > 0 || onlyInB.Count > 0)
        {
            var details = new List<string>();
            if (onlyInA.Count > 0)
                details.Add($"only in {nameA}: [{string.Join(", ", onlyInA)}]");
            if (onlyInB.Count > 0)
                details.Add($"only in {nameB}: [{string.Join(", ", onlyInB)}]");

            throw new GovernanceInvariantViolationException(
                invariantId, $"{nameA} ↔ {nameB}",
                "bidirectional set equality",
                string.Join("; ", details));
        }
    }

    public static void AssertEvidenceLinkage(
        string repoRoot,
        string relativeFile,
        string testMethod,
        string invariantId)
    {
        var sourcePath = Path.Combine(repoRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new GovernanceInvariantViolationException(
                invariantId, sourcePath,
                "evidence source file exists",
                "file not found");
        }

        var source = File.ReadAllText(sourcePath);
        if (!source.Contains(testMethod, StringComparison.Ordinal))
        {
            throw new GovernanceInvariantViolationException(
                invariantId, sourcePath,
                $"test method '{testMethod}' in source",
                "method not found");
        }
    }

    public static void AssertEvidenceItems(
        JsonElement evidenceArray,
        string repoRoot,
        string invariantId)
    {
        var items = evidenceArray.EnumerateArray().ToList();
        if (items.Count == 0)
        {
            throw new GovernanceInvariantViolationException(
                invariantId, "evidence array",
                "at least one evidence item",
                "empty array");
        }

        foreach (var item in items)
        {
            var file = item.GetProperty("file").GetString()!;
            var testMethod = item.GetProperty("testMethod").GetString()!;
            AssertEvidenceLinkage(repoRoot, file, testMethod, invariantId);
        }
    }

    public static void AssertControlledVocabulary(
        IEnumerable<string> tokens,
        IReadOnlySet<string> vocabulary,
        string invariantId,
        string context)
    {
        var invalid = tokens.Where(t => !vocabulary.Contains(t)).Distinct(StringComparer.Ordinal).ToList();
        if (invalid.Count > 0)
        {
            throw new GovernanceInvariantViolationException(
                invariantId, context,
                $"tokens in [{string.Join(", ", vocabulary)}]",
                $"invalid tokens: [{string.Join(", ", invalid)}]");
        }
    }

    public static void AssertSourceContains(
        string source,
        string expected,
        string invariantId,
        string artifactPath)
    {
        if (!source.Contains(expected, StringComparison.Ordinal))
        {
            var truncated = expected.Length > 80 ? expected[..80] + "..." : expected;
            throw new GovernanceInvariantViolationException(
                invariantId, artifactPath,
                $"source contains '{truncated}'",
                "not found");
        }
    }

    public static void AssertFileExists(string path, string invariantId)
    {
        if (!File.Exists(path))
        {
            throw new GovernanceInvariantViolationException(
                invariantId, path,
                "file exists",
                "file not found");
        }
    }

    public static JsonElement RequireTransitionGateDiagnostics(JsonElement root, string invariantId, string artifactPath)
    {
        var diagnostics = RequireProperty(root, "diagnostics", invariantId, artifactPath);
        if (diagnostics.ValueKind != JsonValueKind.Array || diagnostics.GetArrayLength() == 0)
        {
            throw new GovernanceInvariantViolationException(
                invariantId,
                artifactPath,
                "non-empty diagnostics array",
                diagnostics.ValueKind == JsonValueKind.Array ? "empty array" : diagnostics.ValueKind.ToString());
        }

        return diagnostics;
    }

    public static void AssertTransitionGateDiagnostic(JsonElement diagnostic, string invariantId, string artifactPath)
    {
        static string ReadRequiredString(JsonElement node, string propertyName, string invariantIdValue, string artifactPathValue)
        {
            if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                throw new GovernanceInvariantViolationException(
                    invariantIdValue,
                    artifactPathValue,
                    $"property '{propertyName}' as non-empty string",
                    "property missing or not string");
            }

            var value = property.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new GovernanceInvariantViolationException(
                    invariantIdValue,
                    artifactPathValue,
                    $"property '{propertyName}' as non-empty string",
                    "value empty");
            }

            return value!;
        }

        _ = ReadRequiredString(diagnostic, "invariantId", invariantId, artifactPath);
        _ = ReadRequiredString(diagnostic, "lane", invariantId, artifactPath);
        _ = ReadRequiredString(diagnostic, "artifactPath", invariantId, artifactPath);
        _ = ReadRequiredString(diagnostic, "expected", invariantId, artifactPath);
        _ = ReadRequiredString(diagnostic, "actual", invariantId, artifactPath);
    }

    public static JsonElement RequireReleaseDecision(JsonElement root, string invariantId, string artifactPath)
    {
        var decision = RequireProperty(root, "releaseDecision", invariantId, artifactPath);
        if (decision.ValueKind != JsonValueKind.Object)
        {
            throw new GovernanceInvariantViolationException(
                invariantId,
                artifactPath,
                "releaseDecision object present",
                $"releaseDecision kind = {decision.ValueKind}");
        }

        if (!decision.TryGetProperty("state", out var stateNode) || stateNode.ValueKind != JsonValueKind.String)
        {
            throw new GovernanceInvariantViolationException(
                invariantId,
                artifactPath,
                "releaseDecision.state as non-empty string",
                "missing or not string");
        }

        var state = stateNode.GetString();
        if (!string.Equals(state, "ready", StringComparison.Ordinal)
            && !string.Equals(state, "blocked", StringComparison.Ordinal))
        {
            throw new GovernanceInvariantViolationException(
                invariantId,
                artifactPath,
                "releaseDecision.state in {ready, blocked}",
                $"releaseDecision.state = {state ?? "<null>"}");
        }

        return decision;
    }

    public static JsonElement RequireReleaseBlockingReasons(JsonElement root, string invariantId, string artifactPath)
    {
        var reasons = RequireProperty(root, "releaseBlockingReasons", invariantId, artifactPath);
        if (reasons.ValueKind != JsonValueKind.Array)
        {
            throw new GovernanceInvariantViolationException(
                invariantId,
                artifactPath,
                "releaseBlockingReasons as array",
                $"releaseBlockingReasons kind = {reasons.ValueKind}");
        }

        return reasons;
    }

    public static void AssertReleaseBlockingReason(JsonElement reason, string invariantId, string artifactPath)
    {
        static string ReadRequiredString(JsonElement node, string propertyName, string invariantIdValue, string artifactPathValue)
        {
            if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                throw new GovernanceInvariantViolationException(
                    invariantIdValue,
                    artifactPathValue,
                    $"property '{propertyName}' as non-empty string",
                    "property missing or not string");
            }

            var value = property.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new GovernanceInvariantViolationException(
                    invariantIdValue,
                    artifactPathValue,
                    $"property '{propertyName}' as non-empty string",
                    "value empty");
            }

            return value!;
        }

        _ = ReadRequiredString(reason, "category", invariantId, artifactPath);
        _ = ReadRequiredString(reason, "invariantId", invariantId, artifactPath);
        _ = ReadRequiredString(reason, "sourceArtifact", invariantId, artifactPath);
        _ = ReadRequiredString(reason, "expected", invariantId, artifactPath);
        _ = ReadRequiredString(reason, "actual", invariantId, artifactPath);
    }
}

/// <summary>
/// Thrown when a governance invariant is violated. Carries structured diagnostic metadata
/// for deterministic CI failure triage.
/// </summary>
public sealed class GovernanceInvariantViolationException : Exception
{
    public string InvariantId { get; }
    public string ArtifactPath { get; }
    public string Expected { get; }
    public string Actual { get; }

    public GovernanceInvariantViolationException(
        string invariantId,
        string artifactPath,
        string expected,
        string actual)
        : base($"[{invariantId}] Governance invariant violated at '{artifactPath}'. Expected: {expected}. Actual: {actual}.")
    {
        InvariantId = invariantId;
        ArtifactPath = artifactPath;
        Expected = expected;
        Actual = actual;
    }
}
