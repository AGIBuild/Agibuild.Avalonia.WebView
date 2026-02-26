using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

    static AbsolutePath? ResolveFirstExistingPath(params AbsolutePath?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
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

    static void WaitForAndroidBoot(string adbPath, int timeoutMinutes = 3)
    {
        Serilog.Log.Information("Waiting for emulator to boot...");
        var timeout = TimeSpan.FromMinutes(timeoutMinutes);
        var stopwatch = Stopwatch.StartNew();
        var booted = false;

        while (stopwatch.Elapsed < timeout)
        {
            Thread.Sleep(3000);
            try
            {
                var bootResult = RunProcess(adbPath, "shell getprop sys.boot_completed");
                if (bootResult.Trim() == "1")
                {
                    booted = true;
                    break;
                }
            }
            catch
            {
                // Device not ready yet
            }
        }

        Assert.True(booted, $"Emulator did not boot within {timeoutMinutes} minutes.");
        Serilog.Log.Information("Emulator booted successfully ({Elapsed:F0}s).", stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Launch an Android app via monkey, retrying until the activity manager is available.
    /// After sys.boot_completed=1 there is a short window where system services are still initializing.
    /// </summary>
    static void LaunchAndroidApp(string adbPath, string packageName, int maxRetries = 5)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var output = RunProcess(adbPath,
                    $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");

                // monkey writes "Events injected: 1" to stdout on success
                if (output.Contains("Events injected", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Serilog.Log.Warning("monkey attempt {Attempt}/{Max}: unexpected output: {Output}",
                    attempt, maxRetries, output.Trim());
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("monkey attempt {Attempt}/{Max} failed: {Message}",
                    attempt, maxRetries, ex.Message);
            }

            if (attempt < maxRetries)
            {
                Serilog.Log.Information("Waiting 3s before retry...");
                Thread.Sleep(3000);
            }
        }

        throw new InvalidOperationException(
            $"Failed to launch {packageName} after {maxRetries} attempts. " +
            "The activity manager may not be available — is the emulator fully booted?");
    }

    static string RunProcess(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            Serilog.Log.Warning("Process stderr: {Error}", error.Trim());
        }

        return output;
    }

    static string RunProcessCaptureAll(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        return string.Join('\n', new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    static string RunNpmCaptureAll(string arguments, string workingDirectory, int timeoutMs = 30_000)
    {
        if (OperatingSystem.IsWindows())
        {
            return RunProcessCaptureAll(
                "cmd.exe",
                $"/d /s /c \"npm {arguments}\"",
                workingDirectory: workingDirectory,
                timeoutMs: timeoutMs);
        }

        return RunProcessCaptureAll(
            "npm",
            arguments,
            workingDirectory: workingDirectory,
            timeoutMs: timeoutMs);
    }

    static string RunProcessCaptureAllChecked(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        var combined = string.Join('\n', new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}.\n{combined}");
        }

        return combined;
    }

    IEnumerable<AbsolutePath> GetProjectsToBuild()
    {
        // Core libs (always built)
        yield return SrcDirectory / "Agibuild.Fulora.Core" / "Agibuild.Fulora.Core.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Abstractions" / "Agibuild.Fulora.Adapters.Abstractions.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Runtime" / "Agibuild.Fulora.Runtime.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.DependencyInjection" / "Agibuild.Fulora.DependencyInjection.csproj";

        // Platform adapters (always built — stub adapters compile on all platforms)
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Windows" / "Agibuild.Fulora.Adapters.Windows.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Gtk" / "Agibuild.Fulora.Adapters.Gtk.csproj";

        // macOS adapter (native shim requires macOS host)
        if (OperatingSystem.IsMacOS())
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.MacOS" / "Agibuild.Fulora.Adapters.MacOS.csproj";
        }

        // Android adapter (requires Android workload)
        if (HasDotNetWorkload("android"))
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.Android" / "Agibuild.Fulora.Adapters.Android.csproj";
        }
        else
        {
            Serilog.Log.Warning("Android workload not detected — skipping Android adapter build.");
        }

        // iOS adapter (requires macOS host + iOS workload)
        if (OperatingSystem.IsMacOS() && HasDotNetWorkload("ios"))
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.iOS" / "Agibuild.Fulora.Adapters.iOS.csproj";
        }
        else if (OperatingSystem.IsMacOS())
        {
            Serilog.Log.Warning("iOS workload not detected — skipping iOS adapter build.");
        }

        // Main packable project
        yield return SrcDirectory / "Agibuild.Fulora" / "Agibuild.Fulora.csproj";

        // Test projects
        yield return TestsDirectory / "Agibuild.Fulora.Testing" / "Agibuild.Fulora.Testing.csproj";
        yield return TestsDirectory / "Agibuild.Fulora.UnitTests" / "Agibuild.Fulora.UnitTests.csproj";
        yield return IntegrationTestsProject;
    }

    static bool HasDotNetWorkload(string platformKeyword)
    {
        try
        {
            var output = RunProcess("dotnet", "workload list", timeoutMs: 30_000);
            // Workload IDs can be 'android', 'maui-android', 'ios', 'maui-ios', etc.
            // Match any installed workload whose ID contains the platform keyword as a component.
            // Example: 'maui-android' matches keyword 'android'; 'ios' matches keyword 'ios'.
            return output.Split('\n')
                .Any(line =>
                {
                    var trimmed = line.TrimStart();
                    // Skip header/separator lines
                    if (trimmed.Length == 0 || trimmed.StartsWith('-') || trimmed.StartsWith("Installed") || trimmed.StartsWith("Workload") || trimmed.StartsWith("Use "))
                        return false;
                    // Extract the workload ID (first whitespace-delimited token)
                    var id = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    // Match exact ID or as a hyphen-delimited component
                    return id.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase)
                        || id.Split('-').Any(part => part.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase));
                });
        }
        catch
        {
            return false;
        }
    }
}
