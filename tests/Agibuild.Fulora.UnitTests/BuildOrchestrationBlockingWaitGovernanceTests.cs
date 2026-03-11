using System.Text;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BuildOrchestrationBlockingWaitGovernanceTests
{
    private sealed record AllowedBlockingWait(string Fragment, string Owner, string Rationale);

    [Fact]
    public void Build_orchestration_has_no_blocking_waits()
    {
        var repoRoot = FindRepoRoot();
        var buildDir = Path.Combine(repoRoot, "build");
        var lines = Directory.GetFiles(buildDir, "Build*.cs")
            .SelectMany(f => File.ReadAllLines(f, Encoding.UTF8))
            .ToArray();

        var violations = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("*", StringComparison.Ordinal))
                continue;

            if (trimmed.Contains("GetAwaiter().GetResult()", StringComparison.Ordinal)
                || trimmed.Contains("Thread.Sleep(", StringComparison.Ordinal)
                || trimmed.Contains(".WaitForExit(", StringComparison.Ordinal))
            {
                violations.Add(trimmed);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Build orchestration must be fully async. Found {violations.Count} blocking wait(s):\n"
            + string.Join('\n', violations));
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
