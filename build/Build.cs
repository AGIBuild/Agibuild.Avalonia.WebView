using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "ci",
    GitHubActionsImage.MacOsLatest,
    On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest],
    InvokedTargets = [nameof(Ci)],
    AutoGenerate = false)]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    // ──────────────────────────────── Parameters ────────────────────────────────

    [Parameter("Configuration (Debug / Release). Default: Release on CI, Debug locally.")]
    readonly string Configuration = IsServerBuild ? "Release" : "Debug";

    [Parameter("NuGet package version. Default: 0.0.0-dev")]
    readonly string PackageVersion = "0.0.0-dev";

    [Parameter("NuGet source URL for publish. Default: https://api.nuget.org/v3/index.json")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter("NuGet API key for publish.")]
    [Secret]
    readonly string? NuGetApiKey;

    [Parameter("Minimum line coverage percentage (0-100). Default: 90")]
    readonly int CoverageThreshold = 90;

    // ──────────────────────────────── Paths ──────────────────────────────────────

    AbsolutePath SrcDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackageOutputDirectory => ArtifactsDirectory / "packages";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

    AbsolutePath SolutionFile => RootDirectory / "Agibuild.Avalonia.WebView.sln";
    AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";
    AbsolutePath CoverageReportDirectory => ArtifactsDirectory / "coverage-report";

    // Pack project
    AbsolutePath PackProject =>
        SrcDirectory / "Agibuild.Avalonia.WebView" / "Agibuild.Avalonia.WebView.csproj";

    // Test projects
    AbsolutePath UnitTestsProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.UnitTests" / "Agibuild.Avalonia.WebView.UnitTests.csproj";

    AbsolutePath IntegrationTestsProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests.Automation"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj";

    AbsolutePath E2EDesktopProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Desktop"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Desktop.csproj";

    // ──────────────────────────────── Targets ────────────────────────────────────

    Target Clean => _ => _
        .Description("Cleans bin/obj directories and the artifacts folder.")
        .Executes(() =>
        {
            SrcDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Description("Restores NuGet packages for the solution.")
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(SolutionFile));
        });

    Target Compile => _ => _
        .Description("Builds all platform-appropriate projects.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            foreach (var project in GetProjectsToBuild())
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }
        });

    Target UnitTests => _ => _
        .Description("Runs unit tests.")
        .DependsOn(Compile)
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
        .DependsOn(Compile)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            // Run tests with coverlet data collector.
            DotNetTest(s => s
                .SetProjectFile(UnitTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(CoverageDirectory)
                .SetLoggers("trx;LogFileName=unit-tests.trx")
                .SetSettingsFile(RootDirectory / "coverlet.runsettings"));

            // Find the generated cobertura XML.
            var coverageFiles = CoverageDirectory.GlobFiles("**/coverage.cobertura.xml");
            if (!coverageFiles.Any())
            {
                Assert.Fail("No coverage files found. Ensure coverlet.collector is referenced in the test project.");
            }

            var coverageFile = coverageFiles.First();

            // Generate HTML report + enforce threshold via ReportGenerator.
            DotNet(
                $"reportgenerator " +
                $"\"-reports:{coverageFile}\" " +
                $"\"-targetdir:{CoverageReportDirectory}\" " +
                $"\"-reporttypes:Html;Cobertura;TextSummary\" " +
                $"\"-assemblyfilters:+Agibuild.Avalonia.WebView.*;-Agibuild.Avalonia.WebView.Testing;-Agibuild.Avalonia.WebView.UnitTests\"",
                workingDirectory: RootDirectory);

            // Parse the Cobertura XML to extract line coverage and validate threshold.
            var mergedCoberturaFile = CoverageReportDirectory / "Cobertura.xml";
            var coberturaPath = (string)(File.Exists(mergedCoberturaFile) ? mergedCoberturaFile : coverageFile);
            var doc = XDocument.Load(coberturaPath);
            var lineRateAttr = doc.Root?.Attribute("line-rate")?.Value;

            if (lineRateAttr is null || !double.TryParse(lineRateAttr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lineRate))
            {
                Assert.Fail("Unable to parse line-rate from coverage report.");
                return; // unreachable, but keeps compiler happy
            }

            var lineCoveragePct = lineRate * 100;
            Serilog.Log.Information("Line coverage: {Coverage:F2}% (threshold: {Threshold}%)", lineCoveragePct, CoverageThreshold);
            Serilog.Log.Information("HTML report: {Path}", CoverageReportDirectory / "index.html");

            if (lineCoveragePct < CoverageThreshold)
            {
                Assert.Fail(
                    $"Line coverage {lineCoveragePct:F2}% is below the required threshold of {CoverageThreshold}%. " +
                    $"Review the report at {CoverageReportDirectory / "index.html"}");
            }

            Serilog.Log.Information("Coverage gate PASSED: {Coverage:F2}% >= {Threshold}%", lineCoveragePct, CoverageThreshold);

            // Write coverage summary to GitHub Actions Job Summary when running on CI.
            var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrEmpty(summaryPath))
            {
                var textSummaryFile = CoverageReportDirectory / "Summary.txt";
                var summaryContent = File.Exists(textSummaryFile)
                    ? File.ReadAllText(textSummaryFile)
                    : $"Line coverage: {lineCoveragePct:F2}%";

                var markdown =
                    $"## Code Coverage Report\n\n" +
                    $"| Metric | Value |\n" +
                    $"|--------|-------|\n" +
                    $"| **Line Coverage** | **{lineCoveragePct:F2}%** |\n" +
                    $"| Threshold | {CoverageThreshold}% |\n" +
                    $"| Status | {(lineCoveragePct >= CoverageThreshold ? "PASSED" : "FAILED")} |\n\n" +
                    $"<details><summary>Full Summary</summary>\n\n```\n{summaryContent}\n```\n\n</details>\n";

                File.AppendAllText(summaryPath, markdown);
            }
        });

    Target IntegrationTests => _ => _
        .Description("Runs automated integration tests.")
        .DependsOn(Compile)
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

    Target Test => _ => _
        .Description("Runs all tests (unit + integration).")
        .DependsOn(UnitTests, IntegrationTests);

    Target Pack => _ => _
        .Description("Creates the NuGet package (.nupkg).")
        .DependsOn(Compile)
        .Produces(PackageOutputDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackageOutputDirectory.CreateOrCleanDirectory();

            DotNetPack(s => s
                .SetProject(PackProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetVersion(PackageVersion)
                .SetOutputDirectory(PackageOutputDirectory));
        });

    Target Publish => _ => _
        .Description("Pushes the NuGet package to the configured source.")
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            var packages = PackageOutputDirectory.GlobFiles("*.nupkg")
                .Where(p => !p.Name.EndsWith(".symbols.nupkg"));

            foreach (var package in packages)
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .EnableSkipDuplicate());
            }
        });

    Target Start => _ => _
        .Description("Launches the E2E integration test desktop app.")
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(E2EDesktopProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Ci => _ => _
        .Description("Full CI pipeline: compile → coverage → pack.")
        .DependsOn(Coverage, Pack);

    Target CiPublish => _ => _
        .Description("Full CI/CD pipeline: compile → coverage → pack → publish.")
        .DependsOn(Coverage, Publish);

    // ──────────────────────────────── Helpers ────────────────────────────────────

    IEnumerable<AbsolutePath> GetProjectsToBuild()
    {
        // Core libs (always built)
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Core" / "Agibuild.Avalonia.WebView.Core.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Abstractions" / "Agibuild.Avalonia.WebView.Adapters.Abstractions.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Runtime" / "Agibuild.Avalonia.WebView.Runtime.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.DependencyInjection" / "Agibuild.Avalonia.WebView.DependencyInjection.csproj";

        // Platform adapters (always built — stub adapters compile on all platforms)
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Windows" / "Agibuild.Avalonia.WebView.Adapters.Windows.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Gtk" / "Agibuild.Avalonia.WebView.Adapters.Gtk.csproj";

        // macOS adapter (native shim requires macOS host)
        if (OperatingSystem.IsMacOS())
        {
            yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.MacOS" / "Agibuild.Avalonia.WebView.Adapters.MacOS.csproj";
        }

        // Main packable project
        yield return SrcDirectory / "Agibuild.Avalonia.WebView" / "Agibuild.Avalonia.WebView.csproj";

        // Test projects
        yield return TestsDirectory / "Agibuild.Avalonia.WebView.Testing" / "Agibuild.Avalonia.WebView.Testing.csproj";
        yield return TestsDirectory / "Agibuild.Avalonia.WebView.UnitTests" / "Agibuild.Avalonia.WebView.UnitTests.csproj";
    }
}
