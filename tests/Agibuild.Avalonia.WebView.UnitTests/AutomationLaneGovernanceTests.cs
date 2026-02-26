using System.Text.Json;
using System.Text.RegularExpressions;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;
using static Agibuild.Avalonia.WebView.Testing.GovernanceAssertionHelper;
using static Agibuild.Avalonia.WebView.Testing.GovernanceInvariantIds;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class AutomationLaneGovernanceTests
{
    [Fact]
    public void Automation_lane_manifest_declares_required_lanes_and_existing_projects()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "automation-lanes.json");

        using var doc = LoadJsonArtifact(manifestPath, AutomationLaneManifestSchema);
        var lanes = RequireProperty(doc.RootElement, "lanes", AutomationLaneManifestSchema, manifestPath);

        var laneNames = ExtractStringIds(lanes, "name");
        AssertContainsAll(laneNames, ["ContractAutomation", "RuntimeAutomation", "RuntimeAutomation.PackageSmoke"],
            AutomationLaneManifestSchema, manifestPath);

        foreach (var lane in lanes.EnumerateArray())
        {
            var project = lane.GetProperty("project").GetString()!;
            var projectPath = Path.Combine(repoRoot, project.Replace('/', Path.DirectorySeparatorChar));
            AssertFileExists(projectPath, AutomationLaneManifestSchema);
        }
    }

    [Fact]
    public void Runtime_critical_path_manifest_maps_to_existing_tests_or_targets()
    {
        var repoRoot = FindRepoRoot();
        var manifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");

        using var doc = LoadJsonArtifact(manifestPath, RuntimeCriticalPathScenarioPresence);
        var scenarios = RequireProperty(doc.RootElement, "scenarios", RuntimeCriticalPathScenarioPresence, manifestPath);

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

        var scenarioIds = ExtractStringIds(scenarios, "id");
        AssertContainsAll(scenarioIds, requiredScenarioIds, RuntimeCriticalPathScenarioPresence, manifestPath);

        var validCiContexts = new HashSet<string>(StringComparer.Ordinal) { "Ci", "CiPublish" };

        foreach (var scenario in scenarios.EnumerateArray())
        {
            var id = scenario.GetProperty("id").GetString()!;
            var file = scenario.GetProperty("file").GetString()!;
            var testMethod = scenario.GetProperty("testMethod").GetString()!;
            var ciContext = scenario.TryGetProperty("ciContext", out var ciContextNode) ? ciContextNode.GetString()! : "Ci";

            AssertControlledVocabulary([ciContext], validCiContexts, RuntimeCriticalPathScenarioPresence, $"scenario '{id}' ciContext");

            if (string.Equals(id, "package-consumption-smoke", StringComparison.Ordinal))
                Assert.Equal("CiPublish", ciContext);

            AssertEvidenceLinkage(repoRoot, file, testMethod, RuntimeCriticalPathEvidenceLinkage);
        }
    }

    [Fact]
    public void System_integration_ct_matrix_contains_required_rows_and_machine_checkable_evidence()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-system-integration-ct-matrix.json");

        using var doc = LoadJsonArtifact(matrixPath, SystemIntegrationCtMatrixSchema);
        var rows = RequireProperty(doc.RootElement, "rows", SystemIntegrationCtMatrixSchema, matrixPath);

        var rowIds = ExtractStringIds(rows, "id");
        AssertContainsAll(rowIds,
            ["tray-event-inbound", "menu-pruning", "system-action-whitelist", "tray-payload-v2-schema"],
            SystemIntegrationCtMatrixSchema, matrixPath);

        foreach (var row in rows.EnumerateArray())
        {
            var coverage = row.GetProperty("coverage").EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.NotEmpty(coverage);

            AssertEvidenceItems(row.GetProperty("evidence"), repoRoot, SystemIntegrationCtMatrixSchema);
        }
    }

    [Fact]
    public void Build_pipeline_exposes_lane_targets_and_machine_readable_reports()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);
        var mainSource = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.cs"));

        var requiredTargets = new[]
        {
            "Target ContractAutomation", "Target RuntimeAutomation", "Target AutomationLaneReport",
            "Target WarningGovernance", "Target WarningGovernanceSyntheticCheck", "Target PhaseCloseoutSnapshot"
        };
        foreach (var target in requiredTargets)
            AssertSourceContains(combinedSource, target, BuildPipelineTargetGraph, "build/Build*.cs");

        var requiredArtifacts = new[]
        {
            "automation-lane-report.json", "warning-governance-report.json",
            "warning-governance.baseline.json", "nuget-smoke-retry-telemetry.json",
            "ci-evidence-snapshot.json"
        };
        foreach (var artifact in requiredArtifacts)
            AssertSourceContains(combinedSource, artifact, BuildPipelineTargetGraph, "build/Build*.cs");

        var requiredMethods = new[] { "RunNugetSmokeWithRetry", "ClassifyNugetSmokeFailure", "ResolveNugetPackagesRoot" };
        foreach (var method in requiredMethods)
            AssertSourceContains(combinedSource, method, BuildPipelineTargetGraph, "build/Build*.cs");

        AssertSourceContains(mainSource, "partial class BuildTask", BuildPipelineTargetGraph, "build/Build.cs");
        AssertSourceContains(mainSource, "Execute<BuildTask>(x => x.Build)", BuildPipelineTargetGraph, "build/Build.cs");
        AssertSourceContains(combinedSource, "--shellPreset app-shell", BuildPipelineTargetGraph, "build/Build*.cs");
    }

    [Fact]
    public void Warning_governance_baseline_disallows_windowsbase_entries()
    {
        var repoRoot = FindRepoRoot();
        var baselinePath = Path.Combine(repoRoot, "tests", "warning-governance.baseline.json");

        using var doc = LoadJsonArtifact(baselinePath, WarningGovernanceBaseline);
        var conflicts = RequireProperty(doc.RootElement, "windowsBaseConflicts", WarningGovernanceBaseline, baselinePath);
        Assert.Empty(conflicts.EnumerateArray().ToList());
    }

    [Fact]
    public void Webview2_reference_model_is_host_agnostic()
    {
        var repoRoot = FindRepoRoot();
        var adapterProjectPath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView.Adapters.Windows", "Agibuild.Avalonia.WebView.Adapters.Windows.csproj");
        var packProjectPath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView", "Agibuild.Avalonia.WebView.csproj");

        AssertFileExists(adapterProjectPath, WebView2ReferenceModel);
        AssertFileExists(packProjectPath, WebView2ReferenceModel);

        var adapterSource = File.ReadAllText(adapterProjectPath);
        var packSource = File.ReadAllText(packProjectPath);

        AssertSourceContains(adapterSource, "ExcludeAssets=\"compile;build;buildTransitive\"", WebView2ReferenceModel, adapterProjectPath);
        AssertSourceContains(adapterSource, "<Reference Include=\"Microsoft.Web.WebView2.Core\">", WebView2ReferenceModel, adapterProjectPath);
        Assert.DoesNotContain("MSB3277", adapterSource, StringComparison.Ordinal);
        AssertSourceContains(packSource, "ExcludeAssets=\"build;buildTransitive\"", WebView2ReferenceModel, packProjectPath);
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
            AssertFileExists(path, XunitVersionAlignment);
            var xml = File.ReadAllText(path);

            var xunitV3 = ExtractPackageVersion(xml, "xunit.v3");
            Assert.False(string.IsNullOrWhiteSpace(xunitV3), $"[{XunitVersionAlignment}] Missing xunit.v3 in {relative}");
            xunitV3Versions[relative] = xunitV3!;

            var runner = ExtractPackageVersion(xml, "xunit.runner.visualstudio");
            Assert.False(string.IsNullOrWhiteSpace(runner), $"[{XunitVersionAlignment}] Missing xunit.runner.visualstudio in {relative}");
            runnerVersions[relative] = runner!;
        }

        AssertSingleVersion("xunit.v3", xunitV3Versions);
        AssertSingleVersion("xunit.runner.visualstudio", runnerVersions);
    }

    [Fact]
    public void Hybrid_template_metadata_exposes_shell_preset_choices()
    {
        var repoRoot = FindRepoRoot();
        var templatePath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", ".template.config", "template.json");

        using var doc = LoadJsonArtifact(templatePath, TemplateMetadataSchema);
        var symbols = RequireProperty(doc.RootElement, "symbols", TemplateMetadataSchema, templatePath);
        var shellPreset = RequireProperty(symbols, "shellPreset", TemplateMetadataSchema, templatePath);

        Assert.Equal("choice", shellPreset.GetProperty("datatype").GetString());
        Assert.Equal("app-shell", shellPreset.GetProperty("defaultValue").GetString());

        var choices = ExtractStringIds(shellPreset.GetProperty("choices"), "choice");
        AssertContainsAll(choices, ["baseline", "app-shell"], TemplateMetadataSchema, templatePath);
    }

    [Fact]
    public void Hybrid_template_source_contains_shell_preset_wiring_markers()
    {
        var repoRoot = FindRepoRoot();
        var basePath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", "HybridApp.Desktop");

        var desktopMainWindowPath = Path.Combine(basePath, "MainWindow.axaml.cs");
        var appShellPresetPath = Path.Combine(basePath, "MainWindow.AppShellPreset.cs");
        var desktopProjectPath = Path.Combine(basePath, "HybridApp.Desktop.csproj");
        var desktopProgramPath = Path.Combine(basePath, "Program.cs");
        var desktopIndexPath = Path.Combine(basePath, "wwwroot", "index.html");

        foreach (var p in new[] { desktopMainWindowPath, appShellPresetPath, desktopProjectPath, desktopProgramPath, desktopIndexPath })
            AssertFileExists(p, TemplateMetadataSchema);

        var desktopMainWindow = File.ReadAllText(desktopMainWindowPath);
        var appShellPreset = File.ReadAllText(appShellPresetPath);
        var desktopProject = File.ReadAllText(desktopProjectPath);
        var desktopProgram = File.ReadAllText(desktopProgramPath);
        var desktopIndex = File.ReadAllText(desktopIndexPath);

        var mainWindowMarkers = new[]
        {
            "InitializeShellPreset();", "DisposeShellPreset();", "RegisterShellPresetBridgeServices();",
            "partial void InitializeShellPreset();", "partial void DisposeShellPreset();",
            "partial void RegisterShellPresetBridgeServices();"
        };
        foreach (var marker in mainWindowMarkers)
            AssertSourceContains(desktopMainWindow, marker, TemplateMetadataSchema, desktopMainWindowPath);

        var appShellPresetMarkers = new[]
        {
            "new WebViewShellExperience(", "new WebViewHostCapabilityBridge(",
            "WebView.Bridge.Expose<IDesktopHostService>", "TryHandleShellShortcutAsync",
            "ApplyMenuModel(", "UpdateTrayState(", "ExecuteSystemAction(",
            "_systemActionWhitelist = new HashSet<WebViewSystemAction>",
            "SystemActionWhitelist = _systemActionWhitelist",
            "ShowAbout remains disabled unless explicitly added",
            "ShowAbout opt-in snippet marker", "enableShowAboutAction",
            "IsShowAboutActionEnabledFromEnvironment", "AGIBUILD_TEMPLATE_ENABLE_SHOWABOUT",
            "SetShowAboutScenario", "GetSystemIntegrationStrategy",
            "template-showabout-policy-deny", "ShowAboutScenarioState",
            "canonical profile hash format",
            "SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver",
            "WebViewPermissionKind.Other", "ResolveMenuPruningStage",
            "DrainSystemIntegrationEvents(", "PublishSystemIntegrationEvent(",
            "platform.source", "platform.pruningStage",
            "KeyDown +=", "KeyDown -=", "WebViewHostCapabilityCallOutcome"
        };
        foreach (var marker in appShellPresetMarkers)
            AssertSourceContains(appShellPreset, marker, TemplateMetadataSchema, appShellPresetPath);

        Assert.DoesNotContain("ExternalOpenHandler", appShellPreset, StringComparison.Ordinal);

        AssertSourceContains(desktopProject, "Agibuild.Avalonia.WebView", TemplateMetadataSchema, desktopProjectPath);
        Assert.DoesNotContain(".WithInterFont()", desktopProgram, StringComparison.Ordinal);

        var indexMarkers = new[]
        {
            "DesktopHostService.ReadClipboardText", "DesktopHostService.WriteClipboardText",
            "DesktopHostService.ApplyMenuModel", "DesktopHostService.UpdateTrayState",
            "DesktopHostService.ExecuteSystemAction", "DesktopHostService.DrainSystemIntegrationEvents",
            "result.appliedTopLevelItems", "result.pruningStage", "readBoundedMetadata(",
            "platform.source", "platform.pruningStage", "source=", "profileVersion=",
            "platform.profileHash", "result.isVisible", "Host events", "System action denied",
            "window.runTemplateRegressionChecks", "setShowAboutScenario",
            "readSystemIntegrationStrategy", "mode=", "action=", "outcome=", "reason="
        };
        foreach (var marker in indexMarkers)
            AssertSourceContains(desktopIndex, marker, TemplateMetadataSchema, desktopIndexPath);

        var templateJsonPath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", ".template.config", "template.json");
        var templateJson = File.ReadAllText(templateJsonPath);
        var reactTemplateWebPath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", "HybridApp.Web.Vite.React");
        var vueTemplateWebPath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", "HybridApp.Web.Vite.Vue");

        var templateJsonMarkers = new[]
        {
            "\"condition\": \"(shellPreset == 'baseline')\"",
            "\"exclude\": [\"HybridApp.Desktop/MainWindow.AppShellPreset.cs\"]",
            "\"condition\": \"(framework == 'react')\"",
            "\"condition\": \"(framework == 'vue')\"",
            "HybridApp.Web.Vite.React/**", "HybridApp.Web.Vite.Vue/**"
        };
        foreach (var marker in templateJsonMarkers)
            AssertSourceContains(templateJson, marker, TemplateMetadataSchema, templateJsonPath);

        Assert.True(Directory.Exists(reactTemplateWebPath), $"[{TemplateMetadataSchema}] Missing: {reactTemplateWebPath}");
        Assert.True(Directory.Exists(vueTemplateWebPath), $"[{TemplateMetadataSchema}] Missing: {vueTemplateWebPath}");
        AssertFileExists(Path.Combine(reactTemplateWebPath, "package.json"), TemplateMetadataSchema);
        AssertFileExists(Path.Combine(vueTemplateWebPath, "package.json"), TemplateMetadataSchema);
        Assert.DoesNotContain("DesktopHostService.DrainSystemIntegrationEvents", desktopMainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishSystemIntegrationEvent", desktopMainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_production_matrix_declares_platform_coverage_and_executable_evidence()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");
        var lanesPath = Path.Combine(repoRoot, "tests", "automation-lanes.json");

        using var matrixDoc = LoadJsonArtifact(matrixPath, ShellProductionMatrixSchema);
        using var lanesDoc = LoadJsonArtifact(lanesPath, ShellProductionMatrixSchema);

        var requiredPlatforms = new[] { "windows", "macos", "linux", "ios", "android" };
        var allowedCoverageTokens = new HashSet<string>(StringComparer.Ordinal) { "ct", "it-smoke", "it-soak", "n/a" };

        var laneNames = ExtractStringIds(lanesDoc.RootElement.GetProperty("lanes"), "name");
        var platforms = matrixDoc.RootElement.GetProperty("platforms").EnumerateArray()
            .Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        AssertContainsAll(platforms, requiredPlatforms, ShellProductionMatrixSchema, matrixPath);

        var capabilities = matrixDoc.RootElement.GetProperty("capabilities").EnumerateArray().ToList();
        Assert.NotEmpty(capabilities);

        var capabilityIds = capabilities.Select(x => x.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var requiredCapabilityIds = new[]
        {
            "shell-attach-detach-soak", "shell-multi-window-stress", "shell-host-capability-stress",
            "shell-product-experience-closure", "windows-webview2-teardown-stress",
            "shell-devtools-policy-isolation", "shell-devtools-lifecycle-cycles",
            "shell-shortcut-routing", "shell-system-integration-roundtrip",
            "shell-system-integration-v2-tray-payload", "shell-system-integration-v2-timestamp-normalization",
            "shell-system-integration-diagnostic-export"
        };
        AssertContainsAll(capabilityIds, requiredCapabilityIds, ShellProductionMatrixSchema, matrixPath);

        foreach (var capability in capabilities)
        {
            var capabilityId = capability.GetProperty("id").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(capability.GetProperty("supportLevel").GetString()));

            var coverage = capability.GetProperty("coverage");
            foreach (var platform in requiredPlatforms)
            {
                Assert.True(coverage.TryGetProperty(platform, out var coverageItems),
                    $"[{ShellProductionMatrixSchema}] Missing platform coverage '{platform}' in capability '{capabilityId}'.");

                var tokens = coverageItems.EnumerateArray().Select(x => x.GetString()!)
                    .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                Assert.NotEmpty(tokens);
                AssertControlledVocabulary(tokens, allowedCoverageTokens, ShellProductionMatrixSchema, $"capability '{capabilityId}' platform '{platform}'");

                if (platform is "ios" or "android")
                    Assert.All(tokens, token => Assert.Equal("n/a", token));
            }

            var evidenceItems = capability.GetProperty("evidence").EnumerateArray().ToList();
            Assert.NotEmpty(evidenceItems);

            foreach (var evidence in evidenceItems)
            {
                var lane = evidence.GetProperty("lane").GetString()!;
                Assert.Contains(lane, laneNames);
                AssertEvidenceLinkage(repoRoot, evidence.GetProperty("file").GetString()!,
                    evidence.GetProperty("testMethod").GetString()!, ShellProductionMatrixSchema);
            }
        }
    }

    [Fact]
    public void Host_capability_diagnostic_contract_and_external_open_path_remain_schema_stable()
    {
        var repoRoot = FindRepoRoot();
        var bridgePath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView.Runtime", "Shell", "WebViewHostCapabilityBridge.cs");
        var shellPath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView.Runtime", "Shell", "WebViewShellExperience.cs");
        var profilePath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView.Runtime", "Shell", "WebViewSessionPermissionProfiles.cs");
        var helperPath = Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.Testing", "DiagnosticSchemaAssertionHelper.cs");
        var hostCapabilityUnitTestPath = Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.UnitTests", "HostCapabilityBridgeTests.cs");
        var hostCapabilityIntegrationTestPath = Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.Integration.Tests.Automation", "HostCapabilityBridgeIntegrationTests.cs");
        var profileIntegrationTestPath = Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.Integration.Tests.Automation", "MultiWindowLifecycleIntegrationTests.cs");

        foreach (var p in new[] { bridgePath, shellPath, profilePath, helperPath, hostCapabilityUnitTestPath, hostCapabilityIntegrationTestPath, profileIntegrationTestPath })
            AssertFileExists(p, PhaseCloseoutConsistency);

        var bridgeSource = File.ReadAllText(bridgePath);
        var shellSource = File.ReadAllText(shellPath);
        var profileSource = File.ReadAllText(profilePath);
        var helperSource = File.ReadAllText(helperPath);
        var hostCapabilityUnitTestSource = File.ReadAllText(hostCapabilityUnitTestPath);
        var hostCapabilityIntegrationTestSource = File.ReadAllText(hostCapabilityIntegrationTestPath);
        var profileIntegrationTestSource = File.ReadAllText(profileIntegrationTestPath);

        var bridgeSchemaMarkers = new[]
        {
            "public enum WebViewHostCapabilityCallOutcome",
            "Allow = 0", "Deny = 1", "Failure = 2",
            "MenuApplyModel = 6", "TrayUpdateState = 7", "SystemActionExecute = 8",
            "TrayInteractionEventDispatch = 9", "MenuInteractionEventDispatch = 10", "ShowAbout = 3",
            "public sealed class WebViewHostCapabilityBridgeOptions",
            "MinSystemIntegrationMetadataTotalLength = 256",
            "MaxSystemIntegrationMetadataTotalLength = 4096",
            "DefaultSystemIntegrationMetadataTotalLength = 1024",
            "SystemIntegrationMetadataAllowedPrefix = \"platform.\"",
            "SystemIntegrationMetadataExtensionPrefix = \"platform.extension.\"",
            "ReservedSystemIntegrationMetadataKeys",
            "system-integration-event-core-field-missing",
            "system-integration-event-metadata-namespace-invalid",
            "system-integration-event-metadata-key-unregistered",
            "system-integration-event-metadata-budget-exceeded",
            "public sealed class WebViewHostCapabilityDiagnosticEventArgs",
            $"CurrentDiagnosticSchemaVersion = {DiagnosticSchemaAssertionHelper.HostCapabilitySchemaVersion}",
            "public int DiagnosticSchemaVersion { get; }",
            "public Guid CorrelationId { get; }",
            "public WebViewHostCapabilityCallOutcome Outcome { get; }",
            "public WebViewOperationFailureCategory? FailureCategory { get; }",
            "CapabilityCallCompleted"
        };
        foreach (var marker in bridgeSchemaMarkers)
            AssertSourceContains(bridgeSource, marker, PhaseCloseoutConsistency, bridgePath);

        AssertSourceContains(helperSource, "public static class DiagnosticSchemaAssertionHelper", PhaseCloseoutConsistency, helperPath);
        AssertSourceContains(helperSource, "AssertHostCapabilityDiagnostic", PhaseCloseoutConsistency, helperPath);
        AssertSourceContains(helperSource, "AssertSessionProfileDiagnostic", PhaseCloseoutConsistency, helperPath);
        AssertSourceContains(hostCapabilityUnitTestSource, "DiagnosticSchemaAssertionHelper.AssertHostCapabilityDiagnostic", PhaseCloseoutConsistency, hostCapabilityUnitTestPath);
        AssertSourceContains(hostCapabilityIntegrationTestSource, "DiagnosticSchemaAssertionHelper.AssertHostCapabilityDiagnostic", PhaseCloseoutConsistency, hostCapabilityIntegrationTestPath);
        AssertSourceContains(profileIntegrationTestSource, "DiagnosticSchemaAssertionHelper.AssertSessionProfileDiagnostic", PhaseCloseoutConsistency, profileIntegrationTestPath);

        var shellMarkers = new[]
        {
            "Host capability bridge is required for ExternalBrowser strategy.",
            "SystemIntegration = 8",
            "_options.HostCapabilityBridge.ApplyMenuModel(",
            "_options.HostCapabilityBridge.UpdateTrayState(",
            "_options.HostCapabilityBridge.ExecuteSystemAction(",
            "_options.HostCapabilityBridge.DispatchSystemIntegrationEvent(",
            "SystemIntegrationEventReceived",
            "profile.ProfileVersion", "profile.ProfileHash"
        };
        foreach (var marker in shellMarkers)
            AssertSourceContains(shellSource, marker, PhaseCloseoutConsistency, shellPath);

        Assert.DoesNotContain("ExternalOpenHandler", shellSource, StringComparison.Ordinal);

        var profileMarkers = new[]
        {
            "public string? ProfileVersion { get; init; }", "public string? ProfileHash { get; init; }",
            "public string? ProfileVersion { get; }", "public string? ProfileHash { get; }",
            $"CurrentDiagnosticSchemaVersion = {DiagnosticSchemaAssertionHelper.SessionProfileSchemaVersion}",
            "public int DiagnosticSchemaVersion { get; }",
            "NormalizeProfileVersion", "NormalizeProfileHash"
        };
        foreach (var marker in profileMarkers)
            AssertSourceContains(profileSource, marker, PhaseCloseoutConsistency, profilePath);
    }

    [Fact]
    public void Ci_targets_enforce_openspec_strict_governance_gate()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);
        var mainSource = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.cs"));

        var requiredTargets = new[]
        {
            "Target OpenSpecStrictGovernance", "Target DependencyVulnerabilityGovernance",
            "Target TypeScriptDeclarationGovernance", "Target PhaseCloseoutSnapshot"
        };
        foreach (var target in requiredTargets)
            AssertSourceContains(combinedSource, target, CiTargetOpenSpecGate, "build/Build*.cs");

        AssertSourceContains(combinedSource, "validate --all --strict", CiTargetOpenSpecGate, "build/Build*.cs");
        AssertSourceContains(combinedSource, "RunProcessCaptureAllChecked(", CiTargetOpenSpecGate, "build/Build*.cs");
        AssertSourceContains(combinedSource, "dependency-governance-report.json", CiTargetOpenSpecGate, "build/Build*.cs");
        AssertSourceContains(combinedSource, "typescript-governance-report.json", CiTargetOpenSpecGate, "build/Build*.cs");
        AssertSourceContains(combinedSource, "ci-evidence-snapshot.json", CiTargetOpenSpecGate, "build/Build*.cs");

        var ciDependencies = new[]
        {
            "OpenSpecStrictGovernance", "DependencyVulnerabilityGovernance",
            "TypeScriptDeclarationGovernance", "PhaseCloseoutSnapshot",
            "RuntimeCriticalPathExecutionGovernanceCi"
        };
        foreach (var dep in ciDependencies)
        {
            Assert.Matches(
                new Regex($@"Target\s+Ci\s*=>[\s\S]*?\.DependsOn\([\s\S]*{Regex.Escape(dep)}[\s\S]*\);", RegexOptions.Multiline),
                mainSource);
        }

        var ciPublishDependencies = new[]
        {
            "OpenSpecStrictGovernance", "DependencyVulnerabilityGovernance",
            "TypeScriptDeclarationGovernance", "PhaseCloseoutSnapshot",
            "RuntimeCriticalPathExecutionGovernanceCiPublish"
        };
        foreach (var dep in ciPublishDependencies)
        {
            Assert.Matches(
                new Regex($@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*{Regex.Escape(dep)}[\s\S]*\);", RegexOptions.Multiline),
                mainSource);
        }
    }

    [Fact]
    public void Phase5_closeout_roadmap_and_shell_governance_artifacts_remain_consistent()
    {
        var repoRoot = FindRepoRoot();
        var roadmapPath = Path.Combine(repoRoot, "openspec", "ROADMAP.md");
        var runtimeManifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");
        var productionMatrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");
        var templateIndexPath = Path.Combine(repoRoot, "templates", "agibuild-hybrid", "HybridApp.Desktop", "wwwroot", "index.html");
        var hostCapabilityBridgePath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView.Runtime", "Shell", "WebViewHostCapabilityBridge.cs");

        foreach (var p in new[] { roadmapPath, runtimeManifestPath, productionMatrixPath, templateIndexPath, hostCapabilityBridgePath })
            AssertFileExists(p, PhaseCloseoutConsistency);

        var roadmap = File.ReadAllText(roadmapPath);
        AssertSourceContains(roadmap, "## Phase 5: Framework Positioning Foundation (✅ Completed)", PhaseCloseoutConsistency, roadmapPath);
        AssertSourceContains(roadmap, "### Evidence Source Mapping", PhaseCloseoutConsistency, roadmapPath);

        var requiredChangeIds = new[]
        {
            "2026-02-24-system-integration-contract-v2-freeze",
            "2026-02-24-template-webfirst-dx-panel",
            "2026-02-24-system-integration-diagnostic-export"
        };
        foreach (var changeId in requiredChangeIds)
            AssertSourceContains(roadmap, changeId, PhaseCloseoutConsistency, roadmapPath);

        Assert.Matches(new Regex(@"`nuke Test`: Unit `\d+`, Integration `\d+`, Total `\d+` \(pass\)", RegexOptions.Multiline), roadmap);
        Assert.Matches(new Regex(@"`nuke Coverage`: Line `\d+(\.\d+)?%` \(pass, threshold `\d+%`\)", RegexOptions.Multiline), roadmap);

        using var runtimeDoc = LoadJsonArtifact(runtimeManifestPath, PhaseCloseoutConsistency);
        using var matrixDoc = LoadJsonArtifact(productionMatrixPath, PhaseCloseoutConsistency);

        var runtimeScenarioIds = ExtractStringIds(runtimeDoc.RootElement.GetProperty("scenarios"), "id");
        var matrixCapabilityIds = ExtractStringIds(matrixDoc.RootElement.GetProperty("capabilities"), "id");

        var sharedPhase5Ids = new[]
        {
            "shell-system-integration-roundtrip", "shell-system-integration-v2-tray-payload",
            "shell-system-integration-v2-timestamp-normalization", "shell-system-integration-diagnostic-export"
        };
        AssertContainsAll(runtimeScenarioIds, sharedPhase5Ids, PhaseCloseoutConsistency, runtimeManifestPath);
        AssertContainsAll(matrixCapabilityIds, sharedPhase5Ids, PhaseCloseoutConsistency, productionMatrixPath);

        AssertSourceContains(File.ReadAllText(templateIndexPath), "window.runTemplateRegressionChecks", PhaseCloseoutConsistency, templateIndexPath);
        var bridgeSource = File.ReadAllText(hostCapabilityBridgePath);
        AssertSourceContains(bridgeSource, "ToExportRecord", PhaseCloseoutConsistency, hostCapabilityBridgePath);
        AssertSourceContains(bridgeSource, "WebViewHostCapabilityDiagnosticExportRecord", PhaseCloseoutConsistency, hostCapabilityBridgePath);
    }

    [Fact]
    public void Warning_governance_treats_windowsbase_conflicts_as_regressions()
    {
        var repoRoot = FindRepoRoot();
        var warningGovernancePath = Path.Combine(repoRoot, "build", "Build.WarningGovernance.cs");
        AssertFileExists(warningGovernancePath, WindowsBaseConflictGovernance);

        var source = File.ReadAllText(warningGovernancePath);
        AssertSourceContains(source, "WindowsBase conflict warning must be eliminated; baseline acceptance is not allowed.", WindowsBaseConflictGovernance, warningGovernancePath);
        Assert.DoesNotContain("WindowsBase conflict is governed by approved baseline metadata.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Package_metadata_contains_required_properties_for_stable_release()
    {
        var repoRoot = FindRepoRoot();
        var directoryBuildPropsPath = Path.Combine(repoRoot, "Directory.Build.props");
        AssertFileExists(directoryBuildPropsPath, PackageMetadata);

        var props = File.ReadAllText(directoryBuildPropsPath);
        AssertSourceContains(props, "<PackageLicenseExpression>", PackageMetadata, directoryBuildPropsPath);
        AssertSourceContains(props, "<PackageProjectUrl>", PackageMetadata, directoryBuildPropsPath);

        var mainCsprojPath = Path.Combine(repoRoot, "src", "Agibuild.Avalonia.WebView", "Agibuild.Avalonia.WebView.csproj");
        AssertFileExists(mainCsprojPath, PackageMetadata);

        var csproj = File.ReadAllText(mainCsprojPath);
        AssertSourceContains(csproj, "<Description>", PackageMetadata, mainCsprojPath);
        Assert.DoesNotContain("preview", csproj.ToLowerInvariant().Split("<Description>").Last().Split("</Description>").First());
    }

    [Fact]
    public void Readme_quality_signals_match_actual_test_evidence()
    {
        var repoRoot = FindRepoRoot();
        var readmePath = Path.Combine(repoRoot, "README.md");
        AssertFileExists(readmePath, ReadmeQualitySignals);

        var readme = File.ReadAllText(readmePath);

        Assert.True(
            Directory.GetFiles(Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.UnitTests"), "*Tests.cs", SearchOption.AllDirectories).Length > 0,
            $"[{ReadmeQualitySignals}] No unit test files found");
        Assert.True(
            Directory.GetFiles(Path.Combine(repoRoot, "tests", "Agibuild.Avalonia.WebView.Integration.Tests.Automation"), "*Tests.cs", SearchOption.AllDirectories).Length > 0,
            $"[{ReadmeQualitySignals}] No integration test files found");

        Assert.Matches(new Regex(@"\|\s*Unit tests\s*\|\s*\d{3,}\s*\|"), readme);
        Assert.Matches(new Regex(@"\|\s*Integration tests\s*\|\s*\d{2,}\s*\|"), readme);
        Assert.Matches(new Regex(@"\|\s*Line coverage\s*\|\s*\*\*\d+\.\d+%\*\*\s*\|"), readme);
        AssertSourceContains(readme, "Phase 5 | Framework Positioning Foundation | ✅ Completed", ReadmeQualitySignals, readmePath);
    }

    [Fact]
    public void Coverage_target_enforces_branch_coverage_threshold()
    {
        var repoRoot = FindRepoRoot();
        var buildSource = ReadCombinedBuildSource(repoRoot);

        var markers = new[] { "BranchCoverageThreshold", "branch-rate", "Branch coverage", "branchThreshold", "dependencyGovernanceReportExists" };
        foreach (var marker in markers)
            AssertSourceContains(buildSource, marker, CoverageThreshold, "build/Build*.cs");
    }

    [Fact]
    public void Shell_matrix_and_runtime_manifest_are_kept_in_sync_for_shell_capabilities()
    {
        var repoRoot = FindRepoRoot();
        var runtimeManifestPath = Path.Combine(repoRoot, "tests", "runtime-critical-path.manifest.json");
        var matrixPath = Path.Combine(repoRoot, "tests", "shell-production-matrix.json");

        using var runtimeDoc = LoadJsonArtifact(runtimeManifestPath, ShellManifestMatrixSync);
        using var matrixDoc = LoadJsonArtifact(matrixPath, ShellManifestMatrixSync);

        var runtimeShellIds = ExtractStringIds(runtimeDoc.RootElement.GetProperty("scenarios"), "id");
        var matrixCapabilityIds = ExtractStringIds(matrixDoc.RootElement.GetProperty("capabilities"), "id");

        AssertBidirectionalSync(
            runtimeShellIds, "runtime-critical-path",
            matrixCapabilityIds, "shell-production-matrix",
            ShellManifestMatrixSync,
            id => id.StartsWith("shell-", StringComparison.Ordinal));
    }

    [Fact]
    public void Benchmark_baseline_artifact_has_required_metrics_and_tolerance()
    {
        var repoRoot = FindRepoRoot();
        var baselinePath = Path.Combine(repoRoot, "tests", "performance-benchmark-baseline.json");

        using var doc = LoadJsonArtifact(baselinePath, BenchmarkBaselineSchema);
        RequireVersionField(doc.RootElement, BenchmarkBaselineSchema, baselinePath, minimumVersion: 1);

        var toleranceElement = RequireProperty(doc.RootElement, "allowedRegressionPercent", BenchmarkBaselineSchema, baselinePath);
        Assert.True(toleranceElement.GetDouble() > 0, $"[{BenchmarkBaselineSchema}] allowedRegressionPercent must be > 0.");

        var metrics = RequireProperty(doc.RootElement, "metrics", BenchmarkBaselineSchema, baselinePath);
        var metricsList = metrics.EnumerateArray().ToList();
        Assert.NotEmpty(metricsList);

        foreach (var metric in metricsList)
        {
            var id = metric.GetProperty("id").GetString();
            Assert.False(string.IsNullOrWhiteSpace(id), $"[{BenchmarkBaselineSchema}] Metric id must not be empty.");
            Assert.True(metric.GetProperty("baselineMs").GetDouble() > 0, $"[{BenchmarkBaselineSchema}] Metric '{id}' baselineMs must be > 0.");
        }
    }

    [Fact]
    public void Dx_assets_for_bridge_package_and_vue_sample_are_present_and_typed()
    {
        var repoRoot = FindRepoRoot();

        var requiredFiles = new[]
        {
            "packages/bridge/package.json", "packages/bridge/src/index.ts",
            "samples/avalonia-react/AvaloniReact.Web/package.json",
            "samples/avalonia-react/AvaloniReact.Web/src/bridge/services.ts",
            "samples/avalonia-react/AvaloniReact.Web/src/hooks/useBridge.ts",
            "samples/avalonia-vue/AvaloniVue.Web/src/main.ts",
            "samples/avalonia-vue/AvaloniVue.Web/package.json",
            "samples/avalonia-vue/AvaloniVue.Web/src/bridge/services.ts",
            "samples/avalonia-vue/AvaloniVue.Web/tsconfig.json"
        };
        foreach (var relPath in requiredFiles)
            AssertFileExists(Path.Combine(repoRoot, relPath.Replace('/', Path.DirectorySeparatorChar)), BridgeDxAssets);

        var bridgePackage = File.ReadAllText(Path.Combine(repoRoot, "packages", "bridge", "package.json"));
        var bridgeEntry = File.ReadAllText(Path.Combine(repoRoot, "packages", "bridge", "src", "index.ts"));
        var reactPackage = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-react", "AvaloniReact.Web", "package.json"));
        var reactBridge = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-react", "AvaloniReact.Web", "src", "bridge", "services.ts"));
        var reactBridgeHook = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-react", "AvaloniReact.Web", "src", "hooks", "useBridge.ts"));
        var vueEntry = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "src", "main.ts"));
        var vuePackage = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "package.json"));
        var vueBridge = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "src", "bridge", "services.ts"));
        var vueTsConfig = File.ReadAllText(Path.Combine(repoRoot, "samples", "avalonia-vue", "AvaloniVue.Web", "tsconfig.json"));

        AssertSourceContains(bridgePackage, "\"@agibuild/bridge\"", BridgeDxAssets, "packages/bridge/package.json");
        AssertSourceContains(bridgePackage, "\"prepare\": \"npm run build\"", BridgeDxAssets, "packages/bridge/package.json");
        AssertSourceContains(bridgeEntry, "createBridgeClient", BridgeDxAssets, "packages/bridge/src/index.ts");
        AssertSourceContains(bridgeEntry, "bridgeClient", BridgeDxAssets, "packages/bridge/src/index.ts");
        AssertSourceContains(bridgeEntry, "getService", BridgeDxAssets, "packages/bridge/src/index.ts");
        AssertSourceContains(reactPackage, "\"@agibuild/bridge\"", BridgeDxAssets, "samples/avalonia-react/.../package.json");
        AssertSourceContains(reactBridge, "bridgeClient.getService", BridgeDxAssets, "samples/avalonia-react/.../services.ts");
        AssertSourceContains(reactBridgeHook, "bridgeClient.ready", BridgeDxAssets, "samples/avalonia-react/.../useBridge.ts");
        AssertSourceContains(vueEntry, "getAppInfo", BridgeDxAssets, "samples/avalonia-vue/.../main.ts");
        AssertSourceContains(vuePackage, "\"@agibuild/bridge\"", BridgeDxAssets, "samples/avalonia-vue/.../package.json");
        AssertSourceContains(vueBridge, "bridgeClient.getService", BridgeDxAssets, "samples/avalonia-vue/.../services.ts");
        AssertSourceContains(vueTsConfig, "bridge.d.ts", BridgeDxAssets, "samples/avalonia-vue/.../tsconfig.json");
    }

    [Fact]
    public void Ci_evidence_snapshot_build_target_emits_v2_schema_with_provenance()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);

        AssertSourceContains(combinedSource, "schemaVersion = 2", EvidenceContractV2Schema, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "laneContext = \"CiPublish\"", EvidenceContractV2Schema, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "producerTarget = \"PhaseCloseoutSnapshot\"", EvidenceContractV2Schema, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "ci-evidence-snapshot.json", EvidenceContractV2Schema, "build/Build.cs");
    }

    [Fact]
    public void Bridge_distribution_governance_target_exists_in_cipublish_with_v2_provenance()
    {
        var repoRoot = FindRepoRoot();
        var combinedSource = ReadCombinedBuildSource(repoRoot);
        var mainSource = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.cs"));

        AssertSourceContains(combinedSource, "Target BridgeDistributionGovernance", BridgeDistributionParity, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "bridge-distribution-governance-report.json", BridgeDistributionParity, "build/Build*.cs");
        AssertSourceContains(combinedSource, "producerTarget = \"BridgeDistributionGovernance\"", BridgeDistributionParity, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "SMOKE_PASSED", BridgeDistributionParity, "build/Build.Governance.cs");
        AssertSourceContains(combinedSource, "LTS_IMPORT_OK", BridgeDistributionParity, "build/Build.Governance.cs");

        Assert.Matches(
            new Regex(@"Target\s+CiPublish\s*=>[\s\S]*?\.DependsOn\([\s\S]*BridgeDistributionGovernance[\s\S]*\);", RegexOptions.Multiline),
            mainSource);
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
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string? ExtractPackageVersion(string csprojXml, string packageId)
    {
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

        Assert.Fail($"[{XunitVersionAlignment}] Package version drift detected for '{packageId}'.\n{details}");
    }
}
