using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        var args =
            $"run --project \"{desktopProject}\" --configuration Debug " +
            "-- " +
            "--wv2-teardown-stress --wv2-teardown-iterations 10";

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var result = await RunProcessCaptureAsync(
            fileName: "dotnet",
            arguments: args,
            workingDirectory: repoRoot,
            ct: cts.Token);

        var combinedOutput = $"{result.StdOut}\n{result.StdErr}".Trim();

        Assert.True(result.ExitCode == 0, $"Desktop WV2 teardown stress exited with code {result.ExitCode}.\n\n{combinedOutput}");

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

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) stderr.AppendLine(e.Data);
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}

