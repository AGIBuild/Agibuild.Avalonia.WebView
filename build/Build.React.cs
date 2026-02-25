using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class BuildTask
{
    static void EnsureNpmAvailable(string workingDirectory)
    {
        try
        {
            RunNpmProcess("--version", workingDirectory: workingDirectory, timeoutMs: 10_000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                $"npm is required but is not available. Install Node.js and make sure npm is on PATH. Working directory: '{workingDirectory}'.",
                ex);
        }
    }

    static string RunNpmProcess(string arguments, string workingDirectory, int timeoutMs)
    {
        using var process = new Process
        {
            StartInfo = CreateNpmProcessStartInfo(arguments, workingDirectory, redirectStdout: true, redirectStderr: true)
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process 'npm {arguments}' timed out after {timeoutMs}ms.");
        }

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(
                $"npm {arguments} failed with exit code {process.ExitCode}. {details.Trim()}");
        }

        return output;
    }

    static ProcessStartInfo CreateNpmProcessStartInfo(string arguments, string workingDirectory, bool redirectStdout, bool redirectStderr)
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /s /c \"npm {arguments}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = redirectStdout,
                RedirectStandardError = redirectStderr,
                CreateNoWindow = false,
            };
        }

        return new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = redirectStdout,
            RedirectStandardError = redirectStderr,
            CreateNoWindow = false,
        };
    }

    void EnsureReactDepsInstalled()
    {
        Assert.DirectoryExists(ReactWebDirectory, $"React web project not found at {ReactWebDirectory}.");
        EnsureNpmAvailable(ReactWebDirectory);

        var nodeModules = ReactWebDirectory / "node_modules";
        if (!Directory.Exists(nodeModules))
        {
            Serilog.Log.Information("node_modules not found, running npm install...");
            RunNpmProcess("install", workingDirectory: ReactWebDirectory, timeoutMs: 120_000);
            Serilog.Log.Information("npm install completed.");
        }
    }

    static bool IsHttpReady(string url)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = http.GetAsync(url).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    static void WaitForPort(int port, int timeoutSeconds = 30)
    {
        var url = $"http://localhost:{port}";
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (IsHttpReady(url)) return;
            Thread.Sleep(500);
        }

        throw new TimeoutException($"{url} did not become available within {timeoutSeconds}s.");
    }

    // ──────────────────────────── React Sample Targets ────────────────────────────

    Target StartReactDev => _ => _
        .Description("Starts the React Vite dev server for the AvaloniReact sample (standalone, foreground).")
        .Executes(() =>
        {
            EnsureReactDepsInstalled();

            Serilog.Log.Information("Starting Vite dev server on http://localhost:5173 ...");
            var process = new Process
            {
                StartInfo = CreateNpmProcessStartInfo("run dev", ReactWebDirectory, redirectStdout: false, redirectStderr: false)
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                Serilog.Log.Warning("Vite dev server exited with code {Code}.", process.ExitCode);
        });

    Target StartReactApp => _ => _
        .Description("Launches the AvaloniReact desktop sample. In Debug: auto-starts Vite dev server if needed.")
        .Executes(() =>
        {
            Assert.FileExists(ReactDesktopProject, $"AvaloniReact desktop project not found at {ReactDesktopProject}.");

            Process? viteProcess = null;

            if (string.Equals(Configuration, "Debug", StringComparison.OrdinalIgnoreCase))
            {
                if (IsHttpReady("http://localhost:5173"))
                {
                    Serilog.Log.Information("Vite dev server already running on port 5173.");
                }
                else
                {
                    EnsureReactDepsInstalled();

                    Serilog.Log.Information("Starting Vite dev server in background...");
                    viteProcess = new Process
                    {
                        StartInfo = CreateNpmProcessStartInfo("run dev", ReactWebDirectory, redirectStdout: false, redirectStderr: false)
                    };
                    viteProcess.Start();

                    WaitForPort(5173, timeoutSeconds: 60);
                    Serilog.Log.Information("Vite dev server is ready on http://localhost:5173");
                }
            }

            try
            {
                DotNetRun(s => s
                    .SetProjectFile(ReactDesktopProject)
                    .SetConfiguration(Configuration));
            }
            finally
            {
                if (viteProcess is { HasExited: false })
                {
                    Serilog.Log.Information("Stopping Vite dev server...");
                    try { viteProcess.Kill(entireProcessTree: true); }
                    catch { /* best effort */ }
                    viteProcess.Dispose();
                }
            }
        });
}
