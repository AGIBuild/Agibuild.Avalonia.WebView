using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class FuloraWorkspaceConfigTests
{
    [Fact]
    public void Load_returns_null_when_fulora_json_is_missing()
    {
        var root = Path.GetFullPath("/virtual/workspace");
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(root);

        Assert.Null(FuloraWorkspaceConfigResolver.Load(root, fileSystem));
    }

    [Fact]
    public void Save_and_load_round_trip_relative_paths()
    {
        var root = Path.GetFullPath("/virtual/workspace");
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(root);
        var config = new FuloraWorkspaceConfig
        {
            Web = new FuloraWorkspaceConfig.WebSection
            {
                Root = "./app/web",
                Command = "pnpm dev",
                DevServerUrl = "http://localhost:5173",
                GeneratedDir = "./app/web/src/bridge/generated"
            },
            Bridge = new FuloraWorkspaceConfig.BridgeSection
            {
                Project = "./app/bridge/Sample.Bridge.csproj"
            },
            Desktop = new FuloraWorkspaceConfig.DesktopSection
            {
                Project = "./app/desktop/Sample.Desktop.csproj"
            }
        };

        FuloraWorkspaceConfigResolver.Save(root, config, fileSystem);

        var json = fileSystem.ReadAllText(Path.Combine(root, FuloraWorkspaceConfigResolver.FileName));
        Assert.Contains("\"root\": \"./app/web\"", json, StringComparison.Ordinal);
        Assert.Contains("\"generatedDir\": \"./app/web/src/bridge/generated\"", json, StringComparison.Ordinal);
        Assert.Contains("\"project\": \"./app/bridge/Sample.Bridge.csproj\"", json, StringComparison.Ordinal);

        var loaded = FuloraWorkspaceConfigResolver.Load(root, fileSystem);
        Assert.NotNull(loaded);
        Assert.Equal("./app/web", loaded!.Config.Web!.Root);
        Assert.Equal("./app/web/src/bridge/generated", loaded.Config.Web.GeneratedDir);
        Assert.Equal("./app/bridge/Sample.Bridge.csproj", loaded.Config.Bridge!.Project);
        Assert.Equal("./app/desktop/Sample.Desktop.csproj", loaded.Config.Desktop!.Project);
    }

    [Fact]
    public void Resolve_from_nested_working_directory_finds_repo_root_config()
    {
        var root = Path.GetFullPath("/virtual/workspace");
        var nested = Path.Combine(root, "apps", "product-web", "src", "components");
        var fileSystem = new FakeFileSystem();

        FuloraWorkspaceConfigResolver.Save(
            root,
            new FuloraWorkspaceConfig
            {
                Web = new FuloraWorkspaceConfig.WebSection
                {
                    Root = "./apps/product-web",
                    GeneratedDir = "./apps/product-web/src/bridge/generated"
                },
                Bridge = new FuloraWorkspaceConfig.BridgeSection
                {
                    Project = "./apps/product-bridge/Product.Bridge.csproj"
                },
                Desktop = new FuloraWorkspaceConfig.DesktopSection
                {
                    Project = "./apps/product-desktop/Product.Desktop.csproj"
                }
            },
            fileSystem);
        fileSystem.AddDirectory(nested);

        var loaded = FuloraWorkspaceConfigResolver.Load(nested, fileSystem);

        Assert.NotNull(loaded);
        Assert.Equal(root, loaded!.WorkspaceRoot);
        Assert.Equal(Path.Combine(root, "apps", "product-web"), loaded.ResolveWebRoot());
        Assert.Equal(Path.Combine(root, "apps", "product-web", "src", "bridge", "generated"), loaded.ResolveGeneratedDir());
        Assert.Equal(Path.Combine(root, "apps", "product-bridge", "Product.Bridge.csproj"), loaded.ResolveBridgeProject());
        Assert.Equal(Path.Combine(root, "apps", "product-desktop", "Product.Desktop.csproj"), loaded.ResolveDesktopProject());
    }
}
