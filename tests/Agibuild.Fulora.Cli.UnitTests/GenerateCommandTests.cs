using System.Reflection;
using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class GenerateCommandTests
{
    [Fact]
    public async Task ExecuteTypesAsync_writes_artifacts_and_manifest_via_fake_file_system()
    {
        var workspaceRoot = Path.GetFullPath("/virtual/generate-workspace");
        var bridgeProject = Path.Combine(workspaceRoot, "apps", "Product.Bridge", "Product.Bridge.csproj");
        var outputDirectory = Path.Combine(workspaceRoot, "apps", "Product.Web", "src", "bridge", "generated");
        var fileSystem = new FakeFileSystem();
        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        fileSystem.AddFile(bridgeProject, "<Project />");
        fileSystem.AddBinaryFile(assemblyPath, File.ReadAllBytes(assemblyPath));

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await GenerateCommand.ExecuteTypesAsync(
            explicitProject: bridgeProject,
            explicitOutput: outputDirectory,
            workingDirectory: workspaceRoot,
            output: stdout,
            error: stderr,
            runProcessAsync: (_, _, _, _) => Task.FromResult(0),
            findBuiltAssembly: _ => assemblyPath,
            fileSystem: fileSystem,
            ct: CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(fileSystem.FileExists(Path.Combine(outputDirectory, "bridge.d.ts")));
        Assert.True(fileSystem.FileExists(Path.Combine(outputDirectory, "bridge.client.ts")));
        Assert.True(fileSystem.FileExists(Path.Combine(outputDirectory, "bridge.mock.ts")));
        Assert.True(fileSystem.FileExists(Path.Combine(outputDirectory, "bridge.manifest.json")));
        Assert.Contains("bridge.manifest.json", stdout.ToString(), StringComparison.Ordinal);
    }
}
