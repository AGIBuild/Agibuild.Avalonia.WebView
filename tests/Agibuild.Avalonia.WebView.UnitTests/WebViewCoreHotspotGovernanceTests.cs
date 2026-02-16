using System.Text.Json;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class WebViewCoreHotspotGovernanceTests
{
    [Fact]
    public void Hotspot_manifest_entries_have_required_metadata()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "webviewcore-hotspots.manifest.json");
        Assert.True(File.Exists(manifestPath), $"Missing hotspot manifest: {manifestPath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = doc.RootElement;
        var version = root.GetProperty("version").GetInt32();
        Assert.True(version >= 1);

        var hotspots = root.GetProperty("hotspots").EnumerateArray().ToList();
        Assert.NotEmpty(hotspots);

        var laneManifestPath = Path.Combine(repoRoot, "tests", "automation-lanes.json");
        using var laneDoc = JsonDocument.Parse(File.ReadAllText(laneManifestPath));
        var knownLanes = laneDoc.RootElement.GetProperty("lanes")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var hotspot in hotspots)
        {
            var id = hotspot.GetProperty("id").GetString();
            var runtimeMethod = hotspot.GetProperty("runtimeMethod").GetString();
            var branchIntent = hotspot.GetProperty("branchIntent").GetString();
            var owner = hotspot.GetProperty("owner").GetString();
            var lane = hotspot.GetProperty("lane").GetString();
            var testFile = hotspot.GetProperty("testFile").GetString();
            var testMethod = hotspot.GetProperty("testMethod").GetString();

            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.False(string.IsNullOrWhiteSpace(runtimeMethod));
            Assert.False(string.IsNullOrWhiteSpace(branchIntent));
            Assert.False(string.IsNullOrWhiteSpace(owner));
            Assert.False(string.IsNullOrWhiteSpace(lane));
            Assert.False(string.IsNullOrWhiteSpace(testFile));
            Assert.False(string.IsNullOrWhiteSpace(testMethod));

            Assert.Contains(lane!, knownLanes);
        }
    }

    [Fact]
    public void Hotspot_manifest_entries_map_to_executable_test_evidence()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "webviewcore-hotspots.manifest.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));

        foreach (var hotspot in doc.RootElement.GetProperty("hotspots").EnumerateArray())
        {
            var testFile = hotspot.GetProperty("testFile").GetString();
            var testMethod = hotspot.GetProperty("testMethod").GetString();

            var sourcePath = Path.Combine(repoRoot, testFile!.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(sourcePath), $"Hotspot test file does not exist: {sourcePath}");

            var source = File.ReadAllText(sourcePath);
            Assert.Contains(testMethod!, source, StringComparison.Ordinal);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Avalonia.WebView.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
