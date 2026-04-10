namespace Agibuild.Fulora.Cli.Commands;

internal interface IEnvironmentContext
{
    string CurrentDirectory { get; }
    bool IsWindows { get; }
    bool IsMacOS { get; }
    string? GetEnvironmentVariable(string variable);
    string TempPath { get; }
}

internal sealed class RealEnvironmentContext : IEnvironmentContext
{
    internal static RealEnvironmentContext Instance { get; } = new();

    private RealEnvironmentContext()
    {
    }

    public string CurrentDirectory => Directory.GetCurrentDirectory();

    public bool IsWindows => OperatingSystem.IsWindows();

    public bool IsMacOS => OperatingSystem.IsMacOS();

    public string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);

    public string TempPath => Path.GetTempPath();
}
