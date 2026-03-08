using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class BuildTask
{
    void RunContractAutomationTests(string trxFileName)
    {
        DotNetTest(s => s
            .SetProjectFile(UnitTestsProject)
            .SetConfiguration(Configuration)
            .EnableNoRestore()
            .EnableNoBuild()
            .SetResultsDirectory(TestResultsDirectory)
            .SetLoggers($"trx;LogFileName={trxFileName}"));
    }

    void RunRuntimeAutomationTests(string trxFileName)
    {
        DotNetTest(s => s
            .SetProjectFile(IntegrationTestsProject)
            .SetConfiguration(Configuration)
            .EnableNoRestore()
            .EnableNoBuild()
            .SetResultsDirectory(TestResultsDirectory)
            .SetLoggers($"trx;LogFileName={trxFileName}"));
    }

    void RunGtkSmokeDesktopApp()
    {
        TestResultsDirectory.CreateDirectory();

        // Use the desktop integration test app in self-terminating "--gtk-smoke" mode.
        // The app writes detailed logs to stdout/stderr which we persist as an artifact.
        var output = RunProcessCaptureAllChecked(
            "dotnet",
            $"run --project \"{E2EDesktopProject}\" --configuration {Configuration} --no-build -- --gtk-smoke",
            workingDirectory: RootDirectory,
            timeoutMs: 180_000);

        File.WriteAllText(TestResultsDirectory / "gtk-smoke.log", output);
    }

    static void RunLaneWithReporting(
        string lane,
        AbsolutePath project,
        Action run,
        IList<AutomationLaneResult> lanes,
        IList<string> failures)
    {
        try
        {
            run();
            lanes.Add(new AutomationLaneResult(lane, "passed", project.ToString()));
        }
        catch (Exception ex)
        {
            var message = ex.Message.Split('\n').FirstOrDefault() ?? ex.Message;
            lanes.Add(new AutomationLaneResult(lane, "failed", project.ToString(), message));
            failures.Add($"{lane}: {message}");
        }
    }

    static (int Total, int Passed, int Failed, int Skipped) ReadTrxCounters(AbsolutePath trxPath)
    {
        var doc = XDocument.Load(trxPath);
        var counters = doc.Root?
            .Element(XName.Get("ResultSummary", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"))?
            .Element(XName.Get("Counters", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"));

        if (counters is null)
            Assert.Fail($"Unable to parse counters from TRX file: {trxPath}");

        static int ParseIntOrZero(XAttribute? attr) =>
            attr is null || !int.TryParse(attr.Value, out var parsed) ? 0 : parsed;

        return (
            Total: ParseIntOrZero(counters!.Attribute("total")),
            Passed: ParseIntOrZero(counters.Attribute("passed")),
            Failed: ParseIntOrZero(counters.Attribute("failed")),
            Skipped: ParseIntOrZero(counters.Attribute("notExecuted")));
    }

    static HashSet<string> ReadPassedTestNamesFromTrx(AbsolutePath trxPath)
    {
        var doc = XDocument.Load(trxPath);
        var ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
        return doc
            .Descendants(ns + "UnitTestResult")
            .Where(result => string.Equals(
                result.Attribute("outcome")?.Value,
                "Passed",
                StringComparison.OrdinalIgnoreCase))
            .Select(result => result.Attribute("testName")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    static bool HasPassedTestMethod(HashSet<string> passedTests, string testMethod)
    {
        return passedTests.Any(name =>
            name.Equals(testMethod, StringComparison.Ordinal)
            || name.EndsWith("." + testMethod, StringComparison.Ordinal)
            || name.Contains(testMethod, StringComparison.Ordinal));
    }

    static double ReadCoberturaLineCoveragePercent(AbsolutePath coberturaPath)
    {
        var doc = XDocument.Load(coberturaPath);
        var lineRateAttr = doc.Root?.Attribute("line-rate")?.Value;

        if (lineRateAttr is null || !double.TryParse(
                lineRateAttr,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var lineRate))
        {
            Assert.Fail($"Unable to parse line-rate from coverage report: {coberturaPath}");
            return 0;
        }

        return lineRate * 100;
    }

    static double ReadCoberturaBranchCoveragePercent(AbsolutePath coberturaPath)
    {
        var doc = XDocument.Load(coberturaPath);
        var branchRateAttr = doc.Root?.Attribute("branch-rate")?.Value;

        if (branchRateAttr is null || !double.TryParse(
                branchRateAttr,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var branchRate))
        {
            Assert.Fail($"Unable to parse branch-rate from coverage report: {coberturaPath}");
            return 0;
        }

        return branchRate * 100;
    }
}
