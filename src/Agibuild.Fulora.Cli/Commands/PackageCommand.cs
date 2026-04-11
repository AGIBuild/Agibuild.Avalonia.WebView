using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;

namespace Agibuild.Fulora.Cli.Commands;

internal static class PackageCommand
{
    public static Command Create()
    {
        var profileOpt = new Option<string?>("--profile")
        {
            Description = "Packaging profile (recommended: desktop-public, desktop-internal, mac-notarized)"
        };
        var projectOpt = new Option<string?>("--project", "-p")
        {
            Description = "Path to the .csproj (required)"
        };
        var runtimeOpt = new Option<string>("--runtime", "-r")
        {
            Description = "Target runtime identifier (win-x64, osx-arm64, linux-x64, etc.)",
            DefaultValueFactory = _ => "win-x64"
        };
        var versionOpt = new Option<string?>("--version", "-v")
        {
            Description = "Version for the package (semver). Defaults to project version."
        };
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Output directory for packages. Default: ./Releases"
        };
        var iconOpt = new Option<string?>("--icon", "-i")
        {
            Description = "Path to icon file for the package"
        };
        var signParamsOpt = new Option<string?>("--sign-params", "-n")
        {
            Description = "Code signing parameters (platform-specific, passed to vpk)"
        };
        var notarizeOpt = new Option<bool>("--notarize")
        {
            Description = "Enable macOS notarization (uses --notaryProfile default when vpk available)"
        };
        var channelOpt = new Option<string>("--channel", "-c")
        {
            Description = "Release channel",
            DefaultValueFactory = _ => "stable"
        };
        var preflightOnlyOpt = new Option<bool>("--preflight-only")
        {
            Description = "Run packaging and bridge consistency preflight checks, then exit without publishing or packing"
        };

        var command = new Command("package")
        {
            Description = "Package application for distribution. Start with --profile desktop-public, desktop-internal, or mac-notarized."
        };
        command.Options.Add(profileOpt);
        command.Options.Add(projectOpt);
        command.Options.Add(runtimeOpt);
        command.Options.Add(versionOpt);
        command.Options.Add(outputOpt);
        command.Options.Add(iconOpt);
        command.Options.Add(signParamsOpt);
        command.Options.Add(notarizeOpt);
        command.Options.Add(channelOpt);
        command.Options.Add(preflightOnlyOpt);

        command.SetAction(async (parseResult, ct) =>
        {
            return await ExecuteAsync(
                profileName: parseResult.GetValue(profileOpt),
                explicitProject: parseResult.GetValue(projectOpt),
                version: parseResult.GetValue(versionOpt),
                outputDirectory: parseResult.GetValue(outputOpt),
                iconPath: parseResult.GetValue(iconOpt),
                signParams: parseResult.GetValue(signParamsOpt),
                preflightOnly: parseResult.GetValue(preflightOnlyOpt),
                parseResult: parseResult,
                runtimeOption: runtimeOpt,
                notarizeOption: notarizeOpt,
                channelOption: channelOpt,
                workingDirectory: RealEnvironmentContext.Instance.CurrentDirectory,
                output: Console.Out,
                error: Console.Error,
                processRunner: RealProcessRunner.Instance,
                environment: RealEnvironmentContext.Instance,
                fileSystem: RealFileSystem.Instance,
                ct);
        });

