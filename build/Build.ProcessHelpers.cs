using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Nuke.Common.IO;

partial class BuildTask
{
    static readonly JsonSerializerOptions WriteIndentedJsonOptions = new() { WriteIndented = true };
    static readonly ProcessRunner Runner = new();

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

    // ──────────────────────────── Process convenience wrappers ────────────────────────────

    static async Task<string> RunProcessAsync(
        string fileName,
        string[] arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var result = await Runner.RunAsync(
            new ProcessCommand(fileName, arguments, workingDirectory, timeout));

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardError))
            Serilog.Log.Warning("Process stderr: {Error}", result.StandardError.Trim());

        return result.StandardOutput;
    }

    static async Task<string> RunProcessCaptureAllAsync(
        string fileName,
        string[] arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var result = await Runner.RunAsync(
            new ProcessCommand(fileName, arguments, workingDirectory, timeout));

        return CombineOutput(result.StandardOutput, result.StandardError);
    }

    static async Task<string> RunProcessCheckedAsync(
        string fileName,
        string[] arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var result = await Runner.RunAsync(
            new ProcessCommand(fileName, arguments, workingDirectory, timeout));

        var combined = CombineOutput(result.StandardOutput, result.StandardError);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Process '{fileName} {string.Join(' ', arguments)}' failed with exit code {result.ExitCode}.\n{combined}");
        }

        return combined;
    }

    // ──────────────────────────── npm / package-manager helpers ────────────────────────────

    static Task<string> RunNpmCaptureAllAsync(
        string[] arguments,
        string workingDirectory,
        TimeSpan? timeout = null)
    {
        return OperatingSystem.IsWindows()
            ? RunProcessCaptureAllAsync(
                "cmd.exe",
                ["/d", "/s", "/c", $"npm {string.Join(' ', arguments)}"],
                workingDirectory,
                timeout)
            : RunProcessCaptureAllAsync("npm", arguments, workingDirectory, timeout);
    }

    static Task<string> RunNpmCheckedAsync(
        string[] arguments,
        string workingDirectory,
        TimeSpan? timeout = null)
    {
        return OperatingSystem.IsWindows()
            ? RunProcessCheckedAsync(
                "cmd.exe",
                ["/d", "/s", "/c", $"npm {string.Join(' ', arguments)}"],
                workingDirectory,
                timeout)
            : RunProcessCheckedAsync("npm", arguments, workingDirectory, timeout);
    }

    static async Task<bool> IsToolAvailableAsync(string toolName)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                await RunProcessCheckedAsync(
                    "cmd.exe",
                    ["/d", "/s", "/c", $"{toolName} --version"],
                    timeout: TimeSpan.FromSeconds(5));
            }
            else
            {
                await RunProcessCheckedAsync(
                    toolName,
                    ["--version"],
                    timeout: TimeSpan.FromSeconds(5));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    static Task RunPmInstallAsync(string pm, string workingDirectory)
    {
        return OperatingSystem.IsWindows()
            ? RunProcessCheckedAsync(
                "cmd.exe",
                ["/d", "/s", "/c", $"{pm} install"],
                workingDirectory,
                TimeSpan.FromMinutes(2))
            : RunProcessCheckedAsync(pm, ["install"], workingDirectory, TimeSpan.FromMinutes(2));
    }

    // ──────────────────────────── Shared helpers ────────────────────────────

    static string CombineOutput(string stdout, string stderr)
    {
        return string.Join('\n',
            new[] { stdout, stderr }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
