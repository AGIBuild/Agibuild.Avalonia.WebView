using System.CommandLine;

namespace Agibuild.Fulora.Cli.Commands;

internal static class AttachCommand
{
    public static Command Create()
    {
        var command = new Command("attach")
        {
            Description = "Attach Fulora to an existing application workspace"
        };
        command.Subcommands.Add(CreateWebSubcommand());
        return command;
    }

    private static Command CreateWebSubcommand()
    {
        var webOpt = new Option<string>("--web")
        {
            Description = "Path to the existing web app root (must contain package.json)",
            Required = true,
        };
        var desktopOpt = new Option<string?>("--desktop")
        {
            Description = "Path to the Fulora desktop project directory (defaults to a sibling desktop directory)"
        };
        var bridgeOpt = new Option<string?>("--bridge")
        {
            Description = "Path to the Fulora bridge project directory (defaults to a sibling bridge directory)"
        };
        var frameworkOpt = new Option<string>("--framework")
        {
            Description = "Frontend framework mode: react, vue, or generic",
            Required = true,
        };
        frameworkOpt.AcceptOnlyFromAmong("react", "vue", "generic");

        var webCommandOpt = new Option<string?>("--web-command")
        {
            Description = "Command used to start the existing web app dev server"
        };
        var devServerUrlOpt = new Option<string?>("--dev-server-url")
        {
            Description = "Development server URL used by the desktop host (for example http://localhost:5173)"
        };

        var command = new Command("web")
        {
            Description = "Wire Fulora around an existing web app without rewriting the frontend"
        };
        command.Options.Add(webOpt);
        command.Options.Add(desktopOpt);
        command.Options.Add(bridgeOpt);
        command.Options.Add(frameworkOpt);
        command.Options.Add(webCommandOpt);
        command.Options.Add(devServerUrlOpt);

        command.SetAction((parseResult, _) =>
        {
            var options = new AttachWebOptions(
                WebRoot: parseResult.GetValue(webOpt)!,
                DesktopPath: parseResult.GetValue(desktopOpt),
                BridgePath: parseResult.GetValue(bridgeOpt),
                Framework: parseResult.GetValue(frameworkOpt)!,
                WebCommand: parseResult.GetValue(webCommandOpt),
                DevServerUrl: parseResult.GetValue(devServerUrlOpt));
            return ExecuteWebAsync(
                options,
                workspaceRoot: RealEnvironmentContext.Instance.CurrentDirectory,
                output: Console.Out,
                error: Console.Error,
                fileSystem: RealFileSystem.Instance);
        });

        return command;
    }

    internal static Task<int> ExecuteWebAsync(
        AttachWebOptions options,
        string workspaceRoot,
        TextWriter output,
        TextWriter error,
        IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (!IsWebProjectRoot(options.WebRoot, fileSystem))
        {
            error.WriteLine("Fulora could not find your web project root. Point --web to an existing web app directory containing package.json.");
            return Task.FromResult(1);
        }

        var result = AttachWebScaffolder.Scaffold(workspaceRoot, options, fileSystem);

        output.WriteLine("Fulora attached your existing web app.");
        output.WriteLine();
        output.WriteLine("Next steps:");
        if (!string.IsNullOrWhiteSpace(options.WebCommand))
            output.WriteLine($"  {options.WebCommand}");
        output.WriteLine("  fulora dev");
        output.WriteLine($"  fulora generate types --project \"{result.BridgeProjectPath}\"");
        return Task.FromResult(0);
    }

    internal static bool IsWebProjectRoot(string path, IFileSystem? fileSystem = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        fileSystem ??= RealFileSystem.Instance;
        var fullPath = Path.GetFullPath(path);
        return fileSystem.DirectoryExists(fullPath)
            && fileSystem.FileExists(Path.Combine(fullPath, "package.json"));
    }
}
