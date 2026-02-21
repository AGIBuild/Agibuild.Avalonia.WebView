using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class WebView2TeardownStabilityIntegrationTests
{
    [Fact]
    public async Task WebView2_teardown_stress_does_not_emit_chromium_teardown_markers()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var repoRoot = FindRepoRoot();
        var desktopProject = Path.Combine(
            repoRoot,
            "tests",
            "Agibuild.Avalonia.WebView.Integration.Tests",
            "Agibuild.Avalonia.WebView.Integration.Tests.Desktop",
            "Agibuild.Avalonia.WebView.Integration.Tests.Desktop.csproj");

        Assert.True(File.Exists(desktopProject), $"Desktop integration test project not found: {desktopProject}");

        const int iterations = 10;
        var buildArgs = $"build \"{desktopProject}\" --configuration Debug";
        var runArgs =
            $"run --project \"{desktopProject}\" --configuration Debug --no-build --no-restore " +
            "-- " +
            $"--wv2-teardown-stress --wv2-teardown-iterations {iterations}";

        // Build and runtime are split on purpose: teardown correctness should not be coupled
        // to cold-start compile cost fluctuations in CI environments.
        var buildResult = await RunProcessCaptureAsync(
            fileName: "dotnet",
            arguments: buildArgs,
            workingDirectory: repoRoot,
            timeout: TimeSpan.FromMinutes(3),
            ct: TestContext.Current.CancellationToken);

        Assert.False(buildResult.TimedOut, FormatFailureMessage("build", buildResult, buildArgs));
        Assert.True(buildResult.ExitCode == 0, FormatFailureMessage("build", buildResult, buildArgs));

        var result = await RunProcessCaptureAsync(
            fileName: "dotnet",
            arguments: runArgs,
            workingDirectory: repoRoot,
            timeout: TimeSpan.FromMinutes(4),
            ct: TestContext.Current.CancellationToken);

        Assert.False(result.TimedOut, FormatFailureMessage("run", result, runArgs));

        var combinedOutput = $"{result.StdOut}\n{result.StdErr}".Trim();
        Assert.True(result.ExitCode == 0, FormatFailureMessage("run", result, runArgs));

        Assert.DoesNotContain("Failed to unregister class Chrome_WidgetWin_0", combinedOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(@"ui\gfx\win\window_impl.cc:124", combinedOutput, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 12 && dir is not null; i++)
        {
            var sln = Path.Combine(dir.FullName, "Agibuild.Avalonia.WebView.sln");
            if (File.Exists(sln))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root (Agibuild.Avalonia.WebView.sln) from current directory.");
    }

    private static async Task<ProcessResult> RunProcessCaptureAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var timedOut = false;

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            timedOut = true;
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new ProcessResult(process.ExitCode, stdout, stderr, timedOut, timeout);
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore kill race/errors; exit state is validated by caller assertions.
        }
    }

    private static string FormatFailureMessage(string phase, ProcessResult result, string args)
    {
        var combinedOutput = $"{result.StdOut}\n{result.StdErr}".Trim();
        return
            $"WV2 teardown stress {phase} failed.\n" +
            $"Command: dotnet {args}\n" +
            $"ExitCode: {result.ExitCode}\n" +
            $"TimedOut: {result.TimedOut}\n" +
            $"Timeout: {result.Timeout}\n\n" +
            $"{combinedOutput}";
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut, TimeSpan Timeout);
}

