using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;

partial class BuildTask
{
    Target DependencyVulnerabilityGovernance => _ => _
        .Description("Runs dependency vulnerability scans (NuGet + npm) as a hard governance gate.")
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();

            var failures = new List<string>();
            var scanReports = new List<object>();

            var nugetOutput = RunProcessCaptureAll(
                "dotnet",
                $"list \"{SolutionFile}\" package --vulnerable --include-transitive",
                workingDirectory: RootDirectory,
                timeoutMs: 240_000);
            var nugetHasVulnerability = nugetOutput.Contains("has the following vulnerable packages", StringComparison.OrdinalIgnoreCase)
                                        || nugetOutput.Contains("vulnerable", StringComparison.OrdinalIgnoreCase)
                                        && nugetOutput.Contains("Severity", StringComparison.OrdinalIgnoreCase);
            scanReports.Add(new
            {
                ecosystem = "nuget",
                command = "dotnet list <solution> package --vulnerable --include-transitive",
                hasFindings = nugetHasVulnerability,
                output = nugetOutput
            });
            if (nugetHasVulnerability)
                failures.Add("NuGet vulnerability scan reported vulnerable packages.");

            var npmWorkspaces = new[]
            {
                ReactWebDirectory
            }
            .Where(path => File.Exists(path / "package-lock.json"))
            .Distinct()
            .ToArray();

            foreach (var workspace in npmWorkspaces)
            {
                var npmOutput = RunNpmCaptureAll(
                    "audit --json --audit-level=high",
                    workingDirectory: workspace,
                    timeoutMs: 180_000);

                var hasHighOrCritical = false;
                try
                {
                    using var doc = JsonDocument.Parse(npmOutput);
                    if (doc.RootElement.TryGetProperty("metadata", out var metadata)
                        && metadata.TryGetProperty("vulnerabilities", out var vulnerabilities))
                    {
                        var high = vulnerabilities.TryGetProperty("high", out var highNode) ? highNode.GetInt32() : 0;
                        var critical = vulnerabilities.TryGetProperty("critical", out var criticalNode) ? criticalNode.GetInt32() : 0;
                        hasHighOrCritical = high > 0 || critical > 0;
                    }
                }
                catch (JsonException)
                {
                    failures.Add($"npm audit output is not valid JSON for workspace '{workspace}'.");
                }

                scanReports.Add(new
                {
                    ecosystem = "npm",
                    workspace = workspace.ToString(),
                    command = "npm audit --json --audit-level=high",
                    hasFindings = hasHighOrCritical
                });

                if (hasHighOrCritical)
                    failures.Add($"npm audit found high/critical vulnerabilities in '{workspace}'.");
            }

            var reportPayload = new
            {
                generatedAtUtc = DateTime.UtcNow,
                scans = scanReports,
                failureCount = failures.Count,
                failures
            };

            File.WriteAllText(
                DependencyGovernanceReportFile,
                JsonSerializer.Serialize(reportPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("Dependency governance report written to {Path}", DependencyGovernanceReportFile);

            if (failures.Count > 0)
                Assert.Fail("Dependency vulnerability governance failed:\n" + string.Join('\n', failures));
        });

    Target TypeScriptDeclarationGovernance => _ => _
        .Description("Validates TypeScript declaration generation and DX package wiring contracts.")
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();

            var failures = new List<string>();
            var checks = new List<object>();

            var targetsPath = RootDirectory / "src" / "Agibuild.Fulora.Bridge.Generator" / "build" / "Agibuild.Fulora.Bridge.Generator.targets";
            if (!File.Exists(targetsPath))
            {
                failures.Add($"Missing bridge generator targets file: {targetsPath}");
            }
            else
            {
                var targets = File.ReadAllText(targetsPath);
                var hasGenerateFlag = targets.Contains("GenerateBridgeTypeScript", StringComparison.Ordinal);
                var hasOutputDir = targets.Contains("BridgeTypeScriptOutputDir", StringComparison.Ordinal);
                var hasBridgeDts = targets.Contains("bridge.d.ts", StringComparison.Ordinal);

                checks.Add(new
                {
                    file = targetsPath.ToString(),
                    hasGenerateFlag,
                    hasOutputDir,
                    hasBridgeDts
                });

                if (!hasGenerateFlag || !hasOutputDir || !hasBridgeDts)
                    failures.Add("Bridge generator targets are missing required bridge.d.ts generation wiring.");
            }

