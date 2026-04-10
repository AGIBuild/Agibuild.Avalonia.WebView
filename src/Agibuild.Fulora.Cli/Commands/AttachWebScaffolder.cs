namespace Agibuild.Fulora.Cli.Commands;

internal static class AttachWebScaffolder
{
    internal static AttachWebResult Scaffold(string workspaceRoot, AttachWebOptions options, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(options);
        fileSystem ??= RealFileSystem.Instance;

        var webRoot = Path.GetFullPath(options.WebRoot);
        var desktopProjectPath = ResolveProjectPath(options.DesktopPath ?? GetDefaultProjectPath(webRoot, "Desktop"));
        var bridgeProjectPath = ResolveProjectPath(options.BridgePath ?? GetDefaultProjectPath(webRoot, "Bridge"));
        var desktopDirectory = Path.GetDirectoryName(desktopProjectPath)
            ?? throw new InvalidOperationException("Could not determine desktop project directory.");
        var bridgeDirectory = Path.GetDirectoryName(bridgeProjectPath)
            ?? throw new InvalidOperationException("Could not determine bridge project directory.");
        var generatedDirectory = Path.Combine(webRoot, "src", "bridge", "generated");
        var bridgeEntryDirectory = Path.Combine(webRoot, "src", "bridge");

        fileSystem.CreateDirectory(bridgeEntryDirectory);
        fileSystem.CreateDirectory(generatedDirectory);
        fileSystem.CreateDirectory(desktopDirectory);
        fileSystem.CreateDirectory(bridgeDirectory);

        WriteFileIfMissing(Path.Combine(bridgeEntryDirectory, "client.ts"), CreateClientTs(), fileSystem);
        WriteFileIfMissing(Path.Combine(bridgeEntryDirectory, "services.ts"), CreateServicesTs(), fileSystem);
        WriteFileIfMissing(bridgeProjectPath, CreateBridgeProjectFile(bridgeDirectory, generatedDirectory), fileSystem);
        WriteFileIfMissing(Path.Combine(bridgeDirectory, "IAppHostService.cs"), CreateBridgeContractFile(), fileSystem);
        WriteFileIfMissing(desktopProjectPath, CreateDesktopProjectFile(desktopDirectory, bridgeProjectPath), fileSystem);
        WriteFileIfMissing(Path.Combine(desktopDirectory, "Program.cs"), CreateDesktopProgramFile(), fileSystem);

        var config = new FuloraWorkspaceConfig
        {
            Web = new FuloraWorkspaceConfig.WebSection
            {
                Root = FuloraWorkspaceConfigResolver.MakeRelativePath(workspaceRoot, webRoot),
                Command = options.WebCommand,
                DevServerUrl = options.DevServerUrl,
                GeneratedDir = FuloraWorkspaceConfigResolver.MakeRelativePath(workspaceRoot, generatedDirectory),
            },
            Bridge = new FuloraWorkspaceConfig.BridgeSection
            {
                Project = FuloraWorkspaceConfigResolver.MakeRelativePath(workspaceRoot, bridgeProjectPath),
            },
            Desktop = new FuloraWorkspaceConfig.DesktopSection
            {
                Project = FuloraWorkspaceConfigResolver.MakeRelativePath(workspaceRoot, desktopProjectPath),
            }
        };

        FuloraWorkspaceConfigResolver.Save(workspaceRoot, config, fileSystem);

        return new AttachWebResult(webRoot, desktopProjectPath, bridgeProjectPath, generatedDirectory, config);
    }

    private static string ResolveProjectPath(string pathOrDirectory)
    {
        var fullPath = Path.GetFullPath(pathOrDirectory);
        if (fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return fullPath;

        var projectName = Path.GetFileName(fullPath);
        return Path.Combine(fullPath, $"{projectName}.csproj");
    }

    private static string GetDefaultProjectPath(string webRoot, string suffix)
    {
        var webDirectory = new DirectoryInfo(webRoot);
        var parent = webDirectory.Parent?.FullName ?? webRoot;
        var baseName = webDirectory.Name;
        if (baseName.EndsWith("-web", StringComparison.OrdinalIgnoreCase))
            baseName = baseName[..^4];

        var normalized = string.Concat(
            baseName
                .Split(new[] { '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "FuloraApp";

        return Path.Combine(parent, $"{normalized}.{suffix}");
    }

    private static void WriteFileIfMissing(string path, string content, IFileSystem fileSystem)
    {
        if (fileSystem.FileExists(path))
            return;

        fileSystem.WriteAllText(path, content);
    }

    private static string CreateClientTs()
        => """
export { services } from "./services";
""";

    private static string CreateServicesTs()
        => """
/**
 * Fulora-owned app-service façade.
 * Run `fulora generate types` after adding or updating bridge contracts.
 */
export const services = {} as const;
""";

    private static string CreateBridgeProjectFile(string bridgeDirectory, string generatedDirectory)
    {
        var relativeGeneratedDirectory = Path.GetRelativePath(bridgeDirectory, generatedDirectory);

        return $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <BridgeTypeScriptOutputDir>{{relativeGeneratedDirectory}}</BridgeTypeScriptOutputDir>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Agibuild.Fulora.Core" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Agibuild.Fulora.Bridge.Generator"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false"
                      PrivateAssets="all" />
  </ItemGroup>

</Project>
""";
    }

    private static string CreateBridgeContractFile()
        => """
using Agibuild.Fulora;
using System.Threading.Tasks;

namespace FuloraAttachedApp.Bridge;

[JsExport]
public interface IAppHostService
{
    Task<string> Ping();
}
""";

    private static string CreateDesktopProjectFile(string desktopDirectory, string bridgeProjectPath)
    {
        var relativeBridgeProject = Path.GetRelativePath(desktopDirectory, bridgeProjectPath);

        return $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Agibuild.Fulora.Avalonia" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="{{relativeBridgeProject}}" />
  </ItemGroup>

</Project>
""";
    }

    private static string CreateDesktopProgramFile()
        => """
using System;

Console.WriteLine("Fulora desktop host scaffolding created. Replace Program.cs with your actual desktop bootstrap.");
""";
}

internal sealed record AttachWebOptions(
    string WebRoot,
    string? DesktopPath,
    string? BridgePath,
    string Framework,
    string? WebCommand,
    string? DevServerUrl);

internal sealed record AttachWebResult(
    string WebRoot,
    string DesktopProjectPath,
    string BridgeProjectPath,
    string GeneratedDirectory,
    FuloraWorkspaceConfig Config);
