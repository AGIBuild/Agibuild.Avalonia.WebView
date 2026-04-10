using System.CommandLine;
using System.Diagnostics;

namespace Agibuild.Fulora.Cli.Commands;

internal static class DevCommand
{
    internal delegate Task<int> ProcessRunner(string fileName, string arguments, string? workingDirectory, CancellationToken ct);
    internal delegate Task RunUntilCancelled(string fileName, string arguments, string? workingDirectory, string prefix, CancellationToken ct);

    public static Command Create()
    {
        var webDirOpt = new Option<string?>("--web")
        {
            Description = "Path to web project directory (auto-detected if omitted)"
        };
        var desktopDirOpt = new Option<string?>("--desktop")
        {
            Description = "Path to Desktop .csproj (auto-detected if omitted)"
        };
        var npmScriptOpt = new Option<string>("--npm-script")
        {
            Description = "npm script to start the dev server",
            DefaultValueFactory = _ => "dev"
        };
        var preflightOnlyOpt = new Option<bool>("--preflight-only")
        {
            Description = "Run bridge/dev preflight checks and exit without starting the dev processes"
        };

        var command = new Command("dev") { Description = "Start Vite dev server and Avalonia desktop app together" };
        command.Options.Add(webDirOpt);
        command.Options.Add(desktopDirOpt);
        command.Options.Add(npmScriptOpt);
        command.Options.Add(preflightOnlyOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            return await ExecuteAsync(
                explicitWebProject: parseResult.GetValue(webDirOpt),
                explicitDesktopProject: parseResult.GetValue(desktopDirOpt),
                npmScript: parseResult.GetValue(npmScriptOpt) ?? "dev",
                preflightOnly: parseResult.GetValue(preflightOnlyOpt),
                workingDirectory: Directory.GetCurrentDirectory(),
                output: Console.Out,
                error: Console.Error,
                runProcessAsync: NewCommand.RunProcessAsync,
                runUntilCancelledAsync: RunProcessUntilCancelledAsync,
                ct);
        });

