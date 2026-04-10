using System.CommandLine;
using System.Reflection;

namespace Agibuild.Fulora.Cli.Commands;

internal static class GenerateCommand
{
    internal delegate Task<int> ProcessRunner(string fileName, string arguments, string? workingDirectory, CancellationToken ct);

    internal static readonly string[] ExpectedArtifactFileNames = BridgeArtifactConsistency.ExpectedArtifactFileNames;
    internal const string ManifestFileName = BridgeArtifactConsistency.ManifestFileName;

    public static Command Create()
    {
        var group = new Command("generate") { Description = "Bridge and code generation commands" };
        group.Aliases.Add("gen");
        group.Subcommands.Add(CreateTypesSubcommand());
        return group;
    }

    private static Command CreateTypesSubcommand()
    {
        var projectOpt = new Option<string?>("--project", "-p")
        {
            Description = "Path to the Bridge .csproj (auto-detected if omitted)"
        };
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Output directory for generated bridge artifacts (default: auto-detected web project)"
        };

        var command = new Command("types") { Description = "Generate bridge TypeScript artifacts from C# bridge interfaces" };
        command.Options.Add(projectOpt);
        command.Options.Add(outputOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            return await ExecuteTypesAsync(
                explicitProject: parseResult.GetValue(projectOpt),
                explicitOutput: parseResult.GetValue(outputOpt),
                workingDirectory: Directory.GetCurrentDirectory(),
                output: Console.Out,
                error: Console.Error,
                runProcessAsync: NewCommand.RunProcessAsync,
                findBuiltAssembly: FindBuiltAssembly,
                fileSystem: RealFileSystem.Instance,
                ct);
        });

        return command;
    }

    internal static string? DetectBridgeProject()
        => DetectBridgeProject(Directory.GetCurrentDirectory());

    internal static string? ResolveBridgeProject(string? explicitProject, string workingDirectory, IFileSystem? fileSystem = null)
        => explicitProject
            ?? FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveBridgeProject()
            ?? DetectBridgeProject(workingDirectory, fileSystem);

    internal static string? ResolveOutputDirectory(string? explicitOutput, string bridgeProject, string workingDirectory, IFileSystem? fileSystem = null)
        => explicitOutput
            ?? FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveGeneratedDir()
            ?? DetectWebTypesDirectory(bridgeProject, fileSystem);

    internal static string? DetectBridgeProject(string cwd, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;

        var candidates = fileSystem.GetFiles(cwd, "*.Bridge.csproj", SearchOption.AllDirectories)
            .Concat(fileSystem.GetFiles(cwd, "*Bridge*.csproj", SearchOption.AllDirectories))
            .Distinct()
            .ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            > 1 => candidates.FirstOrDefault(p => p.Contains("Bridge", StringComparison.OrdinalIgnoreCase)),
            _ => null,
        };
    }

    internal static IReadOnlyDictionary<string, string> ReadGeneratedArtifactsFromAssembly(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        var declarationsType = assembly.GetTypes().FirstOrDefault(t => t.Name == "BridgeTypeScriptDeclarations");
        if (declarationsType is null)
            throw new InvalidOperationException("No BridgeTypeScriptDeclarations found in the built assembly.");

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ExpectedArtifactFileNames[0]] = ReadArtifactField(declarationsType, "All"),
            [ExpectedArtifactFileNames[1]] = ReadArtifactField(declarationsType, "Client"),
            [ExpectedArtifactFileNames[2]] = ReadArtifactField(declarationsType, "Mock"),
        };
    }

    internal static BridgeArtifactManifest CreateArtifactManifest(
        string bridgeProjectFileName,
        string artifactDirectory,
        string assemblyPath,
        IReadOnlyDictionary<string, string> artifacts)
        => BridgeArtifactConsistency.CreateArtifactManifest(bridgeProjectFileName, artifactDirectory, assemblyPath, artifacts);

    internal static void WriteArtifactManifest(string outputDirectory, BridgeArtifactManifest manifest)
        => BridgeArtifactConsistency.WriteArtifactManifest(outputDirectory, manifest);

    internal static BridgeArtifactManifest? ReadArtifactManifest(string outputDirectory)
        => BridgeArtifactConsistency.ReadArtifactManifest(outputDirectory);

    internal static string? FindBuiltAssembly(string bridgeProject)
        => BridgeArtifactConsistency.FindBuiltAssembly(bridgeProject);

    internal static string? DetectWebArtifactsDirectory(string bridgeProject)
        => DetectWebTypesDirectory(bridgeProject, RealFileSystem.Instance);

    internal static IReadOnlyList<string> CollectArtifactConsistencyWarnings(string bridgeProject)
        => BridgeArtifactConsistency.CollectArtifactConsistencyWarnings(bridgeProject, path => DetectWebTypesDirectory(path, RealFileSystem.Instance));

    internal static async Task<int> ExecuteTypesAsync(
        string? explicitProject,
        string? explicitOutput,
        string workingDirectory,
        TextWriter output,
        TextWriter error,
        ProcessRunner runProcessAsync,
        Func<string, string?> findBuiltAssembly,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(runProcessAsync);
        ArgumentNullException.ThrowIfNull(findBuiltAssembly);
        ArgumentNullException.ThrowIfNull(fileSystem);

        var bridgeProject = ResolveBridgeProject(explicitProject, workingDirectory, fileSystem);
        if (bridgeProject is null)
        {
            error.WriteLine("Could not find a Bridge .csproj. Use --project to specify one.");
            return 1;
        }

        output.WriteLine($"Building {Path.GetFileName(bridgeProject)} to generate bridge TypeScript artifacts...");

        var exitCode = await runProcessAsync("dotnet", $"build \"{bridgeProject}\" -v q -m:1 -nodeReuse:false", null, ct);
        if (exitCode != 0)
        {
            error.WriteLine($"Build failed with exit code {exitCode}.");
            return exitCode;
        }

        var assemblyPath = findBuiltAssembly(bridgeProject);
        if (assemblyPath is null)
        {
            error.WriteLine("Could not find the built Bridge assembly after compilation.");
            error.WriteLine("Ensure the project builds successfully and targets a concrete framework output.");
            return 1;
        }

        var outDir = ResolveOutputDirectory(explicitOutput, bridgeProject, workingDirectory, fileSystem);
        if (outDir is null)
        {
            error.WriteLine("Could not detect web project types directory. Use --output to specify.");
            return 1;
        }

        fileSystem.CreateDirectory(outDir);
        IReadOnlyDictionary<string, string> artifacts;
        try
        {
            artifacts = ReadGeneratedArtifactsFromAssembly(assemblyPath);
        }
        catch (InvalidOperationException ex)
        {
            error.WriteLine(ex.Message);
            error.WriteLine("Ensure the project references Agibuild.Fulora.Bridge.Generator and exposes BridgeTypeScriptDeclarations.");
            return 1;
        }

        foreach (var artifact in artifacts)
        {
            var destPath = Path.Combine(outDir, artifact.Key);
            fileSystem.WriteAllText(destPath, artifact.Value);
            output.WriteLine($"TypeScript artifact written to {destPath}");
        }

        var manifest = BridgeArtifactConsistency.CreateArtifactManifest(Path.GetFileName(bridgeProject), outDir, assemblyPath, artifacts, fileSystem);
        BridgeArtifactConsistency.WriteArtifactManifest(outDir, manifest, fileSystem);
        output.WriteLine($"Bridge artifact manifest written to {Path.Combine(outDir, ManifestFileName)}");
        return 0;
    }

    private static string? DetectWebTypesDirectory(string bridgeProject, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        var solutionDir = Path.GetDirectoryName(bridgeProject);
        if (solutionDir is null) return null;

        var parent = Path.GetDirectoryName(solutionDir);
        if (parent is null) return null;

        var webDirs = fileSystem.GetDirectories(parent)
            .Where(d =>
            {
                var name = Path.GetFileName(d);
                return name.Contains("Web", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Vite", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        if (webDirs.Length == 0)
            return null;

        var webDir = webDirs[0];
        var generatedBridgeDir = Path.Combine(webDir, "src", "bridge", "generated");
        if (fileSystem.DirectoryExists(generatedBridgeDir))
            return generatedBridgeDir;

        var srcBridge = Path.Combine(webDir, "src", "bridge");
        return fileSystem.DirectoryExists(srcBridge) ? srcBridge : Path.Combine(webDir, "src", "types");
    }

    private static string ReadArtifactField(Type declarationsType, string fieldName)
    {
        var field = declarationsType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if (field?.GetValue(null) is string content && !string.IsNullOrWhiteSpace(content))
            return content;

        throw new InvalidOperationException($"BridgeTypeScriptDeclarations.{fieldName} was not found or was empty.");
    }

    internal sealed record BridgeArtifactManifest(
        int SchemaVersion,
        string GeneratedAtUtc,
        string BridgeProjectFileName,
        string ArtifactDirectory,
        string BuildConfiguration,
        string TargetFramework,
        string AssemblyFileName,
        string AssemblySha256,
        IReadOnlyDictionary<string, string> Artifacts);
}