            var packageEntryPath = RootDirectory / "packages" / "bridge" / "src" / "index.ts";
            var packageConfigPath = RootDirectory / "packages" / "bridge" / "package.json";
            checks.Add(new
            {
                file = packageEntryPath.ToString(),
                exists = File.Exists(packageEntryPath)
            });
            checks.Add(new
            {
                file = packageConfigPath.ToString(),
                exists = File.Exists(packageConfigPath)
            });
            if (!File.Exists(packageEntryPath) || !File.Exists(packageConfigPath))
                failures.Add("Bridge npm package source is incomplete.");

            var vueTsconfigPath = RootDirectory / "samples" / "avalonia-vue" / "AvaloniVue.Web" / "tsconfig.json";
            if (!File.Exists(vueTsconfigPath))
            {
                failures.Add($"Missing Vue sample tsconfig: {vueTsconfigPath}");
            }
            else
            {
                var vueTsconfig = File.ReadAllText(vueTsconfigPath);
                var referencesBridgeDeclaration = vueTsconfig.Contains("bridge.d.ts", StringComparison.Ordinal);
                checks.Add(new
                {
                    file = vueTsconfigPath.ToString(),
                    referencesBridgeDeclaration
                });
                if (!referencesBridgeDeclaration)
                    failures.Add("Vue sample tsconfig must include generated bridge.d.ts.");
            }

            var reportPayload = new
            {
                generatedAtUtc = DateTime.UtcNow,
                checks,
                failureCount = failures.Count,
                failures
            };

            File.WriteAllText(
                TypeScriptGovernanceReportFile,
                JsonSerializer.Serialize(reportPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("TypeScript governance report written to {Path}", TypeScriptGovernanceReportFile);

            if (failures.Count > 0)
                Assert.Fail("TypeScript declaration governance failed:\n" + string.Join('\n', failures));
        });

