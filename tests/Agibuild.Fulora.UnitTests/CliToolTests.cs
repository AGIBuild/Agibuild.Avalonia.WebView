using System.Diagnostics;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public class CliToolTests
{
    private static string GetCliProjectPath()
    {
        var repoRoot = FindRepoRoot();
        return Path.Combine(repoRoot, "src", "Agibuild.Fulora.Cli", "Agibuild.Fulora.Cli.csproj");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static async Task<(string Stdout, string Stderr, int ExitCode)> RunCliAsync(string args)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{GetCliProjectPath()}\" -- {args}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = FindRepoRoot(),
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (stdout, stderr, process.ExitCode);
    }

    [Fact]
    public async Task Help_shows_all_commands()
    {
        var (stdout, _, exitCode) = await RunCliAsync("--help");
        Assert.Equal(0, exitCode);
        Assert.Contains("new", stdout);
        Assert.Contains("generate", stdout);
        Assert.Contains("dev", stdout);
        Assert.Contains("add", stdout);
    }

    [Fact]
    public async Task New_command_shows_help()
    {
        var (stdout, _, exitCode) = await RunCliAsync("new --help");
        Assert.Equal(0, exitCode);
        Assert.Contains("--frontend", stdout);
        Assert.Contains("react", stdout);
    }

    [Fact]
    public async Task Add_service_command_shows_help()
    {
        var (stdout, _, exitCode) = await RunCliAsync("add service --help");
        Assert.Equal(0, exitCode);
        Assert.Contains("name", stdout);
        Assert.Contains("--import", stdout);
    }

    [Fact]
    public async Task Generate_types_command_shows_help()
    {
        var (stdout, _, exitCode) = await RunCliAsync("generate types --help");
        Assert.Equal(0, exitCode);
        Assert.Contains("--project", stdout);
    }

    [Fact]
    public async Task Dev_command_shows_help()
    {
        var (stdout, _, exitCode) = await RunCliAsync("dev --help");
        Assert.Equal(0, exitCode);
        Assert.Contains("Vite", stdout);
    }

    [Fact]
    public void Cli_project_exists_and_is_packable()
    {
        var csproj = GetCliProjectPath();
        Assert.True(File.Exists(csproj), $"CLI project not found at {csproj}");

        var content = File.ReadAllText(csproj);
        Assert.Contains("PackAsTool", content);
        Assert.Contains("fulora", content);
        Assert.Contains("System.CommandLine", content);
    }
}
