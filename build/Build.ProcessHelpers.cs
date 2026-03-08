using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common.IO;

partial class BuildTask
{
    static readonly JsonSerializerOptions WriteIndentedJsonOptions = new() { WriteIndented = true };

    static void WriteJsonReport(AbsolutePath path, object payload)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteIndentedJsonOptions));
    }

    sealed class TempDirectoryScope : IDisposable
    {
        readonly string _path;

        TempDirectoryScope(string path)
        {
            _path = path;
            Directory.CreateDirectory(_path);
        }

        public string Path => _path;

        public static TempDirectoryScope Create(string prefix)
        {
            var path = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"{prefix}-{Guid.NewGuid():N}"));
            return new TempDirectoryScope(path);
        }

        public void Dispose()
        {
            if (!Directory.Exists(_path))
                return;

            try
            {
                Directory.Delete(_path, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    static string RunProcess(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        // Read both streams concurrently to prevent buffer-full deadlock
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        var output = stdoutTask.GetAwaiter().GetResult();
        var error = stderrTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            Serilog.Log.Warning("Process stderr: {Error}", error.Trim());
        }

        return output;
    }

    static string RunProcessCaptureAll(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        var output = stdoutTask.GetAwaiter().GetResult();
        var error = stderrTask.GetAwaiter().GetResult();

        return string.Join('\n', new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    static string RunNpmCaptureAll(string arguments, string workingDirectory, int timeoutMs = 30_000)
    {
        if (OperatingSystem.IsWindows())
        {
            return RunProcessCaptureAll(
                "cmd.exe",
                $"/d /s /c \"npm {arguments}\"",
                workingDirectory: workingDirectory,
                timeoutMs: timeoutMs);
        }

        return RunProcessCaptureAll(
            "npm",
            arguments,
            workingDirectory: workingDirectory,
            timeoutMs: timeoutMs);
    }

    static string RunProcessCaptureAllChecked(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        var output = stdoutTask.GetAwaiter().GetResult();
        var error = stderrTask.GetAwaiter().GetResult();

        var combined = string.Join('\n', new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}.\n{combined}");
        }

        return combined;
    }

    static bool IsToolAvailable(string toolName)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                RunProcessCaptureAllChecked(
                    "cmd.exe",
                    $"/d /s /c \"{toolName} --version\"",
                    timeoutMs: 5_000);
            }
            else
            {
                RunProcessCaptureAllChecked(toolName, "--version", timeoutMs: 5_000);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    static void RunPmInstall(string pm, string workingDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            RunProcessCaptureAllChecked("cmd.exe", $"/d /s /c \"{pm} install\"",
                workingDirectory: workingDirectory, timeoutMs: 120_000);
        }
        else
        {
            RunProcessCaptureAllChecked(pm, "install",
                workingDirectory: workingDirectory, timeoutMs: 120_000);
        }
    }
}
