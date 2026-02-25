using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class BuildTask
{
    Target UnitTests => _ => _
        .Description("Runs unit tests.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(UnitTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=unit-tests.trx"));
        });

    Target Coverage => _ => _
        .Description("Runs unit tests with code coverage and enforces minimum threshold.")
        .DependsOn(Build)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                .SetProjectFile(UnitTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(CoverageDirectory)
                .SetLoggers("trx;LogFileName=unit-tests.trx")
                .SetSettingsFile(RootDirectory / "coverlet.runsettings"));

            var coverageFiles = CoverageDirectory.GlobFiles("**/coverage.cobertura.xml");
            if (!coverageFiles.Any())
            {
                Assert.Fail("No coverage files found. Ensure coverlet.collector is referenced in the test project.");
            }

            var coverageFile = coverageFiles.First();

            DotNet(
                $"reportgenerator " +
                $"\"-reports:{coverageFile}\" " +
                $"\"-targetdir:{CoverageReportDirectory}\" " +
                $"\"-reporttypes:Html;Cobertura;TextSummary\" " +
                $"\"-assemblyfilters:+Agibuild.Avalonia.WebView.*;-Agibuild.Avalonia.WebView.Testing;-Agibuild.Avalonia.WebView.UnitTests\"",
                workingDirectory: RootDirectory);

            var mergedCoberturaFile = CoverageReportDirectory / "Cobertura.xml";
            var coberturaPath = (string)(File.Exists(mergedCoberturaFile) ? mergedCoberturaFile : coverageFile);
            var doc = XDocument.Load(coberturaPath);
            var lineRateAttr = doc.Root?.Attribute("line-rate")?.Value;
            var branchRateAttr = doc.Root?.Attribute("branch-rate")?.Value;

            if (lineRateAttr is null || !double.TryParse(lineRateAttr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lineRate))
            {
                Assert.Fail("Unable to parse line-rate from coverage report.");
                return;
            }

            if (branchRateAttr is null || !double.TryParse(branchRateAttr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var branchRate))
            {
                Assert.Fail("Unable to parse branch-rate from coverage report.");
                return;
            }

            var lineCoveragePct = lineRate * 100;
            var branchCoveragePct = branchRate * 100;
            Serilog.Log.Information("Line coverage: {Coverage:F2}% (threshold: {Threshold}%)", lineCoveragePct, CoverageThreshold);
            Serilog.Log.Information("Branch coverage: {Coverage:F2}% (threshold: {Threshold}%)", branchCoveragePct, BranchCoverageThreshold);
            Serilog.Log.Information("HTML report: {Path}", CoverageReportDirectory / "index.html");

            if (lineCoveragePct < CoverageThreshold)
            {
                Assert.Fail(
                    $"Line coverage {lineCoveragePct:F2}% is below the required threshold of {CoverageThreshold}%. " +
                    $"Review the report at {CoverageReportDirectory / "index.html"}");
            }

            if (branchCoveragePct < BranchCoverageThreshold)
            {
                Assert.Fail(
                    $"Branch coverage {branchCoveragePct:F2}% is below the required threshold of {BranchCoverageThreshold}%. " +
                    $"Review the report at {CoverageReportDirectory / "index.html"}");
            }

            Serilog.Log.Information(
                "Coverage gate PASSED: line {LineCoverage:F2}% >= {LineThreshold}%, branch {BranchCoverage:F2}% >= {BranchThreshold}%",
                lineCoveragePct, CoverageThreshold, branchCoveragePct, BranchCoverageThreshold);

            var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrEmpty(summaryPath))
            {
                var textSummaryFile = CoverageReportDirectory / "Summary.txt";
                var summaryContent = File.Exists(textSummaryFile)
                    ? File.ReadAllText(textSummaryFile)
                    : $"Line coverage: {lineCoveragePct:F2}%, Branch coverage: {branchCoveragePct:F2}%";

                var markdown =
                    $"## Code Coverage Report\n\n" +
                    $"| Metric | Value |\n" +
                    $"|--------|-------|\n" +
                    $"| **Line Coverage** | **{lineCoveragePct:F2}%** |\n" +
                    $"| **Branch Coverage** | **{branchCoveragePct:F2}%** |\n" +
                    $"| Line Threshold | {CoverageThreshold}% |\n" +
                    $"| Branch Threshold | {BranchCoverageThreshold}% |\n" +
                    $"| Status | {(lineCoveragePct >= CoverageThreshold && branchCoveragePct >= BranchCoverageThreshold ? "PASSED" : "FAILED")} |\n\n" +
                    $"<details><summary>Full Summary</summary>\n\n```\n{summaryContent}\n```\n\n</details>\n";

                File.AppendAllText(summaryPath, markdown);
            }
        });

    Target IntegrationTests => _ => _
        .Description("Runs automated integration tests.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(IntegrationTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=integration-tests.trx"));
        });

    Target ContractAutomation => _ => _
        .Description("Runs ContractAutomation lane (mock-driven unit tests).")
        .DependsOn(Build)
        .Executes(() =>
        {
            RunContractAutomationTests("contract-automation.trx");
        });

    Target RuntimeAutomation => _ => _
        .Description("Runs RuntimeAutomation lane (real adapter/runtime automation tests).")
        .DependsOn(Build)
        .Executes(() =>
        {
            RunRuntimeAutomationTests("runtime-automation.trx");
        });

    Target AutomationLaneReport => _ => _
        .Description("Runs automation lanes and writes pass/fail/skip report.")
        .DependsOn(Build)
        .Executes(() =>
        {
            var lanes = new List<AutomationLaneResult>();
            var failures = new List<string>();

            RunLaneWithReporting(
                lane: ContractAutomationLane,
                project: UnitTestsProject,
                run: () => RunContractAutomationTests("contract-automation.trx"),
                lanes,
                failures);

            RunLaneWithReporting(
                lane: RuntimeAutomationLane,
                project: IntegrationTestsProject,
                run: () => RunRuntimeAutomationTests("runtime-automation.trx"),
                lanes,
                failures);

            if (!OperatingSystem.IsMacOS())
            {
                lanes.Add(new AutomationLaneResult(
                    Lane: $"{RuntimeAutomationLane}.iOS",
                    Status: "skipped",
                    Project: E2EiOSProject.ToString(),
                    Reason: "Requires macOS host with iOS simulator tooling."));
            }
            else if (!HasDotNetWorkload("ios"))
            {
                lanes.Add(new AutomationLaneResult(
                    Lane: $"{RuntimeAutomationLane}.iOS",
                    Status: "skipped",
                    Project: E2EiOSProject.ToString(),
                    Reason: "iOS workload not installed."));
            }

            if (!HasDotNetWorkload("android"))
            {
                lanes.Add(new AutomationLaneResult(
                    Lane: $"{RuntimeAutomationLane}.Android",
                    Status: "skipped",
                    Project: E2EAndroidProject.ToString(),
                    Reason: "Android workload not installed."));
            }

            if (!OperatingSystem.IsLinux())
            {
                lanes.Add(new AutomationLaneResult(
                    Lane: $"{RuntimeAutomationLane}.Gtk",
                    Status: "skipped",
                    Project: E2EDesktopProject.ToString(),
                    Reason: "Requires Linux host with WebKitGTK and a display server (Xvfb on CI)."));
            }
            else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"))
                     && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            {
                lanes.Add(new AutomationLaneResult(
                    Lane: $"{RuntimeAutomationLane}.Gtk",
                    Status: "skipped",
                    Project: E2EDesktopProject.ToString(),
                    Reason: "No DISPLAY/WAYLAND_DISPLAY detected; cannot run GTK smoke without a display server."));
            }
            else
            {
                RunLaneWithReporting(
                    lane: $"{RuntimeAutomationLane}.Gtk",
                    project: E2EDesktopProject,
                    run: () => RunGtkSmokeDesktopApp(),
                    lanes,
                    failures);
            }

            TestResultsDirectory.CreateDirectory();
            var laneManifestExists = File.Exists(AutomationLaneManifestFile);
            var criticalPathManifestExists = File.Exists(RuntimeCriticalPathManifestFile);
            var reportPayload = new
            {
                generatedAtUtc = DateTime.UtcNow,
                laneManifestPath = AutomationLaneManifestFile.ToString(),
                runtimeCriticalPathManifestPath = RuntimeCriticalPathManifestFile.ToString(),
                laneManifestExists,
                runtimeCriticalPathManifestExists = criticalPathManifestExists,
                lanes
            };

            File.WriteAllText(
                AutomationLaneReportFile,
                JsonSerializer.Serialize(reportPayload, new JsonSerializerOptions { WriteIndented = true }));
            Serilog.Log.Information("Automation lane report written to {Path}", AutomationLaneReportFile);

            if (failures.Count > 0)
            {
                Assert.Fail("Automation lane failures:\n" + string.Join('\n', failures));
            }
        });

    Target Test => _ => _
        .Description("Runs all tests (unit + integration).")
        .DependsOn(UnitTests, IntegrationTests);
}