        return command;
    }

    internal static async Task<int> ExecuteAsync(
        string? profileName,
        string? explicitProject,
        string? version,
        string? outputDirectory,
        string? iconPath,
        string? signParams,
        bool preflightOnly,
        ParseResult parseResult,
        Option<string> runtimeOption,
        Option<bool> notarizeOption,
        Option<string> channelOption,
        string workingDirectory,
        TextWriter output,
        TextWriter error,
        IProcessRunner processRunner,
        IEnvironmentContext environment,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (!TryResolveProfile(profileName, out var profile))
            return 1;

        var workspaceConfig = FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem);
        var runtime = GetValue(runtimeOption, parseResult, profile.Runtime);
        var notarize = GetValue(notarizeOption, parseResult, profile.Notarize);
        var channel = GetValue(channelOption, parseResult, profile.Channel)!;
        var project = ResolveProjectPath(explicitProject, workingDirectory, fileSystem);

        if (string.IsNullOrWhiteSpace(project))
        {
            error.WriteLine("--project is required. Specify the path to your .csproj.");
            return 1;
        }

        project = project.Trim();

        var projectPath = Path.GetFullPath(project);
        if (!fileSystem.FileExists(projectPath))
        {
            error.WriteLine($"Project file not found: {projectPath}");
            return 1;
        }

        if (explicitProject is null && workspaceConfig is not null)
            output.WriteLine($"Using configured desktop project: {Path.GetFileName(projectPath)}");

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var packId = projectName;
        var packVersion = version ?? GetProjectVersion(projectPath, fileSystem) ?? "1.0.0";
        var resolvedOutputDir = Path.GetFullPath(outputDirectory ?? Path.Combine(projectDir, "Releases"));
        var publishDir = Path.Combine(environment.TempPath, "FuloraPackage", Guid.NewGuid().ToString("N"));
        var vpkPath = FindVpk(environment, fileSystem);

        foreach (var note in CollectPreflightNotes(profileName, runtime ?? "win-x64", notarize, signParams, vpkPath is not null, environment.IsMacOS))
            output.WriteLine($"Preflight: {note}");

        foreach (var note in CollectBridgeArtifactPreflightNotes(projectPath))
            output.WriteLine(note);

        if (preflightOnly)
        {
            output.WriteLine("Preflight complete.");
            return 0;
        }

        try
        {
            fileSystem.CreateDirectory(publishDir);

            var publishArgs = $"publish \"{projectPath}\" -c Release -r {runtime} --self-contained -o \"{publishDir}\"";
            output.WriteLine($"Running: dotnet {publishArgs}");
            var exitCode = await processRunner.RunAsync("dotnet", publishArgs, projectDir, ct);
            if (exitCode != 0)
            {
                error.WriteLine("dotnet publish failed.");
                return exitCode;
            }

            var mainExe = GetMainExeName(projectName, runtime ?? "win-x64");
            var mainExePath = Path.Combine(publishDir, mainExe);
            if (!fileSystem.FileExists(mainExePath))
            {
                var fallback = fileSystem.GetFiles(publishDir, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault()
                    ?? fileSystem.GetFiles(publishDir, "*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(f => !Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
                mainExe = fallback is not null ? Path.GetFileName(fallback) : mainExe;
            }

            if (vpkPath is not null)
            {
                var vpkArgs = $"pack -u {packId} -v {packVersion} -p \"{publishDir}\" -e \"{mainExe}\" -o \"{resolvedOutputDir}\" -c {channel}";
                if (!string.IsNullOrWhiteSpace(iconPath))
                    vpkArgs += $" -i \"{Path.GetFullPath(iconPath)}\"";
                if (!string.IsNullOrWhiteSpace(signParams))
                    vpkArgs += $" -n \"{signParams}\"";
                if (notarize && environment.IsMacOS)
                    vpkArgs += " --notaryProfile default";

                output.WriteLine($"Running: vpk {vpkArgs}");
                exitCode = await processRunner.RunAsync(vpkPath, vpkArgs, null, ct);
                if (exitCode != 0)
                {
                    error.WriteLine("vpk pack failed.");
                    return exitCode;
                }
            }
            else
            {
                fileSystem.CreateDirectory(resolvedOutputDir);
                var destDir = Path.Combine(resolvedOutputDir, $"{packId}-{packVersion}");
                if (fileSystem.DirectoryExists(destDir))
                    fileSystem.DeleteDirectory(destDir, recursive: true);
                CopyDirectory(publishDir, destDir, fileSystem);
                output.WriteLine($"Packaged to {destDir} (vpk not found; copied publish output)");
            }

            output.WriteLine();
            output.WriteLine($"Packages created in: {resolvedOutputDir}");
            return 0;
        }
        finally
        {
            if (fileSystem.DirectoryExists(publishDir))
            {
                try { fileSystem.DeleteDirectory(publishDir, recursive: true); }
                catch { /* Ignore */ }
            }
        }
    }

    private static string? GetProjectVersion(string projectPath, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        try
        {
            var xml = fileSystem.ReadAllText(projectPath);
            var match = System.Text.RegularExpressions.Regex.Match(xml, @"<Version>(.*?)</Version>");
            if (match.Success)
                return match.Groups[1].Value.Trim();
            match = System.Text.RegularExpressions.Regex.Match(xml, @"<VersionPrefix>(.*?)</VersionPrefix>");
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }
        catch { /* Ignore */ }
        return null;
    }

    private static string GetMainExeName(string projectName, string runtime)
    {
        if (runtime.StartsWith("win", StringComparison.OrdinalIgnoreCase))
            return $"{projectName}.exe";
        return projectName;
    }

    internal static string? FindVpk(IEnvironmentContext? environment = null, IFileSystem? fileSystem = null)
    {
        environment ??= RealEnvironmentContext.Instance;
        fileSystem ??= RealFileSystem.Instance;
        var pathEnv = environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var vpk = Path.Combine(dir.Trim(), environment.IsWindows ? "vpk.exe" : "vpk");
            if (fileSystem.FileExists(vpk))
                return vpk;
        }
        return null;
    }

    internal static IReadOnlyList<string> CollectPreflightNotes(
        string? profileName,
        string runtime,
        bool notarize,
        string? signParams,
        bool hasVpk,
        bool isMacOS)
    {
        var notes = new List<string>();
        var normalizedProfile = profileName?.Trim();

        if (string.Equals(normalizedProfile, "desktop-public", StringComparison.OrdinalIgnoreCase) && !hasVpk)
        {
            notes.Add("desktop-public is running without `vpk`; Fulora will copy publish output instead of producing installer/update packages.");
        }

        if (string.Equals(normalizedProfile, "mac-notarized", StringComparison.OrdinalIgnoreCase))
        {
            if (!isMacOS)
            {
                notes.Add("mac-notarized usually expects a macOS host; the current host may not be able to complete the notarization flow.");
            }

            if (!hasVpk)
            {
                notes.Add("mac-notarized requested without `vpk`; the fallback publish copy will not be notarized.");
            }

            if (!notarize)
            {
                notes.Add("mac-notarized profile is selected, but notarization is explicitly disabled.");
            }
        }

        if (!string.IsNullOrWhiteSpace(signParams) && !hasVpk)
        {
            notes.Add("Signing parameters were provided, but `vpk` is unavailable, so signing arguments will not be applied.");
        }

        if (runtime.StartsWith("osx", StringComparison.OrdinalIgnoreCase) && !isMacOS)
        {
            notes.Add("Packaging for a macOS runtime on a non-macOS host may require additional signing/notarization steps on macOS.");
        }

        return notes;
    }

    internal static string? ResolveProjectPath(string? explicitProject, string workingDirectory, IFileSystem? fileSystem = null)
        => explicitProject
            ?? FuloraWorkspaceConfigResolver.Load(workingDirectory, fileSystem)?.ResolveDesktopProject();

    internal static IReadOnlyList<string> CollectBridgeArtifactPreflightNotes(string projectPath, IFileSystem? fileSystem = null)
    {
        fileSystem ??= RealFileSystem.Instance;
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        var solutionRoot = projectDirectory is null ? null : Path.GetDirectoryName(projectDirectory);
        if (string.IsNullOrWhiteSpace(solutionRoot))
            return [];

        var bridgeProject = GenerateCommand.DetectBridgeProject(solutionRoot, fileSystem);
        if (string.IsNullOrWhiteSpace(bridgeProject))
            return [];

        return BridgeArtifactConsistency.FormatWarnings(GenerateCommand.CollectArtifactConsistencyWarnings(bridgeProject));
    }

    internal static bool TryResolveProfile(string? profileName, out PackageProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            profile = new PackageProfile(string.Empty, "stable", null, false);
            return true;
        }

        if (PackageProfileDefaults.TryResolve(profileName, out profile))
        {
            return true;
        }

        Console.Error.WriteLine($"Unknown package profile '{profileName}'. Supported profiles: desktop-internal, desktop-public, mac-notarized.");
        return false;
    }

    internal static T GetValue<T>(Option<T> option, ParseResult parseResult, T? profileDefault)
    {
        var optionResult = parseResult.GetResult(option);
        if (optionResult is OptionResult { Implicit: false })
        {
            return parseResult.GetValue(option)!;
        }

        return profileDefault is not null ? profileDefault : parseResult.GetValue(option)!;
    }

    private static void CopyDirectory(string source, string dest, IFileSystem fileSystem)
    {
        fileSystem.CreateDirectory(dest);
        foreach (var file in fileSystem.GetFiles(source, "*", SearchOption.TopDirectoryOnly))
            fileSystem.CopyFile(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in fileSystem.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)), fileSystem);
    }
}
