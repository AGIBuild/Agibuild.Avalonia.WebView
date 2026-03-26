using System.Text.Json;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class DocumentationGovernanceTests
{
    private static readonly IReadOnlyDictionary<string, string> RequiredPlatformDocuments = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Product Platform Roadmap"] = "product-platform-roadmap.md",
        ["Architecture Layering"] = "architecture-layering.md",
        ["Platform Status"] = "platform-status.md",
        ["Release Governance"] = "release-governance.md",
        ["Framework Capabilities"] = "framework-capabilities.json"
    };

    [Fact]
    public void Required_platform_documents_exist()
    {
        var repoRoot = FindRepoRoot();
        var requiredFiles = RequiredPlatformDocuments.Values.Select(x => $"docs/{x}").ToArray();

        foreach (var relativePath in requiredFiles)
        {
            var absolutePath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(absolutePath), $"Missing required platform document: {relativePath}");
        }
    }

    [Fact]
    public void Docs_index_exposes_platform_document_entry_set()
    {
        var repoRoot = FindRepoRoot();
        var indexPath = Path.Combine(repoRoot, "docs", "index.md");
        Assert.True(File.Exists(indexPath), "Missing docs/index.md");

        var platformLinks = ParsePlatformDocumentsTableLinks(File.ReadAllLines(indexPath));
        Assert.Equal(RequiredPlatformDocuments.Count, platformLinks.Count);

        foreach (var expected in RequiredPlatformDocuments)
            Assert.Equal(expected.Value, platformLinks[expected.Key]);
    }

    [Fact]
    public void Docs_toc_includes_required_platform_navigation_at_top_level()
    {
        var repoRoot = FindRepoRoot();
        var tocPath = Path.Combine(repoRoot, "docs", "toc.yml");
        Assert.True(File.Exists(tocPath), "Missing docs/toc.yml");

        var topLevelItems = ParseTopLevelTocItems(File.ReadAllLines(tocPath));
        foreach (var expected in RequiredPlatformDocuments)
        {
            Assert.True(
                topLevelItems.TryGetValue(expected.Key, out var href),
                $"Top-level TOC item '{expected.Key}' not found.");
            Assert.Equal(expected.Value, href);
        }
    }

    [Fact]
    public void Platform_document_entries_are_consistent_across_index_toc_and_docfx_content()
    {
        var repoRoot = FindRepoRoot();
        var indexPath = Path.Combine(repoRoot, "docs", "index.md");
        var tocPath = Path.Combine(repoRoot, "docs", "toc.yml");
        var docfxPath = Path.Combine(repoRoot, "docs", "docfx.json");

        var indexLinks = ParsePlatformDocumentsTableLinks(File.ReadAllLines(indexPath));
        var tocLinks = ParseTopLevelTocItems(File.ReadAllLines(tocPath));

        Assert.Equal(indexLinks.Count, RequiredPlatformDocuments.Count);
        Assert.Equal(tocLinks.Count(x => RequiredPlatformDocuments.ContainsKey(x.Key)), RequiredPlatformDocuments.Count);

        foreach (var expected in RequiredPlatformDocuments)
        {
            Assert.Equal(expected.Value, indexLinks[expected.Key]);
            Assert.Equal(expected.Value, tocLinks[expected.Key]);
        }

        using var docfx = JsonDocument.Parse(File.ReadAllText(docfxPath));
        var contentFiles = docfx.RootElement.GetProperty("build").GetProperty("content")
            .EnumerateArray()
            .Where(x => x.TryGetProperty("files", out _))
            .SelectMany(x => x.GetProperty("files").EnumerateArray())
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("*.md", contentFiles);
        Assert.Contains("toc.yml", contentFiles);
        Assert.Contains("framework-capabilities.json", contentFiles);
    }

    [Fact]
    public void Docfx_build_content_includes_framework_capabilities_json()
    {
        var repoRoot = FindRepoRoot();
        var docfxPath = Path.Combine(repoRoot, "docs", "docfx.json");
        Assert.True(File.Exists(docfxPath), "Missing docs/docfx.json");

        using var doc = JsonDocument.Parse(File.ReadAllText(docfxPath));
        var contentEntries = doc.RootElement.GetProperty("build").GetProperty("content");

        var includesFrameworkCapabilities = contentEntries
            .EnumerateArray()
            .Where(x => x.TryGetProperty("files", out _))
            .SelectMany(x => x.GetProperty("files").EnumerateArray())
            .Select(x => x.GetString())
            .Any(x => string.Equals(x, "framework-capabilities.json", StringComparison.Ordinal));

        Assert.True(
            includesFrameworkCapabilities,
            "DocFX build content must include framework-capabilities.json so docs links remain valid.");
    }

    [Fact]
    public void Framework_capabilities_entries_declare_compatibility_scope_and_rollback_strategy()
    {
        var repoRoot = FindRepoRoot();
        var capabilitiesPath = Path.Combine(repoRoot, "docs", "framework-capabilities.json");
        Assert.True(File.Exists(capabilitiesPath), "Missing docs/framework-capabilities.json");

        using var doc = JsonDocument.Parse(File.ReadAllText(capabilitiesPath));
        var capabilities = doc.RootElement.GetProperty("capabilities").EnumerateArray().ToList();
        Assert.NotEmpty(capabilities);

        var validPolicies = new HashSet<string>(StringComparer.Ordinal)
        {
            "architecture-approval-required",
            "release-gate-required",
            "compatibility-note-required"
        };

        foreach (var capability in capabilities)
        {
            var capabilityId = capability.GetProperty("id").GetString();

            Assert.True(
                capability.TryGetProperty("breakingChangePolicy", out var policy)
                && policy.ValueKind == JsonValueKind.String
                && validPolicies.Contains(policy.GetString() ?? string.Empty),
                $"Capability '{capabilityId}' must define a governed breakingChangePolicy.");

            Assert.True(
                capability.TryGetProperty("compatibilityScope", out var compatibilityScope)
                && compatibilityScope.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(compatibilityScope.GetString()),
                $"Capability '{capabilityId}' must define non-empty compatibilityScope.");

            Assert.True(
                capability.TryGetProperty("rollbackStrategy", out var rollbackStrategy)
                && rollbackStrategy.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(rollbackStrategy.GetString()),
                $"Capability '{capabilityId}' must define non-empty rollbackStrategy.");
        }
    }

    [Fact]
    public void Roadmap_breaking_change_rule_matches_capability_policy_model()
    {
        var repoRoot = FindRepoRoot();
        var roadmapPath = Path.Combine(repoRoot, "docs", "product-platform-roadmap.md");
        Assert.True(File.Exists(roadmapPath), "Missing docs/product-platform-roadmap.md");

        var content = File.ReadAllText(roadmapPath);
        Assert.Contains("Breaking capability changes must follow each capability's `breakingChangePolicy`.", content, StringComparison.Ordinal);
        Assert.Contains("Architecture approval is mandatory for kernel-level changes", content, StringComparison.Ordinal);
        Assert.Contains("release-gate evidence is required for all breaking capability changes", content, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Fulora.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static Dictionary<string, string> ParseTopLevelTocItems(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        string? pendingName = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var trimmedStart = line.TrimStart();
            var leadingSpaces = line.Length - trimmedStart.Length;

            if (trimmedStart.StartsWith("- name:", StringComparison.Ordinal))
            {
                if (leadingSpaces != 0)
                    continue;

                pendingName = trimmedStart["- name:".Length..].Trim();
                continue;
            }

            if (leadingSpaces == 0)
            {
                pendingName = null;
                continue;
            }

            if (trimmedStart.StartsWith("href:", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(pendingName))
            {
                var href = trimmedStart["href:".Length..].Trim();
                result[pendingName] = href;
                pendingName = null;
            }
        }

        return result;
    }

    private static Dictionary<string, string> ParsePlatformDocumentsTableLinks(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var inSection = false;
        var inTable = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!inSection)
            {
                if (string.Equals(line, "## Platform Documents", StringComparison.Ordinal))
                    inSection = true;

                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
                break;

            if (!inTable)
            {
                if (line.StartsWith("| Platform document |", StringComparison.Ordinal))
                    inTable = true;

                continue;
            }

            if (!line.StartsWith("|", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("|---", StringComparison.Ordinal))
                continue;

            var cells = line.Split('|', StringSplitOptions.None);
            if (cells.Length < 3)
                continue;

            var docCell = cells[1].Trim();
            var labelStart = docCell.IndexOf('[', StringComparison.Ordinal);
            var labelEnd = docCell.IndexOf(']', StringComparison.Ordinal);
            var hrefStart = docCell.IndexOf('(', StringComparison.Ordinal);
            var hrefEnd = docCell.IndexOf(')', StringComparison.Ordinal);

            if (labelStart < 0 || labelEnd <= labelStart || hrefStart < 0 || hrefEnd <= hrefStart)
                continue;

            var label = docCell[(labelStart + 1)..labelEnd];
            var href = docCell[(hrefStart + 1)..hrefEnd];
            result[label] = href;
        }

        return result;
    }
}