        return command;
    }

    internal static async Task<int> PrepareBridgeArtifactsAsync(
        string? explicitBridgeProject,
        TextWriter output,
        TextWriter error,
        ProcessRunner runProcessAsync,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(runProcessAsync);

        var bridgeProject = explicitBridgeProject ?? GenerateCommand.DetectBridgeProject();
        if (bridgeProject is null)
        {
            output.WriteLine("No Bridge .csproj detected; skipping bridge artifact preflight.");
            return 0;
        }

        output.WriteLine($"Refreshing bridge artifacts from {Path.GetFileName(bridgeProject)}...");
        var bridgeProjectDirectory = Path.GetDirectoryName(bridgeProject);
        var exitCode = await runProcessAsync("dotnet", $"build \"{bridgeProject}\" -v q -m:1 -nodeReuse:false", bridgeProjectDirectory, ct);
        if (exitCode == 0)
        {
            output.WriteLine("Bridge artifacts ready.");
            EmitArtifactConsistencyWarnings(bridgeProject, output);
            return 0;
        }

        error.WriteLine("Bridge artifact generation failed during dev preflight.");
        error.WriteLine($"Fix the bridge build and rerun `fulora dev`, or inspect generation manually with `fulora generate types --project \"{bridgeProject}\"`.");
        return exitCode;
    }

    internal static async Task<int> ExecuteAsync(
        string? explicitWebProject,
        string? explicitDesktopProject,
        string npmScript,
        bool preflightOnly,
        string workingDirectory,
        TextWriter output,
        TextWriter error,
        ProcessRunner runProcessAsync,
        RunUntilCancelled runUntilCancelledAsync,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(runProcessAsync);
        ArgumentNullException.ThrowIfNull(runUntilCancelledAsync);

        var web = ResolveWebProject(explicitWebProject, workingDirectory);
        var desktop = ResolveDesktopProject(explicitDesktopProject, workingDirectory);

        if (web is null)
        {
            error.WriteLine("Could not find web project (package.json). Use --web to specify.");
            return 1;
        }

        if (desktop is null)
        {
            error.WriteLine("Could not find Desktop .csproj. Use --desktop to specify.");
            return 1;
        }

        var preflightExitCode = await PrepareBridgeArtifactsAsync(
            explicitBridgeProject: ResolveBridgeProject(workingDirectory),
            output: output,
            error: error,
            runProcessAsync: runProcessAsync,
            ct);
        if (preflightExitCode != 0)
            return preflightExitCode;

        if (preflightOnly)
        {
            output.WriteLine("Preflight complete.");
            return 0;
        }

        output.WriteLine($"Starting dev server in {Path.GetFileName(web)}...");
        output.WriteLine($"Starting Avalonia app from {Path.GetFileName(desktop)}...");
        output.WriteLine("Press Ctrl+C to stop both.");
        output.WriteLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ConsoleCancelEventHandler? handler = null;
        handler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += handler;

        try
        {
            var npmCmd = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
            var viteTask = runUntilCancelledAsync(npmCmd, $"run {npmScript}", web, "[vite]", cts.Token);
            var dotnetTask = runUntilCancelledAsync("dotnet", $"run --project \"{desktop}\"", null, "[dotnet]", cts.Token);

            try
            {
                await Task.WhenAll(viteTask, dotnetTask);
            }
            catch (OperationCanceledException)
            {
                // Expected on Ctrl+C
            }
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }

        output.WriteLine();
        output.WriteLine("Development servers stopped.");
        return 0;
    }

    internal static string? ResolveWebProject(string? explicitWebProject, string workingDirectory, IFileSystem? fileSystem = null)
        => explicitWebProject
            ?? FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveWebRoot()
            ?? DetectWebProject(workingDirectory, fileSystem);

    internal static string? ResolveDesktopProject(string? explicitDesktopProject, string workingDirectory, IFileSystem? fileSystem = null)
        => explicitDesktopProject
            ?? FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveDesktopProject()
            ?? DetectDesktopProject(workingDirectory, fileSystem);

    internal static string? ResolveBridgeProject(string workingDirectory, IFileSystem? fileSystem = null)
        => FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveBridgeProject()
            ?? GenerateCommand.DetectBridgeProject(workingDirectory, fileSystem);

    private static void EmitArtifactConsistencyWarnings(string bridgeProject, TextWriter output)
    {
        var warnings = GenerateCommand.CollectArtifactConsistencyWarnings(bridgeProject);
        if (warnings.Count == 0)
            return;

        foreach (var line in BridgeArtifactConsistency.FormatWarnings(warnings))
            output.WriteLine(line);
    }

    private static async Task RunProcessUntilCancelledAsync(
        string fileName, string arguments, string? workingDirectory, string prefix, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
        };

        using var process = Process.Start(psi);
        if (process is null) return;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await process.StandardOutput.ReadLineAsync(ct);
                if (line is null) break;
                Console.WriteLine($"{prefix} {line}");
            }
        }, ct);

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await process.StandardError.ReadLineAsync(ct);
                if (line is null) break;
                Console.Error.WriteLine($"{prefix} {line}");
            }
        }, ct);

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* Best effort */ }
        }
    }

    private static string? DetectWebProject()
        => DetectWebProject(Directory.GetCurrentDirectory());

    private static string? DetectWebProject(string cwd, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        var packageJsons = fileSystem.GetFiles(cwd, "package.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("node_modules"))
            .ToArray();

        foreach (var pj in packageJsons)
        {
            var dir = Path.GetDirectoryName(pj)!;
            var name = Path.GetFileName(dir);
            if (name.Contains("Web", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Vite", StringComparison.OrdinalIgnoreCase))
                return dir;
        }

        return packageJsons.Length == 1 ? Path.GetDirectoryName(packageJsons[0]) : null;
    }

    private static string? DetectDesktopProject()
        => DetectDesktopProject(Directory.GetCurrentDirectory());

    private static string? DetectDesktopProject(string cwd, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        var csprojs = fileSystem.GetFiles(cwd, "*.Desktop.csproj", SearchOption.AllDirectories);
        return csprojs.Length == 1 ? csprojs[0] : csprojs.FirstOrDefault();
    }
}
