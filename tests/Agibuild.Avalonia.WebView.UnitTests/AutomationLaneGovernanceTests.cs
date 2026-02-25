using System.Text.Json;
using System.Text.RegularExpressions;
using Agibuild.Avalonia.WebView.Testing;
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
            "shell-product-experience-closure",
            "windows-webview2-teardown-stress",
            "shell-devtools-policy-isolation",
            "shell-devtools-lifecycle-cycles",
            "shell-shortcut-routing",
            "shell-system-integration-roundtrip",
            "shell-system-integration-v2-tray-payload",
            "shell-system-integration-v2-timestamp-normalization",
            "shell-system-integration-diagnostic-export"
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
    public void System_integration_ct_matrix_contains_required_rows_and_machine_checkable_evidence()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-system-integration-ct-matrix.json");
        Assert.True(File.Exists(matrixPath), $"Missing system integration CT matrix: {matrixPath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(matrixPath));
        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().ToList();
        Assert.NotEmpty(rows);

        var rowIds = rows
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("tray-event-inbound", rowIds);
        Assert.Contains("menu-pruning", rowIds);
        Assert.Contains("system-action-whitelist", rowIds);
        Assert.Contains("tray-payload-v2-schema", rowIds);

        foreach (var row in rows)
        {
            var coverage = row.GetProperty("coverage").EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.NotEmpty(coverage);

            var evidenceItems = row.GetProperty("evidence").EnumerateArray().ToList();
            Assert.NotEmpty(evidenceItems);
            foreach (var evidence in evidenceItems)
            {
                var file = evidence.GetProperty("file").GetString();
                var testMethod = evidence.GetProperty("testMethod").GetString();
                Assert.False(string.IsNullOrWhiteSpace(file));
                Assert.False(string.IsNullOrWhiteSpace(testMethod));

                var sourcePath = Path.Combine(repoRoot, file!.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(sourcePath), $"CT matrix evidence file does not exist: {sourcePath}");
                var source = File.ReadAllText(sourcePath);
                Assert.Contains(testMethod!, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Build_pipeline_exposes_lane_targets_and_machine_readable_reports()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);
        var mainSource = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.cs"));

        Assert.Contains("partial class BuildTask", mainSource, StringComparison.Ordinal);
        Assert.Contains("Execute<BuildTask>(x => x.Build)", mainSource, StringComparison.Ordinal);

        Assert.Contains("Target ContractAutomation", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target RuntimeAutomation", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target AutomationLaneReport", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target WarningGovernance", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target WarningGovernanceSyntheticCheck", combinedSource, StringComparison.Ordinal);
        Assert.Contains("automation-lane-report.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("warning-governance-report.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("warning-governance.baseline.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("nuget-smoke-retry-telemetry.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("phase5-closeout-snapshot.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target PhaseCloseoutSnapshot", combinedSource, StringComparison.Ordinal);
        Assert.Contains("--shellPreset app-shell", combinedSource, StringComparison.Ordinal);
        Assert.Contains("RunNugetSmokeWithRetry", combinedSource, StringComparison.Ordinal);
        Assert.Contains("ClassifyNugetSmokeFailure", combinedSource, StringComparison.Ordinal);
        Assert.Contains("ResolveNugetPackagesRoot", combinedSource, StringComparison.Ordinal);
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
        var desktopIndexPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "wwwroot",
            "index.html");

        Assert.True(File.Exists(desktopMainWindowPath), $"Missing template source file: {desktopMainWindowPath}");
        Assert.True(File.Exists(appShellPresetPath), $"Missing app-shell preset source file: {appShellPresetPath}");
        Assert.True(File.Exists(desktopProjectPath), $"Missing desktop template project file: {desktopProjectPath}");
        Assert.True(File.Exists(desktopProgramPath), $"Missing desktop template program file: {desktopProgramPath}");
        Assert.True(File.Exists(desktopIndexPath), $"Missing desktop template index file: {desktopIndexPath}");

        var desktopMainWindow = File.ReadAllText(desktopMainWindowPath);
        var appShellPreset = File.ReadAllText(appShellPresetPath);
        var desktopProject = File.ReadAllText(desktopProjectPath);
        var desktopProgram = File.ReadAllText(desktopProgramPath);
        var desktopIndex = File.ReadAllText(desktopIndexPath);

        Assert.Contains("InitializeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("DisposeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("RegisterShellPresetBridgeServices();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("partial void InitializeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("partial void DisposeShellPreset();", desktopMainWindow, StringComparison.Ordinal);
        Assert.Contains("partial void RegisterShellPresetBridgeServices();", desktopMainWindow, StringComparison.Ordinal);

        Assert.Contains("new WebViewShellExperience(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("new WebViewHostCapabilityBridge(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("WebView.Bridge.Expose<IDesktopHostService>", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("TryHandleShellShortcutAsync", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ApplyMenuModel(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("UpdateTrayState(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ExecuteSystemAction(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("_systemActionWhitelist = new HashSet<WebViewSystemAction>", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("SystemActionWhitelist = _systemActionWhitelist", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ShowAbout remains disabled unless explicitly added", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ShowAbout opt-in snippet marker", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("enableShowAboutAction", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("IsShowAboutActionEnabledFromEnvironment", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("AGIBUILD_TEMPLATE_ENABLE_SHOWABOUT", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("SetShowAboutScenario", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("GetSystemIntegrationStrategy", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("template-showabout-policy-deny", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ShowAboutScenarioState", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("canonical profile hash format", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("WebViewPermissionKind.Other", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("ResolveMenuPruningStage", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("DrainSystemIntegrationEvents(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("PublishSystemIntegrationEvent(", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("platform.source", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("platform.pruningStage", appShellPreset, StringComparison.Ordinal);
        Assert.DoesNotContain("ExternalOpenHandler", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("KeyDown +=", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("KeyDown -=", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("WebViewHostCapabilityCallOutcome", appShellPreset, StringComparison.Ordinal);
        Assert.Contains("Agibuild.Avalonia.WebView", desktopProject, StringComparison.Ordinal);
        Assert.DoesNotContain(".WithInterFont()", desktopProgram, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.ReadClipboardText", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.WriteClipboardText", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.ApplyMenuModel", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.UpdateTrayState", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.ExecuteSystemAction", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("DesktopHostService.DrainSystemIntegrationEvents", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("result.appliedTopLevelItems", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("result.pruningStage", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("readBoundedMetadata(", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("platform.source", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("platform.pruningStage", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("source=", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("profileVersion=", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("platform.profileHash", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("result.isVisible", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("Host events", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("System action denied", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("window.runTemplateRegressionChecks", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("setShowAboutScenario", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("readSystemIntegrationStrategy", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("mode=", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("action=", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("outcome=", desktopIndex, StringComparison.Ordinal);
        Assert.Contains("reason=", desktopIndex, StringComparison.Ordinal);

        // Baseline preset must remain free from app-shell bidirectional wiring.
        var templateJsonPath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", ".template.config", "template.json");
        var templateJson = File.ReadAllText(templateJsonPath);
        Assert.Contains("\"condition\": \"(shellPreset == 'baseline')\"", templateJson, StringComparison.Ordinal);
        Assert.Contains("\"exclude\": [\"HybridApp.Desktop/MainWindow.AppShellPreset.cs\"]", templateJson, StringComparison.Ordinal);
        Assert.DoesNotContain("DesktopHostService.DrainSystemIntegrationEvents", desktopMainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishSystemIntegrationEvent", desktopMainWindow, StringComparison.Ordinal);
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

        var requiredPlatforms = new[] { "windows", "macos", "linux", "ios", "android" };
        var allowedCoverageTokens = new HashSet<string>(StringComparer.Ordinal)
        {
            "ct",
            "it-smoke",
            "it-soak",
            "n/a"
        };
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
        var capabilityIds = capabilities
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var requiredCapabilityIds = new[]
        {
            "shell-attach-detach-soak",
            "shell-multi-window-stress",
            "shell-host-capability-stress",
            "shell-product-experience-closure",
            "windows-webview2-teardown-stress",
            "shell-devtools-policy-isolation",
            "shell-devtools-lifecycle-cycles",
            "shell-shortcut-routing",
            "shell-system-integration-roundtrip",
            "shell-system-integration-v2-tray-payload",
            "shell-system-integration-v2-timestamp-normalization",
            "shell-system-integration-diagnostic-export"
        };

        foreach (var capabilityId in requiredCapabilityIds)
        {
            Assert.Contains(capabilityId, capabilityIds);
        }

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
                var coverageTokens = coverageItems.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray();
                Assert.NotEmpty(coverageTokens);
                Assert.All(coverageTokens, token => Assert.Contains(token, allowedCoverageTokens));

                if (platform is "ios" or "android")
                    Assert.All(coverageTokens, token => Assert.Equal("n/a", token));
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
    public void Host_capability_diagnostic_contract_and_external_open_path_remain_schema_stable()
    {
        var repoRoot = FindRepoRoot();
        var bridgePath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView.Runtime",
            "Shell",
            "WebViewHostCapabilityBridge.cs");
        var shellPath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView.Runtime",
            "Shell",
            "WebViewShellExperience.cs");
        var profilePath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView.Runtime",
            "Shell",
            "WebViewSessionPermissionProfiles.cs");
        var helperPath = Path.Combine(
            repoRoot,
            "tests",
            "Agibuild.Avalonia.WebView.Testing",
            "DiagnosticSchemaAssertionHelper.cs");
        var hostCapabilityUnitTestPath = Path.Combine(
            repoRoot,
            "tests",
            "Agibuild.Avalonia.WebView.UnitTests",
            "HostCapabilityBridgeTests.cs");
        var hostCapabilityIntegrationTestPath = Path.Combine(
            repoRoot,
            "tests",
            "Agibuild.Avalonia.WebView.Integration.Tests.Automation",
            "HostCapabilityBridgeIntegrationTests.cs");
        var profileIntegrationTestPath = Path.Combine(
            repoRoot,
            "tests",
            "Agibuild.Avalonia.WebView.Integration.Tests.Automation",
            "MultiWindowLifecycleIntegrationTests.cs");

        Assert.True(File.Exists(bridgePath), $"Missing host capability bridge source: {bridgePath}");
        Assert.True(File.Exists(shellPath), $"Missing shell experience source: {shellPath}");
        Assert.True(File.Exists(profilePath), $"Missing session permission profile source: {profilePath}");
        Assert.True(File.Exists(helperPath), $"Missing diagnostic schema helper source: {helperPath}");
        Assert.True(File.Exists(hostCapabilityUnitTestPath), $"Missing host capability unit test source: {hostCapabilityUnitTestPath}");
        Assert.True(File.Exists(hostCapabilityIntegrationTestPath), $"Missing host capability integration test source: {hostCapabilityIntegrationTestPath}");
        Assert.True(File.Exists(profileIntegrationTestPath), $"Missing profile integration test source: {profileIntegrationTestPath}");

        var bridgeSource = File.ReadAllText(bridgePath);
        var shellSource = File.ReadAllText(shellPath);
        var profileSource = File.ReadAllText(profilePath);
        var helperSource = File.ReadAllText(helperPath);
        var hostCapabilityUnitTestSource = File.ReadAllText(hostCapabilityUnitTestPath);
        var hostCapabilityIntegrationTestSource = File.ReadAllText(hostCapabilityIntegrationTestPath);
        var profileIntegrationTestSource = File.ReadAllText(profileIntegrationTestPath);

        // Outcome schema must keep deterministic allow/deny/failure model.
        Assert.Contains("public enum WebViewHostCapabilityCallOutcome", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("Allow = 0", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("Deny = 1", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("Failure = 2", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("MenuApplyModel = 6", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("TrayUpdateState = 7", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("SystemActionExecute = 8", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("TrayInteractionEventDispatch = 9", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("MenuInteractionEventDispatch = 10", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("ShowAbout = 3", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("public sealed class WebViewHostCapabilityBridgeOptions", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("MinSystemIntegrationMetadataTotalLength = 256", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("MaxSystemIntegrationMetadataTotalLength = 4096", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("DefaultSystemIntegrationMetadataTotalLength = 1024", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("SystemIntegrationMetadataAllowedPrefix = \"platform.\"", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("SystemIntegrationMetadataExtensionPrefix = \"platform.extension.\"", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("ReservedSystemIntegrationMetadataKeys", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("system-integration-event-core-field-missing", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("system-integration-event-metadata-namespace-invalid", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("system-integration-event-metadata-key-unregistered", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("system-integration-event-metadata-budget-exceeded", bridgeSource, StringComparison.Ordinal);

        // Diagnostic payload must remain machine-checkable.
        Assert.Contains("public sealed class WebViewHostCapabilityDiagnosticEventArgs", bridgeSource, StringComparison.Ordinal);
        Assert.Contains(
            $"CurrentDiagnosticSchemaVersion = {DiagnosticSchemaAssertionHelper.HostCapabilitySchemaVersion}",
            bridgeSource,
            StringComparison.Ordinal);
        Assert.Contains("public int DiagnosticSchemaVersion { get; }", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("public Guid CorrelationId { get; }", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("public WebViewHostCapabilityCallOutcome Outcome { get; }", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("public WebViewOperationFailureCategory? FailureCategory { get; }", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("CapabilityCallCompleted", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("public static class DiagnosticSchemaAssertionHelper", helperSource, StringComparison.Ordinal);
        Assert.Contains("AssertHostCapabilityDiagnostic", helperSource, StringComparison.Ordinal);
        Assert.Contains("AssertSessionProfileDiagnostic", helperSource, StringComparison.Ordinal);
        Assert.Contains("DiagnosticSchemaAssertionHelper.AssertHostCapabilityDiagnostic", hostCapabilityUnitTestSource, StringComparison.Ordinal);
        Assert.Contains("DiagnosticSchemaAssertionHelper.AssertHostCapabilityDiagnostic", hostCapabilityIntegrationTestSource, StringComparison.Ordinal);
        Assert.Contains("DiagnosticSchemaAssertionHelper.AssertSessionProfileDiagnostic", profileIntegrationTestSource, StringComparison.Ordinal);

        // External open must route through typed capability bridge without legacy fallback handler path.
        Assert.Contains("Host capability bridge is required for ExternalBrowser strategy.", shellSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExternalOpenHandler", shellSource, StringComparison.Ordinal);
        Assert.Contains("SystemIntegration = 8", shellSource, StringComparison.Ordinal);
        Assert.Contains("_options.HostCapabilityBridge.ApplyMenuModel(", shellSource, StringComparison.Ordinal);
        Assert.Contains("_options.HostCapabilityBridge.UpdateTrayState(", shellSource, StringComparison.Ordinal);
        Assert.Contains("_options.HostCapabilityBridge.ExecuteSystemAction(", shellSource, StringComparison.Ordinal);
        Assert.Contains("_options.HostCapabilityBridge.DispatchSystemIntegrationEvent(", shellSource, StringComparison.Ordinal);
        Assert.Contains("SystemIntegrationEventReceived", shellSource, StringComparison.Ordinal);
        Assert.Contains("profile.ProfileVersion", shellSource, StringComparison.Ordinal);
        Assert.Contains("profile.ProfileHash", shellSource, StringComparison.Ordinal);
        Assert.Contains("public string? ProfileVersion { get; init; }", profileSource, StringComparison.Ordinal);
        Assert.Contains("public string? ProfileHash { get; init; }", profileSource, StringComparison.Ordinal);
        Assert.Contains("public string? ProfileVersion { get; }", profileSource, StringComparison.Ordinal);
        Assert.Contains("public string? ProfileHash { get; }", profileSource, StringComparison.Ordinal);
        Assert.Contains(
            $"CurrentDiagnosticSchemaVersion = {DiagnosticSchemaAssertionHelper.SessionProfileSchemaVersion}",
            profileSource,
            StringComparison.Ordinal);
        Assert.Contains("public int DiagnosticSchemaVersion { get; }", profileSource, StringComparison.Ordinal);
        Assert.Contains("NormalizeProfileVersion", profileSource, StringComparison.Ordinal);
        Assert.Contains("NormalizeProfileHash", profileSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Ci_targets_enforce_openspec_strict_governance_gate()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);
        var mainSource = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.cs"));

        Assert.Contains("Target OpenSpecStrictGovernance", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target DependencyVulnerabilityGovernance", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target TypeScriptDeclarationGovernance", combinedSource, StringComparison.Ordinal);
        Assert.Contains("validate --all --strict", combinedSource, StringComparison.Ordinal);
        Assert.Contains("RunProcessCaptureAllChecked(", combinedSource, StringComparison.Ordinal);
        Assert.Contains("OpenSpecStrictGovernanceReportFile", combinedSource, StringComparison.Ordinal);
        Assert.Contains("dependency-governance-report.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("typescript-governance-report.json", combinedSource, StringComparison.Ordinal);
        Assert.Contains("Target PhaseCloseoutSnapshot", combinedSource, StringComparison.Ordinal);
        Assert.Contains("phase5-closeout-snapshot.json", combinedSource, StringComparison.Ordinal);

        Assert.Matches(
            new Regex(@"Target\s+Ci\s*=>[\s\S]*?\.DependsOn\([\s\S]*OpenSpecStrictGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+Ci\s*=>[\s\S]*?\.DependsOn\([\s\S]*DependencyVulnerabilityGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+Ci\s*=>[\s\S]*?\.DependsOn\([\s\S]*TypeScriptDeclarationGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+Ci\s*=>[\s\S]*?\.DependsOn\([\s\S]*PhaseCloseoutSnapshot[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*OpenSpecStrictGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*DependencyVulnerabilityGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*TypeScriptDeclarationGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
        Assert.Matches(
            new Regex(@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*PhaseCloseoutSnapshot[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
    }

    [Fact]
    public void Phase5_closeout_roadmap_and_shell_governance_artifacts_remain_consistent()
    {
        var repoRoot = FindRepoRoot();
        var roadmapPath = Path.Combine(repoRoot, "openspec", "ROADMAP.md");
        var runtimeManifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");
        var productionMatrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");
        var templateIndexPath = Path.Combine(
            repoRoot,
            "templates",
            "agibuild-hybrid",
            "HybridApp.Desktop",
            "wwwroot",
            "index.html");
        var hostCapabilityBridgePath = Path.Combine(
            repoRoot,
            "src",
            "Agibuild.Avalonia.WebView.Runtime",
            "Shell",
            "WebViewHostCapabilityBridge.cs");

        Assert.True(File.Exists(roadmapPath), $"Missing roadmap file: {roadmapPath}");
        Assert.True(File.Exists(runtimeManifestPath), $"Missing runtime critical-path manifest: {runtimeManifestPath}");
        Assert.True(File.Exists(productionMatrixPath), $"Missing shell production matrix: {productionMatrixPath}");
        Assert.True(File.Exists(templateIndexPath), $"Missing template index file: {templateIndexPath}");
        Assert.True(File.Exists(hostCapabilityBridgePath), $"Missing host capability bridge source: {hostCapabilityBridgePath}");

        var roadmap = File.ReadAllText(roadmapPath);
        Assert.Contains("## Phase 5: Electron Replacement Foundation (✅ Completed)", roadmap, StringComparison.Ordinal);
        Assert.Contains("### Evidence Source Mapping", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-02-24-system-integration-contract-v2-freeze", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-02-24-template-webfirst-dx-panel", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-02-24-system-integration-diagnostic-export", roadmap, StringComparison.Ordinal);
        Assert.Matches(
            new Regex(@"`nuke Test`: Unit `\d+`, Integration `\d+`, Total `\d+` \(pass\)", RegexOptions.Multiline),
            roadmap);
        Assert.Matches(
            new Regex(@"`nuke Coverage`: Line `\d+(\.\d+)?%` \(pass, threshold `\d+%`\)", RegexOptions.Multiline),
            roadmap);

        using var runtimeDoc = JsonDocument.Parse(File.ReadAllText(runtimeManifestPath));
        using var matrixDoc = JsonDocument.Parse(File.ReadAllText(productionMatrixPath));
        var runtimeScenarioIds = runtimeDoc.RootElement.GetProperty("scenarios")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        var matrixCapabilityIds = matrixDoc.RootElement.GetProperty("capabilities")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var sharedPhase5Ids = new[]
        {
            "shell-system-integration-roundtrip",
            "shell-system-integration-v2-tray-payload",
            "shell-system-integration-v2-timestamp-normalization",
            "shell-system-integration-diagnostic-export"
        };

        foreach (var id in sharedPhase5Ids)
        {
            Assert.Contains(id, runtimeScenarioIds);
            Assert.Contains(id, matrixCapabilityIds);
        }

        var templateIndex = File.ReadAllText(templateIndexPath);
        var bridgeSource = File.ReadAllText(hostCapabilityBridgePath);
        Assert.Contains("window.runTemplateRegressionChecks", templateIndex, StringComparison.Ordinal);
        Assert.Contains("ToExportRecord", bridgeSource, StringComparison.Ordinal);
        Assert.Contains("WebViewHostCapabilityDiagnosticExportRecord", bridgeSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Warning_governance_treats_windowsbase_conflicts_as_regressions()
    {
        var repoRoot = FindRepoRoot();
        var warningGovernancePath = Path.Combine(repoRoot, "build", "Build.WarningGovernance.cs");
        Assert.True(File.Exists(warningGovernancePath), $"Missing warning governance source: {warningGovernancePath}");
        var source = File.ReadAllText(warningGovernancePath);

        Assert.Contains(
            "WindowsBase conflict warning must be eliminated; baseline acceptance is not allowed.",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WindowsBase conflict is governed by approved baseline metadata.",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Package_metadata_contains_required_properties_for_stable_release()
    {
        var repoRoot = FindRepoRoot();
        var directoryBuildPropsPath = Path.Combine(repoRoot, "Directory.Build.props");
        Assert.True(File.Exists(directoryBuildPropsPath), $"Missing Directory.Build.props at {directoryBuildPropsPath}");
        var props = File.ReadAllText(directoryBuildPropsPath);

        Assert.Contains("<PackageLicenseExpression>", props, StringComparison.Ordinal);
        Assert.Contains("<PackageProjectUrl>", props, StringComparison.Ordinal);

        var mainCsprojPath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView", "Agibuild.Avalonia.WebView.csproj");
        Assert.True(File.Exists(mainCsprojPath), $"Missing main csproj: {mainCsprojPath}");
        var csproj = File.ReadAllText(mainCsprojPath);
        Assert.Contains("<Description>", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("preview", csproj.ToLowerInvariant().Split("<Description>").Last().Split("</Description>").First());
    }

    [Fact]
    public void Readme_quality_signals_match_actual_test_evidence()
    {
        var repoRoot = FindRepoRoot();
        var readmePath = Path.Combine(repoRoot, "README.md");
        Assert.True(File.Exists(readmePath), $"Missing README.md at {readmePath}");
        var readme = File.ReadAllText(readmePath);

        var unitTestFiles = Directory.GetFiles(
            Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.UnitTests"),
            "*Tests.cs", SearchOption.AllDirectories);
        var integrationTestFiles = Directory.GetFiles(
            Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.Integration.Tests.Automation"),
            "*Tests.cs", SearchOption.AllDirectories);

        Assert.True(unitTestFiles.Length > 0, "No unit test files found");
        Assert.True(integrationTestFiles.Length > 0, "No integration test files found");

        Assert.Matches(new Regex(@"\|\s*Unit tests\s*\|\s*\d{3,}\s*\|"), readme);
        Assert.Matches(new Regex(@"\|\s*Integration tests\s*\|\s*\d{2,}\s*\|"), readme);
        Assert.Matches(new Regex(@"\|\s*Line coverage\s*\|\s*\*\*\d+\.\d+%\*\*\s*\|"), readme);

        Assert.Contains("Phase 5 | Electron Replacement Foundation | ✅ Completed", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void Coverage_target_enforces_branch_coverage_threshold()
    {
        var repoRoot = FindRepoRoot();
        var buildSource = ReadCombinedBuildSource(repoRoot);

        Assert.Contains("BranchCoverageThreshold", buildSource, StringComparison.Ordinal);
        Assert.Contains("branch-rate", buildSource, StringComparison.Ordinal);
        Assert.Contains("Branch coverage", buildSource, StringComparison.Ordinal);
        Assert.Contains("branchThreshold", buildSource, StringComparison.Ordinal);
        Assert.Contains("dependencyGovernanceReportExists", buildSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_matrix_and_runtime_manifest_are_kept_in_sync_for_shell_capabilities()
    {
        var repoRoot = FindRepoRoot();
        var runtimeManifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");

        Assert.True(File.Exists(runtimeManifestPath), $"Missing runtime manifest: {runtimeManifestPath}");
        Assert.True(File.Exists(matrixPath), $"Missing shell production matrix: {matrixPath}");

        using var runtimeDoc = JsonDocument.Parse(File.ReadAllText(runtimeManifestPath));
        using var matrixDoc = JsonDocument.Parse(File.ReadAllText(matrixPath));

        var runtimeShellIds = runtimeDoc.RootElement.GetProperty("scenarios")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id) && id!.StartsWith("shell-", StringComparison.Ordinal))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var matrixCapabilityIds = matrixDoc.RootElement.GetProperty("capabilities")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var shellId in runtimeShellIds)
        {
            Assert.Contains(shellId, matrixCapabilityIds);
        }
    }

    [Fact]
    public void Benchmark_baseline_artifact_has_required_metrics_and_tolerance()
    {
        var repoRoot = FindRepoRoot();
        var baselinePath = Path.Combine(repoRoot, "tests", "performance-benchmark-baseline.json");
        Assert.True(File.Exists(baselinePath), $"Missing benchmark baseline: {baselinePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(baselinePath));
        Assert.True(doc.RootElement.TryGetProperty("version", out var versionNode));
        Assert.True(versionNode.GetInt32() >= 1);
        Assert.True(doc.RootElement.TryGetProperty("allowedRegressionPercent", out var toleranceNode));
        Assert.True(toleranceNode.GetDouble() > 0);

        var metrics = doc.RootElement.GetProperty("metrics").EnumerateArray().ToList();
        Assert.NotEmpty(metrics);

        foreach (var metric in metrics)
        {
            var id = metric.GetProperty("id").GetString();
            Assert.False(string.IsNullOrWhiteSpace(id));
            var baseline = metric.GetProperty("baselineMs").GetDouble();
            Assert.True(baseline > 0, $"Metric '{id}' baselineMs must be > 0.");
        }
    }

    [Fact]
    public void Dx_assets_for_bridge_package_and_vue_sample_are_present_and_typed()
    {
        var repoRoot = FindRepoRoot();
        var bridgePackagePath = Path.Combine(repoRoot, "packages", "bridge", "package.json");
        var bridgeEntryPath = Path.Combine(repoRoot, "packages", "bridge", "src", "index.ts");
        var vueSampleEntryPath = Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "src", "main.ts");
        var vueSampleBridgePath = Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "src", "bridge", "services.ts");
        var vueSampleTsConfigPath = Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "tsconfig.json");

        Assert.True(File.Exists(bridgePackagePath), $"Missing bridge package json: {bridgePackagePath}");
        Assert.True(File.Exists(bridgeEntryPath), $"Missing bridge package entry: {bridgeEntryPath}");
        Assert.True(File.Exists(vueSampleEntryPath), $"Missing Vue sample entry: {vueSampleEntryPath}");
        Assert.True(File.Exists(vueSampleBridgePath), $"Missing Vue bridge services: {vueSampleBridgePath}");
        Assert.True(File.Exists(vueSampleTsConfigPath), $"Missing Vue sample tsconfig: {vueSampleTsConfigPath}");

        var bridgePackage = File.ReadAllText(bridgePackagePath);
        var bridgeEntry = File.ReadAllText(bridgeEntryPath);
        var vueEntry = File.ReadAllText(vueSampleEntryPath);
        var vueBridge = File.ReadAllText(vueSampleBridgePath);
        var vueTsConfig = File.ReadAllText(vueSampleTsConfigPath);

        Assert.Contains("\"@agibuild/bridge\"", bridgePackage, StringComparison.Ordinal);
        Assert.Contains("createBridgeClient", bridgeEntry, StringComparison.Ordinal);
        Assert.Contains("bridgeClient", bridgeEntry, StringComparison.Ordinal);
        Assert.Contains("getService", bridgeEntry, StringComparison.Ordinal);
        Assert.Contains("getAppInfo", vueEntry, StringComparison.Ordinal);
        Assert.Contains("AppShellService.getAppInfo", vueBridge, StringComparison.Ordinal);
        Assert.Contains("bridge.d.ts", vueTsConfig, StringComparison.Ordinal);
    }

    private static string ReadCombinedBuildSource(string repoRoot)
    {
        var buildDir = Path.Combine(repoRoot, "build");
        var buildFiles = Directory.GetFiles(buildDir, "Build*.cs");
        Assert.True(buildFiles.Length >= 2, $"Expected multiple Build*.cs partial files in {buildDir}, found {buildFiles.Length}.");
        return string.Join("\n", buildFiles.Select(File.ReadAllText));
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
