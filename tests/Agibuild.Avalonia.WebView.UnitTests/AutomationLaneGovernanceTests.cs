using System.Text.Json;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class AutomationLaneGovernanceTests
{
    [Fact]
    public void Automation_lane_manifest_declares_required_lanes_and_existing_projects()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "automation-lanes.json");
        Assert.True(File.Exists(manifestPath), $"Missing automation lane manifest: {manifestPath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var lanes = doc.RootElement.GetProperty("lanes").EnumerateArray().ToList();
        Assert.NotEmpty(lanes);

        var laneNames = lanes
            .Select(x => x.GetProperty("name").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ContractAutomation", laneNames);
        Assert.Contains("RuntimeAutomation", laneNames);
        Assert.Contains("RuntimeAutomation.PackageSmoke", laneNames);

        foreach (var lane in lanes)
        {
            var project = lane.GetProperty("project").GetString();
            Assert.False(string.IsNullOrWhiteSpace(project));

            var projectPath = Path.Combine(repoRoot, project!.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(projectPath), $"Lane project does not exist: {projectPath}");
        }
    }

    [Fact]
    public void Runtime_critical_path_manifest_maps_to_existing_tests_or_targets()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");
        Assert.True(File.Exists(manifestPath), $"Missing critical-path manifest: {manifestPath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var scenarios = doc.RootElement.GetProperty("scenarios").EnumerateArray().ToList();
        Assert.NotEmpty(scenarios);

        var requiredScenarioIds = new[]
        {
            "off-thread-handle-marshaling",
            "off-thread-navigation-marshaling",
            "lifecycle-contextmenu-reattach-wiring",
            "instance-options-isolation",
            "package-consumption-smoke"
        };

        var scenarioIds = scenarios
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requiredId in requiredScenarioIds)
        {
            Assert.Contains(requiredId, scenarioIds);
        }

        foreach (var scenario in scenarios)
        {
            var file = scenario.GetProperty("file").GetString();
            var testMethod = scenario.GetProperty("testMethod").GetString();
            Assert.False(string.IsNullOrWhiteSpace(file));
            Assert.False(string.IsNullOrWhiteSpace(testMethod));

            var sourcePath = Path.Combine(repoRoot, file!.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(sourcePath), $"Scenario source file does not exist: {sourcePath}");

            var source = File.ReadAllText(sourcePath);
            Assert.Contains(testMethod!, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Build_pipeline_exposes_lane_targets_and_machine_readable_reports()
    {
        var repoRoot = FindRepoRoot();
        var buildPath = Path.Combine(repoRoot, "build", "Build.cs");
        var source = File.ReadAllText(buildPath);

        Assert.Contains("Target ContractAutomation", source, StringComparison.Ordinal);
        Assert.Contains("Target RuntimeAutomation", source, StringComparison.Ordinal);
        Assert.Contains("Target AutomationLaneReport", source, StringComparison.Ordinal);
        Assert.Contains("automation-lane-report.json", source, StringComparison.Ordinal);
        Assert.Contains("nuget-smoke-retry-telemetry.json", source, StringComparison.Ordinal);
        Assert.Contains("RunNugetSmokeWithRetry", source, StringComparison.Ordinal);
        Assert.Contains("ClassifyNugetSmokeFailure", source, StringComparison.Ordinal);
        Assert.Contains("ResolveNugetPackagesRoot", source, StringComparison.Ordinal);
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
