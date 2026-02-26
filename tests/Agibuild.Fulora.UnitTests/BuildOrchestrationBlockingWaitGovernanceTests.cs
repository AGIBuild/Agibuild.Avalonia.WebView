using System.Text;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BuildOrchestrationBlockingWaitGovernanceTests
{
    private sealed record AllowedBlockingWait(string Fragment, string Owner, string Rationale);

    [Fact]
    public void Build_orchestration_blocking_waits_are_whitelisted_with_owner_and_rationale()
    {
        var repoRoot = FindRepoRoot();
        var buildDir = Path.Combine(repoRoot, "build");
        var lines = Directory.GetFiles(buildDir, "Build*.cs")
            .SelectMany(f => File.ReadAllLines(f, Encoding.UTF8))
            .ToArray();

        var allowed = new[]
        {
            new AllowedBlockingWait(
                "launchProcess.WaitForExit(15_000)",
                "Build.StartIOS",
                "Simulator launch may hang and requires bounded synchronous wait before kill/recover."),
            new AllowedBlockingWait(
                "process.WaitForExit()",
                "Build.StartReactDev",
                "Foreground dev-server target is intentionally blocking until user stops the process."),
            new AllowedBlockingWait(
                "!process.WaitForExit(timeoutMs)",
                "Build.RunProcess",
                "All shell commands use a bounded timeout and fail fast on hangs."),
            new AllowedBlockingWait(
                "Thread.Sleep(3000);",
                "Build.AndroidBootAndLaunch",
                "ADB/device boot and activity-manager readiness polling uses bounded sleep interval."),
            new AllowedBlockingWait(
                "Thread.Sleep(delayMs);",
                "Build.RunNugetSmokeWithRetry",
                "Transient package smoke retry uses bounded backoff before next attempt."),
            new AllowedBlockingWait(
                "http.GetAsync(url).GetAwaiter().GetResult()",
                "Build.IsHttpReady",
                "Synchronous readiness probe for port polling remains constrained to build helper boundary."),
            new AllowedBlockingWait(
                "Thread.Sleep(500);",
                "Build.WaitForPort",
                "Port readiness loop uses bounded short polling interval.")
        };

        var found = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("GetAwaiter().GetResult()", StringComparison.Ordinal)
                || trimmed.Contains("Thread.Sleep(", StringComparison.Ordinal)
                || trimmed.Contains("WaitForExit(", StringComparison.Ordinal))
            {
                found.Add(trimmed);
            }
        }

        Assert.NotEmpty(found);
        foreach (var usage in found)
        {
            Assert.Contains(
                allowed,
                x => usage.Contains(x.Fragment, StringComparison.Ordinal));
        }

        foreach (var entry in allowed)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Owner));
            Assert.False(string.IsNullOrWhiteSpace(entry.Rationale));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Fulora.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
