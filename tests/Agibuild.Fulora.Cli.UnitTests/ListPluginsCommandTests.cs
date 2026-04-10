using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public class ListPluginsCommandTests
{
    [Fact]
    public void GetFuloraPluginsFromCsproj_WithPluginReferences_FindsThem()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Agibuild.Fulora.Plugin.Database" Version="1.0.0" />
                    <PackageReference Include="Agibuild.Fulora.Core" Version="1.0.0" />
                    <PackageReference Include="Agibuild.Fulora.Plugin.HttpClient" Version="2.0.0" />
                  </ItemGroup>
                </Project>
                """);

            var plugins = ListPluginsCommand.GetFuloraPluginsFromCsproj(tmpFile);
            Assert.Equal(2, plugins.Count);
            Assert.Contains(plugins, p => p.PackageId == "Agibuild.Fulora.Plugin.Database" && p.Version == "1.0.0");
            Assert.Contains(plugins, p => p.PackageId == "Agibuild.Fulora.Plugin.HttpClient" && p.Version == "2.0.0");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void GetFuloraPluginsFromCsproj_NoPlugins_ReturnsEmpty()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Agibuild.Fulora.Core" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """);

            var plugins = ListPluginsCommand.GetFuloraPluginsFromCsproj(tmpFile);
            Assert.Empty(plugins);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void GetFuloraPluginsFromCsproj_NonExistentFile_ReturnsEmpty()
    {
        var plugins = ListPluginsCommand.GetFuloraPluginsFromCsproj("/tmp/nonexistent.csproj");
        Assert.Empty(plugins);
    }

    [Fact]
    public void GetInstalledFuloraVersion_FindsVersion()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "test.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Agibuild.Fulora.Core" Version="1.1.0" />
                  </ItemGroup>
                </Project>
                """);

            var version = ListPluginsCommand.GetInstalledFuloraVersion(tmpDir);
            Assert.NotNull(version);
            Assert.Equal(new Version(1, 1, 0), version);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void GetInstalledFuloraVersion_NoFuloraRef_ReturnsNull()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "test.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
                  </ItemGroup>
                </Project>
                """);

            var version = ListPluginsCommand.GetInstalledFuloraVersion(tmpDir);
            Assert.Null(version);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }
}