    Target OpenSpecStrictGovernance => _ => _
        .Description("Runs OpenSpec strict validation as a hard governance gate.")
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();
            var output = OperatingSystem.IsWindows()
                ? RunProcessCaptureAllChecked(
                    "powershell",
                    "-NoLogo -NoProfile -ExecutionPolicy Bypass -Command \"npm exec --yes @fission-ai/openspec -- validate --all --strict\"",
                    workingDirectory: RootDirectory,
                    timeoutMs: 180_000)
                : RunProcessCaptureAllChecked(
                    "bash",
                    "-lc \"npm exec --yes @fission-ai/openspec -- validate --all --strict\"",
                    workingDirectory: RootDirectory,
                    timeoutMs: 180_000);
            File.WriteAllText(OpenSpecStrictGovernanceReportFile, output);
            Serilog.Log.Information("OpenSpec strict governance report written to {Path}", OpenSpecStrictGovernanceReportFile);
        });

    Target RuntimeCriticalPathExecutionGovernanceCi => _ => _
        .Description("Validates runtime critical-path execution evidence (Ci context).")
        .DependsOn(AutomationLaneReport)
        .Executes(() =>
        {
            ValidateRuntimeCriticalPathExecutionEvidence(includeCiPublishContext: false);
        });

    Target RuntimeCriticalPathExecutionGovernanceCiPublish => _ => _
        .Description("Validates runtime critical-path execution evidence (Ci + CiPublish contexts).")
        .DependsOn(AutomationLaneReport, NugetPackageTest)
        .Executes(() =>
        {
            ValidateRuntimeCriticalPathExecutionEvidence(includeCiPublishContext: true);
        });

    void ValidateRuntimeCriticalPathExecutionEvidence(bool includeCiPublishContext)
    {
        TestResultsDirectory.CreateDirectory();
        if (!File.Exists(RuntimeCriticalPathManifestFile))
            Assert.Fail($"Missing runtime critical-path manifest: {RuntimeCriticalPathManifestFile}");

        var runtimeTrxPath = ResolveFirstExistingPath(
            TestResultsDirectory / "runtime-automation.trx",
            TestResultsDirectory / "integration-tests.trx");
        var contractTrxPath = ResolveFirstExistingPath(
            TestResultsDirectory / "contract-automation.trx",
            TestResultsDirectory / "unit-tests.trx");

        var runtimePassed = runtimeTrxPath is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : ReadPassedTestNamesFromTrx(runtimeTrxPath);
        var contractPassed = contractTrxPath is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : ReadPassedTestNamesFromTrx(contractTrxPath);

        using var manifestDoc = JsonDocument.Parse(File.ReadAllText(RuntimeCriticalPathManifestFile));
        var scenarios = manifestDoc.RootElement.GetProperty("scenarios").EnumerateArray().ToArray();

        var failures = new List<string>();
        var checks = new List<object>();

        foreach (var scenario in scenarios)
        {
            var id = scenario.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
            var lane = scenario.TryGetProperty("lane", out var laneNode) ? laneNode.GetString() : null;
            var file = scenario.TryGetProperty("file", out var fileNode) ? fileNode.GetString() : null;
            var testMethod = scenario.TryGetProperty("testMethod", out var methodNode) ? methodNode.GetString() : null;
            var ciContext = scenario.TryGetProperty("ciContext", out var contextNode) ? contextNode.GetString() : "Ci";

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(lane))
            {
                failures.Add("Runtime critical-path scenario is missing required id/lane fields.");
                continue;
            }

            var inScope = string.Equals(ciContext, "Ci", StringComparison.Ordinal)
                          || (includeCiPublishContext && string.Equals(ciContext, "CiPublish", StringComparison.Ordinal));
            if (!inScope)
                continue;

            if (string.IsNullOrWhiteSpace(file))
            {
                failures.Add($"Scenario '{id}' is missing required file field.");
                continue;
            }

            if (string.Equals(file, "build/Build.cs", StringComparison.Ordinal))
            {
                if (string.Equals(id, "package-consumption-smoke", StringComparison.Ordinal))
                {
                    var telemetryExists = File.Exists(NugetSmokeTelemetryFile);
                    checks.Add(new
                    {
                        id,
                        lane,
                        ciContext,
                        evidenceType = "nuget-smoke-telemetry",
                        telemetryPath = NugetSmokeTelemetryFile.ToString(),
                        passed = telemetryExists
                    });

                    if (includeCiPublishContext && !telemetryExists)
                        failures.Add($"Scenario '{id}' requires NuGet smoke telemetry evidence at '{NugetSmokeTelemetryFile}'.");
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(testMethod))
            {
                failures.Add($"Scenario '{id}' must declare testMethod for test evidence validation.");
                continue;
            }

            HashSet<string>? passedTests = lane.StartsWith("RuntimeAutomation", StringComparison.Ordinal)
                ? runtimePassed
                : lane.StartsWith("ContractAutomation", StringComparison.Ordinal)
                    ? contractPassed
                    : null;

            if (passedTests is null)
            {
                failures.Add($"Scenario '{id}' has unsupported lane '{lane}'.");
                continue;
            }

            var passed = HasPassedTestMethod(passedTests, testMethod);
            checks.Add(new
            {
                id,
                lane,
                ciContext,
                testMethod,
                passed
            });

            if (!passed)
                failures.Add($"Scenario '{id}' expected passed test evidence for method '{testMethod}' in lane '{lane}'.");
        }

        var reportPayload = new
        {
            schemaVersion = 2,
            provenance = new
            {
                laneContext = includeCiPublishContext ? "CiPublish" : "Ci",
                producerTarget = includeCiPublishContext
                    ? "RuntimeCriticalPathExecutionGovernanceCiPublish"
                    : "RuntimeCriticalPathExecutionGovernanceCi",
                timestamp = DateTime.UtcNow.ToString("o")
            },
            includeCiPublishContext,
            manifestPath = RuntimeCriticalPathManifestFile.ToString(),
            runtimeTrxPath = runtimeTrxPath?.ToString(),
            contractTrxPath = contractTrxPath?.ToString(),
            checks,
            failureCount = failures.Count,
            failures
        };

        File.WriteAllText(
            RuntimeCriticalPathGovernanceReportFile,
            JsonSerializer.Serialize(reportPayload, new JsonSerializerOptions { WriteIndented = true }));
        Serilog.Log.Information("Runtime critical-path governance report written to {Path}", RuntimeCriticalPathGovernanceReportFile);

        if (failures.Count > 0)
            Assert.Fail("Runtime critical-path execution governance failed:\n" + string.Join('\n', failures));
    }

    Target BridgeDistributionGovernance => _ => _
        .Description("Validates @agibuild/bridge npm package builds and imports across package managers and Node LTS.")
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();

            var bridgeDir = RootDirectory / "packages" / "bridge";
            var distIndex = bridgeDir / "dist" / "index.js";
            var checks = new List<object>();
            var failures = new List<string>();

            var nodeVersion = RunProcessCaptureAll("node", "--version", workingDirectory: RootDirectory, timeoutMs: 10_000).Trim();

            // 1. Build bridge package with npm (canonical path)
            try
            {
                RunNpmCaptureAll("install", workingDirectory: bridgeDir, timeoutMs: 120_000);
                RunNpmCaptureAll("run build", workingDirectory: bridgeDir, timeoutMs: 60_000);
                checks.Add(new { manager = "npm", phase = "install+build", passed = true });
            }
            catch (Exception ex)
            {
                failures.Add($"npm install+build failed: {ex.Message}");
                checks.Add(new { manager = "npm", phase = "install+build", passed = false, error = ex.Message });
            }

            // 2. Package-manager parity: pnpm and yarn consume smoke
            foreach (var pm in new[] { "pnpm", "yarn" })
            {
                var available = IsToolAvailable(pm);
                if (!available)
                {
                    checks.Add(new { manager = pm, phase = "consume-smoke", passed = true, skipped = true, reason = $"{pm} not installed" });
                    Serilog.Log.Warning("Skipping {Pm} parity check — tool not found on PATH.", pm);
                    continue;
                }

                AbsolutePath? tempDir = null;
                try
                {
                    tempDir = (AbsolutePath)Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"bridge-{pm}-smoke-{Guid.NewGuid():N}"));
                    Directory.CreateDirectory(tempDir);

                    var packageJson = $$"""
                        {
                          "name": "bridge-{{pm}}-smoke",
                          "version": "1.0.0",
                          "private": true,
                          "type": "module",
                          "dependencies": {
                            "@agibuild/bridge": "file:{{bridgeDir.ToString().Replace("\\", "/")}}"
                          }
                        }
                        """;
                    File.WriteAllText(tempDir / "package.json", packageJson);

                    var consumerScript = """
                        import { createBridgeClient } from '@agibuild/bridge';
                        if (typeof createBridgeClient !== 'function') process.exit(1);
                        console.log('SMOKE_PASSED');
                        """;
                    File.WriteAllText(tempDir / "consumer.mjs", consumerScript);

                    RunPmInstall(pm, tempDir);
                    var output = RunProcessCaptureAll("node", "consumer.mjs", workingDirectory: tempDir, timeoutMs: 30_000);
                    var passed = output.Contains("SMOKE_PASSED", StringComparison.Ordinal);
                    checks.Add(new { manager = pm, phase = "consume-smoke", passed });
                    if (!passed)
                        failures.Add($"{pm} consume smoke did not produce SMOKE_PASSED.");
                }
                catch (Exception ex)
                {
                    failures.Add($"{pm} consume smoke failed: {ex.Message}");
                    checks.Add(new { manager = pm, phase = "consume-smoke", passed = false, error = ex.Message });
                }
                finally
                {
                    if (tempDir is not null && Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, recursive: true); }
                        catch { /* best-effort cleanup */ }
                    }
                }
            }

            // 3. Node LTS import smoke
            if (File.Exists(distIndex))
            {
                try
                {
                    var importCheck = RunProcessCaptureAll(
                        "node",
                        $"-e \"const b = require('{distIndex.ToString().Replace("\\", "/")}'); if (typeof b.createBridgeClient !== 'function') process.exit(1); console.log('LTS_IMPORT_OK');\"",
                        workingDirectory: RootDirectory,
                        timeoutMs: 10_000);
                    var passed = importCheck.Contains("LTS_IMPORT_OK", StringComparison.Ordinal);
                    checks.Add(new { phase = "node-lts-import", nodeVersion, passed });
                    if (!passed)
                        failures.Add($"Node LTS import check failed on {nodeVersion}.");
                }
                catch (Exception ex)
                {
                    failures.Add($"Node LTS import check failed: {ex.Message}");
                    checks.Add(new { phase = "node-lts-import", nodeVersion, passed = false, error = ex.Message });
                }
            }
            else
            {
                failures.Add($"Bridge dist/index.js not found at {distIndex}. Build may have failed.");
                checks.Add(new { phase = "node-lts-import", nodeVersion, passed = false, error = "dist/index.js not found" });
            }

            var reportPayload = new
            {
                schemaVersion = 2,
                provenance = new
                {
                    laneContext = "CiPublish",
                    producerTarget = "BridgeDistributionGovernance",
                    timestamp = DateTime.UtcNow.ToString("o")
                },
                nodeVersion,
                checks,
                failureCount = failures.Count,
                failures
            };

            File.WriteAllText(
                BridgeDistributionGovernanceReportFile,
                JsonSerializer.Serialize(reportPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("Bridge distribution governance report written to {Path}", BridgeDistributionGovernanceReportFile);

            if (failures.Count > 0)
                Assert.Fail("Bridge distribution governance failed:\n" + string.Join('\n', failures));
        });

    static bool IsToolAvailable(string toolName)
    {
        try
        {
            var fileName = OperatingSystem.IsWindows() ? "where" : "which";
            RunProcessCaptureAll(fileName, toolName, timeoutMs: 5_000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void RunPmInstall(string pm, string workingDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            RunProcessCaptureAllChecked("cmd.exe", $"/d /s /c \"{pm} install\"",
                workingDirectory: workingDirectory, timeoutMs: 120_000);
        }
        else
        {
            RunProcessCaptureAllChecked(pm, "install",
                workingDirectory: workingDirectory, timeoutMs: 120_000);
        }
    }

    Target PhaseCloseoutSnapshot => _ => _
        .Description("Generates machine-readable CI evidence snapshot (v2) from test/coverage artifacts.")
        .DependsOn(Coverage, AutomationLaneReport, OpenSpecStrictGovernance)
        .Executes(() =>
        {
            TestResultsDirectory.CreateDirectory();

            var unitTrxPath = ResolveFirstExistingPath(
                TestResultsDirectory / "unit-tests.trx",
                CoverageDirectory / "unit-tests.trx");
            var integrationTrxPath = ResolveFirstExistingPath(
                TestResultsDirectory / "integration-tests.trx",
                TestResultsDirectory / "runtime-automation.trx");
            var coberturaPath = ResolveFirstExistingPath(
                CoverageReportDirectory / "Cobertura.xml",
                CoverageDirectory.GlobFiles("**/coverage.cobertura.xml").FirstOrDefault());

            if (unitTrxPath is null)
                Assert.Fail("CI evidence snapshot requires unit test TRX file (unit-tests.trx).");
            if (integrationTrxPath is null)
                Assert.Fail("CI evidence snapshot requires integration/runtime automation TRX file.");
            if (coberturaPath is null)
                Assert.Fail("CI evidence snapshot requires Cobertura coverage report.");

            var unitCounters = ReadTrxCounters(unitTrxPath!);
            var integrationCounters = ReadTrxCounters(integrationTrxPath!);
            var lineCoveragePct = ReadCoberturaLineCoveragePercent(coberturaPath!);
            var branchCoveragePct = ReadCoberturaBranchCoveragePercent(coberturaPath!);

            var archiveDirectory = RootDirectory / "openspec" / "changes" / "archive";
            var requiredCloseoutChangeIds = new[]
            {
                "system-integration-contract-v2-freeze",
                "template-webfirst-dx-panel",
                "system-integration-diagnostic-export"
            };
            var closeoutArchives = Directory.Exists(archiveDirectory)
                ? requiredCloseoutChangeIds
                    .Select(changeId => Directory.GetDirectories(archiveDirectory)
                        .Select(Path.GetFileName)
                        .FirstOrDefault(name => name is not null && name.EndsWith(changeId, StringComparison.Ordinal)))
                    .Where(name => name is not null)
                    .Cast<string>()
                    .ToArray()
                : Array.Empty<string>();

            var snapshotPayload = new
            {
                schemaVersion = 2,
                provenance = new
                {
                    laneContext = "CiPublish",
                    producerTarget = "PhaseCloseoutSnapshot",
                    timestamp = DateTime.UtcNow.ToString("o")
                },
                sourcePaths = new
                {
                    unitTrx = unitTrxPath!.ToString(),
                    integrationTrx = integrationTrxPath!.ToString(),
                    cobertura = coberturaPath!.ToString(),
                    openSpecStrictGovernance = OpenSpecStrictGovernanceReportFile.ToString()
                },
                tests = new
                {
                    unit = new
                    {
                        total = unitCounters.Total,
                        passed = unitCounters.Passed,
                        failed = unitCounters.Failed,
                        skipped = unitCounters.Skipped
                    },
                    integration = new
                    {
                        total = integrationCounters.Total,
                        passed = integrationCounters.Passed,
                        failed = integrationCounters.Failed,
                        skipped = integrationCounters.Skipped
                    },
                    total = new
                    {
                        total = unitCounters.Total + integrationCounters.Total,
                        passed = unitCounters.Passed + integrationCounters.Passed,
                        failed = unitCounters.Failed + integrationCounters.Failed,
                        skipped = unitCounters.Skipped + integrationCounters.Skipped
                    }
                },
                coverage = new
                {
                    linePercent = Math.Round(lineCoveragePct, 2),
                    lineThreshold = CoverageThreshold,
                    branchPercent = Math.Round(branchCoveragePct, 2),
                    branchThreshold = BranchCoverageThreshold
                },
                governance = new
                {
                    openSpecStrictGovernanceReportExists = File.Exists(OpenSpecStrictGovernanceReportFile),
                    automationLaneReportExists = File.Exists(AutomationLaneReportFile),
                    dependencyGovernanceReportExists = File.Exists(DependencyGovernanceReportFile),
                    typeScriptGovernanceReportExists = File.Exists(TypeScriptGovernanceReportFile),
                    runtimeCriticalPathGovernanceReportExists = File.Exists(RuntimeCriticalPathGovernanceReportFile)
                },
                phase5Archives = closeoutArchives
            };

            File.WriteAllText(
                CiEvidenceSnapshotFile,
                JsonSerializer.Serialize(snapshotPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("CI evidence snapshot (v2) written to {Path}", CiEvidenceSnapshotFile);
        });
}
