using System.Text;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class TestGetAwaiterGetResultUsageTests
{
    [Fact]
    public void Test_sources_limit_GetAwaiterGetResult_to_approved_threading_boundaries()
    {
        var repoRoot = FindRepoRoot();
        var testsRoot = Path.Combine(repoRoot, "tests");
        var files = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories);

        var approvedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "tests/Agibuild.Avalonia.WebView.Testing/DispatcherTestPump.cs",
            "tests/Agibuild.Avalonia.WebView.Testing/ThreadingTestHelper.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1AnyThreadAsyncApiTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1BaseUrlTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1EventThreadingTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1NativeNavigationTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1OperationQueueTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1SourceAndStopTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/ContractSemanticsV1ThreadingTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/CoverageGapTests.cs",
            "tests/Agibuild.Avalonia.WebView.UnitTests/WebDialogTests.cs",
            "tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/WebAuthBrokerIntegrationTests.cs",
        };

        var found = new List<(string File, string Line)>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            if (relative.EndsWith("GetAwaiterGetResultUsageTests.cs", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {
                if (!line.Contains(".GetAwaiter().GetResult()", StringComparison.Ordinal))
                {
                    continue;
                }

                found.Add((relative, line.Trim()));
            }
        }

        Assert.NotEmpty(found);
        foreach (var (file, line) in found)
        {
            if (!approvedFiles.Contains(file))
            {
                Assert.Fail($"Unexpected test-side GetAwaiter().GetResult() in {file}: {line}");
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Agibuild.Avalonia.WebView.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
