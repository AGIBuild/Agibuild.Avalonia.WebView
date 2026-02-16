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
        Assert.Contains("Target WarningGovernance", source, StringComparison.Ordinal);
        Assert.Contains("Target WarningGovernanceSyntheticCheck", source, StringComparison.Ordinal);
        Assert.Contains("automation-lane-report.json", source, StringComparison.Ordinal);
        Assert.Contains("warning-governance-report.json", source, StringComparison.Ordinal);
        Assert.Contains("warning-governance.baseline.json", source, StringComparison.Ordinal);
        Assert.Contains("nuget-smoke-retry-telemetry.json", source, StringComparison.Ordinal);
        Assert.Contains("RunNugetSmokeWithRetry", source, StringComparison.Ordinal);
        Assert.Contains("ClassifyNugetSmokeFailure", source, StringComparison.Ordinal);
        Assert.Contains("ResolveNugetPackagesRoot", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Warning_governance_baseline_disallows_windowsbase_entries()
    {
        var repoRoot = FindRepoRoot();
        var baselinePath = Path.Combine(repoRoot, "tests", "warning-governance.baseline.json");
        Assert.True(File.Exists(baselinePath), $"Missing warning governance baseline: {baselinePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var windowsBaseConflicts = doc.RootElement.GetProperty("windowsBaseConflicts").EnumerateArray().ToList();
        Assert.Empty(windowsBaseConflicts);
    }

    [Fact]
    public void Webview2_reference_model_is_host_agnostic()
    {
        var repoRoot = FindRepoRoot();
        var adapterProjectPath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView.Adapters.Windows",
            "Agibuild.Avalonia.WebView.Adapters.Windows.csproj");
        var packProjectPath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView",
            "Agibuild.Avalonia.WebView.csproj");

        var adapterSource = File.ReadAllText(adapterProjectPath);
        var packSource = File.ReadAllText(packProjectPath);

        Assert.Contains("ExcludeAssets=\"compile;build;buildTransitive\"", adapterSource, StringComparison.Ordinal);
        Assert.Contains("<Reference Include=\"Microsoft.Web.WebView2.Core\">", adapterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MSB3277", adapterSource, StringComparison.Ordinal);
        Assert.Contains("ExcludeAssets=\"build;buildTransitive\"", packSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Warning_governance_treats_windowsbase_conflicts_as_regressions()
    {
        var repoRoot = FindRepoRoot();
        var buildPath = Path.Combine(repoRoot, "build", "Build.cs");
        var source = File.ReadAllText(buildPath);

        Assert.Contains(
            "WindowsBase conflict warning must be eliminated; baseline acceptance is not allowed.",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WindowsBase conflict is governed by approved baseline metadata.",
            source,
            StringComparison.Ordinal);
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
