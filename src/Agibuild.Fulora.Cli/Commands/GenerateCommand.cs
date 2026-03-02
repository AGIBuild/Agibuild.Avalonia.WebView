using System.CommandLine;

namespace Agibuild.Fulora.Cli.Commands;

internal static class GenerateCommand
{
    public static Command Create()
    {
        var group = new Command("generate") { Description = "Code generation commands" };
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
            Description = "Output directory for .d.ts files (default: auto-detected web project)"
        };

        var command = new Command("types") { Description = "Generate TypeScript declarations from C# bridge interfaces" };
        command.Options.Add(projectOpt);
        command.Options.Add(outputOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            var project = parseResult.GetValue(projectOpt);
            var output = parseResult.GetValue(outputOpt);

            var bridgeProject = project ?? DetectBridgeProject();
            if (bridgeProject is null)
            {
                Console.Error.WriteLine("Could not find a Bridge .csproj. Use --project to specify one.");
                return 1;
            }

            Console.WriteLine($"Building {Path.GetFileName(bridgeProject)} to generate TypeScript declarations...");

            var exitCode = await NewCommand.RunProcessAsync("dotnet", $"build \"{bridgeProject}\" -v q", ct: ct);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Build failed with exit code {exitCode}.");
                return exitCode;
            }

            var declFile = FindGeneratedDeclarations(bridgeProject);
            if (declFile is null)
            {
                Console.Error.WriteLine("No BridgeTypeScriptDeclarations found in build output.");
                Console.Error.WriteLine("Ensure the project references Agibuild.Fulora.Bridge.Generator.");
                return 1;
            }

            var outDir = output ?? DetectWebTypesDirectory(bridgeProject);
            if (outDir is null)
            {
                Console.Error.WriteLine("Could not detect web project types directory. Use --output to specify.");
                return 1;
            }

            Directory.CreateDirectory(outDir);
            var destPath = Path.Combine(outDir, "bridge.d.ts");
            File.Copy(declFile, destPath, overwrite: true);

            Console.WriteLine($"TypeScript declarations written to {destPath}");
            return 0;
        });

        return command;
    }

    private static string? DetectBridgeProject()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = Directory.GetFiles(cwd, "*.Bridge.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(cwd, "*Bridge*.csproj", SearchOption.AllDirectories))
            .Distinct()
            .ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            > 1 => candidates.FirstOrDefault(p => p.Contains("Bridge", StringComparison.OrdinalIgnoreCase)),
            _ => null,
        };
    }

    private static string? FindGeneratedDeclarations(string bridgeProject)
    {
        var projectDir = Path.GetDirectoryName(bridgeProject)!;
        var objDir = Path.Combine(projectDir, "obj");

        if (!Directory.Exists(objDir))
            return null;

        return Directory.GetFiles(objDir, "BridgeTypeScriptDeclarations.g.cs", SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static string? DetectWebTypesDirectory(string bridgeProject)
    {
        var solutionDir = Path.GetDirectoryName(bridgeProject);
        if (solutionDir is null) return null;

        var parent = Directory.GetParent(solutionDir)?.FullName;
        if (parent is null) return null;

        var webDirs = Directory.GetDirectories(parent)
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
        var srcBridge = Path.Combine(webDir, "src", "bridge");
        return Directory.Exists(srcBridge) ? srcBridge : Path.Combine(webDir, "src", "types");
    }
}
