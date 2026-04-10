using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class BridgeArtifactConsistencyTests
{
    [Fact]
    public void CollectArtifactConsistencyWarnings_reports_stale_generated_artifacts_with_fake_file_system()
    {
        var root = Path.GetFullPath("/virtual/bridge-workspace");
        var bridgeProject = Path.Combine(root, "apps", "Product.Bridge", "Product.Bridge.csproj");
        var artifactDirectory = Path.Combine(root, "apps", "Product.Web", "src", "bridge", "generated");
        var assemblyPath = Path.Combine(root, "apps", "Product.Bridge", "bin", "Debug", "net10.0", "Product.Bridge.dll");
        var fileSystem = new FakeFileSystem();

        fileSystem.AddFile(bridgeProject, "<Project />");
        fileSystem.AddFile(assemblyPath, "bridge-assembly-v1");
        fileSystem.AddFile(Path.Combine(artifactDirectory, "bridge.d.ts"), "// dts v1");
        fileSystem.AddFile(Path.Combine(artifactDirectory, "bridge.client.ts"), "// client v1");
        fileSystem.AddFile(Path.Combine(artifactDirectory, "bridge.mock.ts"), "// mock v1");

        var manifest = BridgeArtifactConsistency.CreateArtifactManifest(
            "Product.Bridge.csproj",
            artifactDirectory,
            assemblyPath,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bridge.d.ts"] = "// dts v1",
                ["bridge.client.ts"] = "// client v1",
                ["bridge.mock.ts"] = "// mock v1",
            },
            fileSystem);
        BridgeArtifactConsistency.WriteArtifactManifest(artifactDirectory, manifest, fileSystem);

        fileSystem.WriteAllText(Path.Combine(artifactDirectory, "bridge.client.ts"), "// client mutated");

        var warnings = BridgeArtifactConsistency.CollectArtifactConsistencyWarnings(
            bridgeProject,
            _ => artifactDirectory,
            fileSystem);

        Assert.Contains(warnings, warning => warning.Contains("stale generated bridge artifacts", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(warnings, warning => warning.Contains("bridge.client.ts hash does not match manifest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindBuiltAssembly_uses_fake_file_system()
    {
        var root = Path.GetFullPath("/virtual/bridge-workspace");
        var bridgeProject = Path.Combine(root, "src", "Demo.Bridge", "Demo.Bridge.csproj");
        var assemblyPath = Path.Combine(root, "src", "Demo.Bridge", "bin", "Debug", "net10.0", "Demo.Bridge.dll");
        var fileSystem = new FakeFileSystem();

        fileSystem.AddFile(bridgeProject, "<Project />");
        fileSystem.AddFile(assemblyPath, "assembly");

        var resolved = BridgeArtifactConsistency.FindBuiltAssembly(bridgeProject, fileSystem);

        Assert.Equal(assemblyPath, resolved);
    }
}
