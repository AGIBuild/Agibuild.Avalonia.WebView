using System.Text.Json;
using System.Text.RegularExpressions;
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
            "package-consumption-smoke",
            "shell-attach-detach-soak",
            "shell-multi-window-stress",
            "shell-host-capability-stress",
            "windows-webview2-teardown-stress"
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
        Assert.Contains("--shellPreset app-shell", source, StringComparison.Ordinal);
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
    public void Xunit_v3_package_versions_are_aligned_across_repo_tests_templates_and_samples()
    {
        var repoRoot = FindRepoRoot();

        var projects = new[]
        {
            "tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj",
            "tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj",
            "templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj",
            "samples/avalonia-react/AvaloniReact.Tests/AvaloniReact.Tests.csproj",
        };

        var xunitV3Versions = new Dictionary<string, string>(StringComparer.Ordinal);
        var runnerVersions = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var relative in projects)
        {
            var path = Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"Expected project file does not exist: {path}");
            var xml = File.ReadAllText(path);

            var xunitV3 = ExtractPackageVersion(xml, "xunit.v3");
            Assert.False(string.IsNullOrWhiteSpace(xunitV3), $"Missing PackageReference Include=\"xunit.v3\" in {relative}");
            xunitV3Versions[relative] = xunitV3!;

            var runner = ExtractPackageVersion(xml, "xunit.runner.visualstudio");
            Assert.False(string.IsNullOrWhiteSpace(runner), $"Missing PackageReference Include=\"xunit.runner.visualstudio\" in {relative}");
            runnerVersions[relative] = runner!;
        }

        AssertSingleVersion("xunit.v3", xunitV3Versions);
        AssertSingleVersion("xunit.runner.visualstudio", runnerVersions);
    }

    [Fact]
    public void Hybrid_template_metadata_exposes_shell_preset_choices()
    {
        var repoRoot = FindRepoRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            ".template.config",
            "template.json");
        Assert.True(File.Exists(templatePath), $"Missing template metadata file: {templatePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(templatePath));
        var symbols = doc.RootElement.GetProperty("symbols");
        var shellPreset = symbols.GetProperty("shellPreset");

        Assert.Equal("choice", shellPreset.GetProperty("datatype").GetString());
        Assert.Equal("app-shell", shellPreset.GetProperty("defaultValue").GetString());

        var choices = shellPreset.GetProperty("choices")
            .EnumerateArray()
            .Select(c => c.GetProperty("choice").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("baseline", choices);
        Assert.Contains("app-shell", choices);
    }

    [Fact]
    public void Hybrid_template_source_contains_shell_preset_wiring_markers()
    {
        var repoRoot = FindRepoRoot();
        var desktopMainWindowPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "MainWindow.axaml.cs");
        var appShellPresetPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "MainWindow.AppShellPreset.cs");
        var desktopProjectPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "HybridApp.Desktop.csproj");
        var desktopProgramPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "Program.cs");

        Assert.True(File.Exists(desktopMainWindowPath), $"Missing template source file: {desktopMainWindowPath}");
        Assert.True(File.Exists(appShellPresetPath), $"Missing app-shell preset source file: {appShellPresetPath}");
        Assert.True(File.Exists(desktopProjectPath), $"Missing desktop template project file: {desktopProjectPath}");
        Assert.True(File.Exists(desktopProgramPath), $"Missing desktop template program file: {desktopProgramPath}");

        var desktopMainWindow = File.ReadAllText(desktopMainWindowPath);
        var appShellPreset = File.ReadAllText(appShellPresetPath);
        var desktopProject = File.ReadAllText(desktopProjectPath);
        var desktopProgram = File.ReadAllText(desktopProgramPath);

        Assert.Contains("InitializeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("DisposeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("partial void InitializeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("partial void DisposeShellPreset();", desktopMainWindow, StringComparison.Ordinal);

        Assert.Contains("WebView.NewWindowRequested +=", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("WebView.PermissionRequested +=", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("WebView.DownloadRequested +=", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("Agibuild.Avalonia.WebView", desktopProject, StringComparison.Ordinal);
        Assert.DoesNotContain(".WithInterFont()", desktopProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_production_matrix_declares_platform_coverage_and_executable_evidence()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");
        var lanesPath = Path.Combine(repoRoot, "tests", "automation-lanes.json");

        Assert.True(File.Exists(matrixPath), $"Missing shell production matrix: {matrixPath}");
        Assert.True(File.Exists(lanesPath), $"Missing automation lanes manifest: {lanesPath}");

        using var matrixDoc = JsonDocument.Parse(File.ReadAllText(matrixPath));
        using var lanesDoc = JsonDocument.Parse(File.ReadAllText(lanesPath));

        var requiredPlatforms = new[] { "windows", "macos", "linux" };
        var laneNames = lanesDoc.RootElement.GetProperty("lanes")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var platforms = matrixDoc.RootElement.GetProperty("platforms")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var platform in requiredPlatforms)
        {
            Assert.Contains(platform, platforms);
        }

        var capabilities = matrixDoc.RootElement.GetProperty("capabilities").EnumerateArray().ToList();
        Assert.NotEmpty(capabilities);

        foreach (var capability in capabilities)
        {
            var capabilityId = capability.GetProperty("id").GetString();
            Assert.False(string.IsNullOrWhiteSpace(capabilityId));

            var supportLevel = capability.GetProperty("supportLevel").GetString();
            Assert.False(string.IsNullOrWhiteSpace(supportLevel));

            var coverage = capability.GetProperty("coverage");
            foreach (var platform in requiredPlatforms)
            {
                Assert.True(
                    coverage.TryGetProperty(platform, out var coverageItems),
                    $"Missing platform coverage '{platform}' in capability '{capabilityId}'.");
                Assert.NotEmpty(coverageItems.EnumerateArray());
            }

            var evidenceItems = capability.GetProperty("evidence").EnumerateArray().ToList();
            Assert.NotEmpty(evidenceItems);

            foreach (var evidence in evidenceItems)
            {
                var lane = evidence.GetProperty("lane").GetString();
                var file = evidence.GetProperty("file").GetString();
                var testMethod = evidence.GetProperty("testMethod").GetString();

                Assert.False(string.IsNullOrWhiteSpace(lane));
                Assert.False(string.IsNullOrWhiteSpace(file));
                Assert.False(string.IsNullOrWhiteSpace(testMethod));
                Assert.Contains(lane!, laneNames);

                var sourcePath = Path.Combine(repoRoot, file!.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(sourcePath), $"Matrix evidence source file does not exist: {sourcePath}");

                var source = File.ReadAllText(sourcePath);
                Assert.Contains(testMethod!, source, StringComparison.Ordinal);
            }
        }
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

    private static string? ExtractPackageVersion(string csprojXml, string packageId)
    {
        // Supports:
        //   <PackageReference Include="xunit.v3" Version="3.2.2" />
        //   <PackageReference Include="xunit.runner.visualstudio"><Version>3.1.5</Version></PackageReference>

        var attrPattern = new Regex(
            $@"<PackageReference\s+[^>]*Include=""{Regex.Escape(packageId)}""[^>]*\s+Version=""(?<v>[^""]+)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var attrMatch = attrPattern.Match(csprojXml);
        if (attrMatch.Success)
            return attrMatch.Groups["v"].Value.Trim();

        var elementPattern = new Regex(
            $@"<PackageReference\s+[^>]*Include=""{Regex.Escape(packageId)}""[^>]*>[\s\S]*?<Version>(?<v>[^<]+)</Version>[\s\S]*?</PackageReference>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var elementMatch = elementPattern.Match(csprojXml);
        if (elementMatch.Success)
            return elementMatch.Groups["v"].Value.Trim();

        return null;
    }

    private static void AssertSingleVersion(string packageId, IReadOnlyDictionary<string, string> versionsByProject)
    {
        var distinct = versionsByProject
            .Select(kvp => kvp.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinct.Count == 1)
            return;

        var details = string.Join(
            Environment.NewLine,
            versionsByProject.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        Assert.Fail($"Package version drift detected for '{packageId}'.\n{details}");
    }
}
