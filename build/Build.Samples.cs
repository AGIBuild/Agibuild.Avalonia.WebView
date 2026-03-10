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
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException($"Process 'npm {arguments}' timed out after {timeoutMs}ms.");
        }

        var output = stdoutTask.GetAwaiter().GetResult();
        var error = stderrTask.GetAwaiter().GetResult();

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

    void EnsureSampleWebDepsInstalled(AbsolutePath webDirectory)
    {
        Assert.DirectoryExists(webDirectory, $"Web project not found at {webDirectory}.");
        EnsureNpmAvailable(webDirectory);

        var nodeModules = webDirectory / "node_modules";
        var bridgeRuntimeEntry = nodeModules / "@agibuild" / "bridge" / "dist" / "index.js";
        if (!Directory.Exists(nodeModules))
        {
            Serilog.Log.Information("node_modules not found in {Dir}, running npm install...", webDirectory);
            RunNpmProcess("install", workingDirectory: webDirectory, timeoutMs: 120_000);
            Serilog.Log.Information("npm install completed.");
            return;
        }

        if (!File.Exists(bridgeRuntimeEntry))
        {
            Serilog.Log.Information("Bridge runtime entry not found at {Path}, refreshing npm install...", bridgeRuntimeEntry);
            RunNpmProcess("install", workingDirectory: webDirectory, timeoutMs: 120_000);
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

    void StartDesktopApp(AbsolutePath desktopProject, AbsolutePath webDirectory, int devPort)
    {
        Assert.FileExists(desktopProject, $"Desktop project not found at {desktopProject}.");

        Process? viteProcess = null;

        if (string.Equals(Configuration, "Debug", StringComparison.OrdinalIgnoreCase))
        {
            var devUrl = $"http://localhost:{devPort}";
            if (IsHttpReady(devUrl))
            {
                Serilog.Log.Information("Vite dev server already running on port {Port}.", devPort);
            }
            else
            {
                EnsureSampleWebDepsInstalled(webDirectory);

                Serilog.Log.Information("Starting Vite dev server in background...");
                viteProcess = new Process
                {
                    StartInfo = CreateNpmProcessStartInfo("run dev", webDirectory, redirectStdout: false, redirectStderr: false)
                };
                viteProcess.Start();

                WaitForPort(devPort, timeoutSeconds: 60);
                Serilog.Log.Information("Vite dev server is ready on http://localhost:{Port}", devPort);
            }
        }

        try
        {
            DotNetRun(s => s
                .SetProjectFile(desktopProject)
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
    }

    // ──────────────────────────── Sample App Targets ────────────────────────────

    Target StartAiChatApp => _ => _
        .Description("Launches the AI Chat sample. Ensures Ollama is running and the required model is available.")
        .Executes(() =>
        {
            EnsureOllamaReady();
            StartDesktopApp(AiChatDesktopProject, AiChatWebDirectory, devPort: 5175);
        });

    Target StartReactApp => _ => _
        .Description("Launches the React sample. In Debug: auto-starts Vite dev server if needed.")
        .Executes(() => StartDesktopApp(ReactDesktopProject, ReactWebDirectory, devPort: 5173));

    Target StartVueApp => _ => _
        .Description("Launches the Vue sample. In Debug: auto-starts Vite dev server if needed.")
        .Executes(() => StartDesktopApp(VueDesktopProject, VueWebDirectory, devPort: 5174));

    Target StartTodoApp => _ => _
        .Description("Launches the Showcase Todo sample. In Debug: auto-starts Vite dev server if needed.")
        .Executes(() => StartDesktopApp(TodoDesktopProject, TodoWebDirectory, devPort: 5176));

    Target StartMinimalApp => _ => _
        .Description("Launches the Minimal Hybrid sample (static wwwroot, no Vite).")
        .Executes(() =>
        {
            Assert.FileExists(MinimalHybridDesktopProject, $"Desktop project not found at {MinimalHybridDesktopProject}.");
            DotNetRun(s => s
                .SetProjectFile(MinimalHybridDesktopProject)
                .SetConfiguration(Configuration));
        });

    // ──────────────────────────── Ollama Bootstrapping ───────────────────────────

    const string OllamaEndpoint = "http://localhost:11434";
    const string OllamaModel = "qwen2.5:3b";

    static Process? _ollamaServeProcess;

    void EnsureOllamaReady()
    {
        var echoMode = string.Equals(
            Environment.GetEnvironmentVariable("AI__PROVIDER"), "echo", StringComparison.OrdinalIgnoreCase);
        if (echoMode)
        {
            Serilog.Log.Information("AI__PROVIDER=echo — skipping Ollama bootstrap.");
            return;
        }

        if (!IsToolAvailable("ollama"))
        {
            Serilog.Log.Warning(
                "ollama is not installed. The app will prompt the user to install it. " +
                "Download from https://ollama.com/download");
            return;
        }

        Serilog.Log.Information("ollama is installed.");

        var apiUrl = $"{OllamaEndpoint.TrimEnd('/')}/api/tags";
        if (!IsHttpReady(apiUrl))
        {
            Serilog.Log.Information("Ollama API is not reachable at {Url}. Starting 'ollama serve' in background...", OllamaEndpoint);
            _ollamaServeProcess = StartOllamaServe();
            WaitForOllamaApi(timeoutSeconds: 20);
            Serilog.Log.Information("Ollama API is now reachable.");
        }
        else
        {
            Serilog.Log.Information("Ollama API is already running at {Url}.", OllamaEndpoint);
        }

        if (IsModelAvailable(OllamaModel))
        {
            Serilog.Log.Information("Model '{Model}' is already available.", OllamaModel);
        }
        else
        {
            Serilog.Log.Information("Model '{Model}' not found. Pulling (this may take a few minutes)...", OllamaModel);
            PullOllamaModel(OllamaModel);
            Serilog.Log.Information("Model '{Model}' is now available.", OllamaModel);
        }
    }

    static Process StartOllamaServe()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = "serve",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
        };
        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }

    void WaitForOllamaApi(int timeoutSeconds = 20)
    {
        var apiUrl = $"{OllamaEndpoint.TrimEnd('/')}/api/tags";
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (IsHttpReady(apiUrl)) return;
            Thread.Sleep(500);
        }

        throw new TimeoutException(
            $"Ollama API at {OllamaEndpoint} did not become available within {timeoutSeconds}s. " +
            "Check that 'ollama serve' is working correctly.");
    }

    static bool IsModelAvailable(string model)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = http.GetAsync($"{OllamaEndpoint.TrimEnd('/')}/api/tags").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return false;
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return body.Contains($"\"name\":\"{model}\"", StringComparison.OrdinalIgnoreCase)
                || body.Contains($"\"name\": \"{model}\"", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    static void PullOllamaModel(string model)
    {
        var output = RunProcessCaptureAllChecked("ollama", $"pull {model}", timeoutMs: 600_000);
        Serilog.Log.Debug("ollama pull output: {Output}", output.Trim());
    }
}
