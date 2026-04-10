using System.CommandLine;
using System.CommandLine.Parsing;
using Agibuild.Fulora.Cli.Commands;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class PackageCommandTests
{
    [Fact]
    public void PackageCommand_creates_valid_command_with_expected_options()
    {
        var command = PackageCommand.Create();
        Assert.NotNull(command);
        Assert.Equal("package", command.Name);

        var optionNames = command.Options.Select(o => o.Name).ToHashSet();
        Assert.Contains("--project", optionNames);
        Assert.Contains("--runtime", optionNames);
        Assert.Contains("--version", optionNames);
        Assert.Contains("--output", optionNames);
        Assert.Contains("--icon", optionNames);
        Assert.Contains("--sign-params", optionNames);
        Assert.Contains("--notarize", optionNames);
        Assert.Contains("--channel", optionNames);
    }

    [Fact]
    public void PackageCommand_creates_valid_command_with_profile_option()
    {
        var command = PackageCommand.Create();

        var optionNames = command.Options.Select(o => o.Name).ToHashSet();
        Assert.Contains("--profile", optionNames);
    }

    [Fact]
    public void ResolveProfile_desktop_public_sets_stable_channel_defaults()
    {
        var profile = PackageProfileDefaults.Resolve("desktop-public");

        Assert.Equal("stable", profile.Channel);
        Assert.False(profile.Notarize);
    }

    [Fact]
    public void PackageCommand_profile_defaults_allow_explicit_false_notarize_override()
    {
        var command = PackageCommand.Create();
        var notarizeOption = Assert.IsType<Option<bool>>(command.Options.Single(o => o.Name == "--notarize"));
        var profile = PackageProfileDefaults.Resolve("mac-notarized");
        var root = new RootCommand("test");
        root.Subcommands.Add(command);
        var parseResult = root.Parse("package --profile mac-notarized --notarize false");

        var optionResult = parseResult.GetResult(notarizeOption);

        Assert.NotNull(optionResult);
        Assert.False(optionResult!.Implicit);
        Assert.False(PackageCommand.GetValue(notarizeOption, parseResult, profile.Notarize));
    }

    [Fact]
    public void PackageCommand_profile_defaults_apply_when_options_are_implicit()
    {
        var command = PackageCommand.Create();
        var runtimeOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--runtime"));
        var channelOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--channel"));
        var notarizeOption = Assert.IsType<Option<bool>>(command.Options.Single(o => o.Name == "--notarize"));
        var profile = PackageProfileDefaults.Resolve("mac-notarized");
        var root = new RootCommand("test");
        root.Subcommands.Add(command);
        var parseResult = root.Parse("package --profile mac-notarized");

        var runtimeResult = parseResult.GetResult(runtimeOption);
        var channelResult = parseResult.GetResult(channelOption);
        var notarizeResult = parseResult.GetResult(notarizeOption);

        Assert.NotNull(runtimeResult);
        Assert.NotNull(channelResult);
        Assert.NotNull(notarizeResult);
        Assert.True(runtimeResult!.Implicit);
        Assert.True(channelResult!.Implicit);
        Assert.True(notarizeResult!.Implicit);
        Assert.Equal("osx-arm64", PackageCommand.GetValue(runtimeOption, parseResult, profile.Runtime));
        Assert.Equal("stable", PackageCommand.GetValue(channelOption, parseResult, profile.Channel));
        Assert.True(PackageCommand.GetValue(notarizeOption, parseResult, profile.Notarize));
    }

    [Fact]
    public void PackageCommand_profile_defaults_allow_explicit_runtime_channel_and_notarize_overrides()
    {
        var command = PackageCommand.Create();
        var runtimeOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--runtime"));
        var channelOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--channel"));
        var notarizeOption = Assert.IsType<Option<bool>>(command.Options.Single(o => o.Name == "--notarize"));
        var profile = PackageProfileDefaults.Resolve("mac-notarized");
        var root = new RootCommand("test");
        root.Subcommands.Add(command);
        var parseResult = root.Parse("package --profile mac-notarized --runtime osx-x64 --channel preview --notarize false");

        var runtimeResult = parseResult.GetResult(runtimeOption);
        var channelResult = parseResult.GetResult(channelOption);
        var notarizeResult = parseResult.GetResult(notarizeOption);

        Assert.NotNull(runtimeResult);
        Assert.NotNull(channelResult);
        Assert.NotNull(notarizeResult);
        Assert.False(runtimeResult!.Implicit);
        Assert.False(channelResult!.Implicit);
        Assert.False(notarizeResult!.Implicit);
        Assert.Equal("osx-x64", PackageCommand.GetValue(runtimeOption, parseResult, profile.Runtime));
        Assert.Equal("preview", PackageCommand.GetValue(channelOption, parseResult, profile.Channel));
        Assert.False(PackageCommand.GetValue(notarizeOption, parseResult, profile.Notarize));
    }

    [Fact]
    public void PackageCommand_try_resolve_profile_reports_unknown_profiles()
    {
        using var stderr = new StringWriter();
        var previousError = Console.Error;
        Console.SetError(stderr);

        try
        {
            var resolved = PackageCommand.TryResolveProfile("unknown-profile", out var profile);

            Assert.False(resolved);
            Assert.Equal(string.Empty, profile.Name);
            Assert.Equal("stable", profile.Channel);
            Assert.Null(profile.Runtime);
            Assert.False(profile.Notarize);
            Assert.Contains("Unknown package profile 'unknown-profile'.", stderr.ToString(), StringComparison.Ordinal);
            Assert.Contains("desktop-internal, desktop-public, mac-notarized", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(previousError);
        }
    }

    [Fact]
    public void PackageCommand_validates_missing_project_argument()
    {
        var root = new RootCommand("test");
        root.Subcommands.Add(PackageCommand.Create());
        var parseResult = root.Parse("package");
        var exitCode = parseResult.Invoke();
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void PackageCommand_collects_preflight_notes_for_desktop_public_without_vpk()
    {
        var notes = PackageCommand.CollectPreflightNotes(
            profileName: "desktop-public",
            runtime: "win-x64",
            notarize: false,
            signParams: null,
            hasVpk: false,
            isMacOS: false);

        Assert.Contains(notes, note => note.Contains("desktop-public", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("vpk", StringComparison.Ordinal));
    }

    [Fact]
    public void PackageCommand_collects_preflight_notes_for_mac_notarized_without_vpk()
    {
        var notes = PackageCommand.CollectPreflightNotes(
            profileName: "mac-notarized",
            runtime: "osx-arm64",
            notarize: true,
            signParams: null,
            hasVpk: false,
            isMacOS: true);

        Assert.Contains(notes, note => note.Contains("notarized", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(notes, note => note.Contains("vpk", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("not be notarized", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PackageCommand_collects_preflight_notes_for_mac_notarized_on_non_macos_host()
    {
        var notes = PackageCommand.CollectPreflightNotes(
            profileName: "mac-notarized",
            runtime: "osx-arm64",
            notarize: true,
            signParams: null,
            hasVpk: true,
            isMacOS: false);

        Assert.Contains(notes, note => note.Contains("macOS", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("current host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PackageCommand_execute_preflight_uses_fake_environment_and_skips_process_execution()
    {
        var workspaceRoot = Path.GetFullPath("/virtual/package-workspace");
        var desktopProject = Path.Combine(workspaceRoot, "apps", "native-host", "Host.csproj");
        var fileSystem = new FakeFileSystem();
        var processRunner = new FakeProcessRunner();
        var environment = new FakeEnvironmentContext
        {
            CurrentDirectory = workspaceRoot,
            IsWindows = false,
            IsMacOS = false,
            TempPath = Path.Combine(workspaceRoot, ".tmp")
        };

        fileSystem.AddFile(
            desktopProject,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Version>1.2.3</Version>
              </PropertyGroup>
            </Project>
            """);

        FuloraWorkspaceConfigResolver.Save(
            workspaceRoot,
            new FuloraWorkspaceConfig
            {
                Desktop = new FuloraWorkspaceConfig.DesktopSection
                {
                    Project = "./apps/native-host/Host.csproj"
                }
            },
            fileSystem);

        var command = PackageCommand.Create();
        var root = new RootCommand("test");
        root.Subcommands.Add(command);
        var parseResult = root.Parse("package --profile desktop-public --preflight-only");
        var runtimeOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--runtime"));
        var notarizeOption = Assert.IsType<Option<bool>>(command.Options.Single(o => o.Name == "--notarize"));
        var channelOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--channel"));

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await PackageCommand.ExecuteAsync(
            profileName: "desktop-public",
            explicitProject: null,
            version: null,
            outputDirectory: null,
            iconPath: null,
            signParams: null,
            preflightOnly: true,
            parseResult: parseResult,
            runtimeOption: runtimeOption,
            notarizeOption: notarizeOption,
            channelOption: channelOption,
            workingDirectory: workspaceRoot,
            output: stdout,
            error: stderr,
            processRunner: processRunner,
            environment: environment,
            fileSystem: fileSystem,
            ct: CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("Using configured desktop project: Host", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Preflight complete.", stdout.ToString(), StringComparison.Ordinal);
        Assert.Empty(processRunner.Calls);
    }

    [Fact]
    public async Task PackageCommand_execute_without_vpk_copies_publish_output_via_fake_file_system()
    {
        var workspaceRoot = Path.GetFullPath("/virtual/package-copy-workspace");
        var desktopProject = Path.Combine(workspaceRoot, "apps", "native-host", "Host.csproj");
        var fileSystem = new FakeFileSystem();
        var environment = new FakeEnvironmentContext
        {
            CurrentDirectory = workspaceRoot,
            IsWindows = true,
            IsMacOS = false,
            TempPath = Path.Combine(workspaceRoot, ".tmp")
        };
        var processRunner = new FakeProcessRunner();

        fileSystem.AddFile(
            desktopProject,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Version>2.3.4</Version>
              </PropertyGroup>
            </Project>
            """);

        processRunner.RunCallbackAsync = (fileName, arguments, _, _) =>
        {
            if (fileName == "dotnet" && arguments.Contains("publish", StringComparison.Ordinal))
            {
                var outputMarker = "-o \"";
                var start = arguments.IndexOf(outputMarker, StringComparison.Ordinal);
                Assert.True(start >= 0);
                start += outputMarker.Length;
                var end = arguments.IndexOf('"', start);
                var publishDir = arguments[start..end];
                fileSystem.AddFile(Path.Combine(publishDir, "Host.exe"), "binary");
                fileSystem.AddFile(Path.Combine(publishDir, "Host.dll"), "library");
            }

            return Task.FromResult(0);
        };

        var command = PackageCommand.Create();
        var root = new RootCommand("test");
        root.Subcommands.Add(command);
        var parseResult = root.Parse($"package --project \"{desktopProject}\" --profile desktop-public");
        var runtimeOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--runtime"));
        var notarizeOption = Assert.IsType<Option<bool>>(command.Options.Single(o => o.Name == "--notarize"));
        var channelOption = Assert.IsType<Option<string>>(command.Options.Single(o => o.Name == "--channel"));

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await PackageCommand.ExecuteAsync(
            profileName: "desktop-public",
            explicitProject: desktopProject,
            version: null,
            outputDirectory: null,
            iconPath: null,
            signParams: null,
            preflightOnly: false,
            parseResult: parseResult,
            runtimeOption: runtimeOption,
            notarizeOption: notarizeOption,
            channelOption: channelOption,
            workingDirectory: workspaceRoot,
            output: stdout,
            error: stderr,
            processRunner: processRunner,
            environment: environment,
            fileSystem: fileSystem,
            ct: CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("Packaged to", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Packages created in", stdout.ToString(), StringComparison.Ordinal);
        Assert.True(fileSystem.FileExists(Path.Combine(Path.GetDirectoryName(desktopProject)!, "Releases", "Host-2.3.4", "Host.exe")));
        Assert.Single(processRunner.Calls);
        Assert.Equal("dotnet", processRunner.Calls[0].FileName);
    }
}
