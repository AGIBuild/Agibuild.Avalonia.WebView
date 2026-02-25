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
                var npmOutput = RunProcessCaptureAll(
                    "npm",
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

            var targetsPath = RootDirectory / "src" / "Agibuild.Avalonia.WebView.Bridge.Generator" / "build" / "Agibuild.Avalonia.WebView.Bridge.Generator.targets";
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

    Target PhaseCloseoutSnapshot => _ => _
        .Description("Generates machine-readable Phase 5 closeout evidence snapshot from test/coverage artifacts.")
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
                Assert.Fail("Phase closeout snapshot requires unit test TRX file (unit-tests.trx).");
            if (integrationTrxPath is null)
                Assert.Fail("Phase closeout snapshot requires integration/runtime automation TRX file.");
            if (coberturaPath is null)
                Assert.Fail("Phase closeout snapshot requires Cobertura coverage report.");

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
                generatedAtUtc = DateTime.UtcNow,
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
                    typeScriptGovernanceReportExists = File.Exists(TypeScriptGovernanceReportFile)
                },
                phase5Archives = closeoutArchives
            };

            File.WriteAllText(
                PhaseCloseoutSnapshotFile,
                JsonSerializer.Serialize(snapshotPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("Phase closeout snapshot written to {Path}", PhaseCloseoutSnapshotFile);
        });
}
