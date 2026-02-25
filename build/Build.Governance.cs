using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;

partial class BuildTask
{
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
                    threshold = CoverageThreshold
                },
                governance = new
                {
                    openSpecStrictGovernanceReportExists = File.Exists(OpenSpecStrictGovernanceReportFile),
                    automationLaneReportExists = File.Exists(AutomationLaneReportFile)
                },
                phase5Archives = closeoutArchives
            };

            File.WriteAllText(
                PhaseCloseoutSnapshotFile,
                JsonSerializer.Serialize(snapshotPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("Phase closeout snapshot written to {Path}", PhaseCloseoutSnapshotFile);
        });
}
