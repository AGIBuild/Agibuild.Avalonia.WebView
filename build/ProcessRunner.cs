using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591 // build infrastructure — no XML doc needed

public sealed record ProcessCommand(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null);

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    public bool IsSuccess => ExitCode == 0;
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        ProcessCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        ProcessCommand command,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
            startInfo.WorkingDirectory = command.WorkingDirectory;

        foreach (var argument in command.Arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Failed to start process '{command.FileName}'.");
        }

        var stopwatch = Stopwatch.StartNew();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (command.Timeout is { } timeout)
            linkedCts.CancelAfter(timeout);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException(
                $"Process '{command.FileName}' timed out after {command.Timeout}.");
        }
        catch
        {
            TryKill(process);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        stopwatch.Stop();

        return new ProcessResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask,
            stopwatch.Elapsed);
    }

    static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best-effort kill
        }
    }
}
