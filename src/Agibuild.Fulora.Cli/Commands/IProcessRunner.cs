using System.Diagnostics;

namespace Agibuild.Fulora.Cli.Commands;

internal interface IProcessRunner
{
    Task<int> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct);
}

internal sealed class RealProcessRunner : IProcessRunner
{
    internal static RealProcessRunner Instance { get; } = new();

    private RealProcessRunner()
    {
    }

    public async Task<int> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? RealEnvironmentContext.Instance.CurrentDirectory,
        };

        using var process = Process.Start(psi);
        if (process is null)
            return -1;

        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}
