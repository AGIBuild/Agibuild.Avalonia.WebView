using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class AttachWebScaffolderTests
{
    [Fact]
    public void Scaffold_writes_fulora_owned_files_via_fake_file_system()
    {
        var workspaceRoot = Path.GetFullPath("/virtual/attach-workspace");
        var webRoot = Path.Combine(workspaceRoot, "apps", "product-web");
        var desktopRoot = Path.Combine(workspaceRoot, "apps", "Product.Desktop");
        var bridgeRoot = Path.Combine(workspaceRoot, "apps", "Product.Bridge");
        var fileSystem = new FakeFileSystem();

        fileSystem.AddDirectory(webRoot);
        fileSystem.AddFile(Path.Combine(webRoot, "package.json"), """{"name":"product-web","private":true}""");

        var result = AttachWebScaffolder.Scaffold(
            workspaceRoot,
            new AttachWebOptions(
                WebRoot: webRoot,
                DesktopPath: desktopRoot,
                BridgePath: bridgeRoot,
                Framework: "react",
                WebCommand: "npm run dev",
                DevServerUrl: "http://localhost:5173"),
            fileSystem);

        Assert.True(fileSystem.FileExists(Path.Combine(webRoot, "src", "bridge", "client.ts")));
        Assert.True(fileSystem.FileExists(Path.Combine(webRoot, "src", "bridge", "services.ts")));
        Assert.True(fileSystem.DirectoryExists(Path.Combine(webRoot, "src", "bridge", "generated")));
        Assert.True(fileSystem.FileExists(Path.Combine(bridgeRoot, "Product.Bridge.csproj")));
        Assert.True(fileSystem.FileExists(Path.Combine(desktopRoot, "Product.Desktop.csproj")));
        Assert.True(fileSystem.FileExists(Path.Combine(workspaceRoot, FuloraWorkspaceConfigResolver.FileName)));

        Assert.Equal("./apps/product-web", result.Config.Web!.Root);
        Assert.Equal("./apps/Product.Bridge/Product.Bridge.csproj", result.Config.Bridge!.Project);
        Assert.Equal("./apps/Product.Desktop/Product.Desktop.csproj", result.Config.Desktop!.Project);
    }
}
