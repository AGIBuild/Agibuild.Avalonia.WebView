using Agibuild.Fulora.Cli.Commands;

namespace Agibuild.Fulora.UnitTests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public List<(string FileName, string Arguments, string? WorkingDirectory)> Calls { get; } = [];

    public Func<string, string, string?, int>? ExitCodeFactory { get; set; }
    public Func<string, string, string?, CancellationToken, Task<int>>? RunCallbackAsync { get; set; }

    public Task<int> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct)
    {
        Calls.Add((fileName, arguments, workingDirectory));
        if (RunCallbackAsync is not null)
            return RunCallbackAsync(fileName, arguments, workingDirectory, ct);
        return Task.FromResult(ExitCodeFactory?.Invoke(fileName, arguments, workingDirectory) ?? 0);
    }
}
