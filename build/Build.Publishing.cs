using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Nuke.Common.IO;

partial class BuildTask
{
    private sealed record NugetSmokeRetryTelemetry(
        int Attempt,
        string Classification,
        string Outcome,
        string? Message);

    private sealed record NugetPackagesRootResolution(string Path, string Source);

    static NugetPackagesRootResolution ResolveNugetPackagesRoot()
    {
        var fromEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return new NugetPackagesRootResolution(
                NormalizePath(fromEnv),
                "NUGET_PACKAGES");
        }

        try
        {
            var output = RunProcess("dotnet", "nuget locals global-packages --list", timeoutMs: 15_000);
            const string marker = "global-packages:";
            var pathFromCli = output
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                .Select(line => line[(line.IndexOf(':') + 1)..].Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(pathFromCli))
            {
                return new NugetPackagesRootResolution(
                    NormalizePath(pathFromCli),
                    "dotnet-nuget-locals");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning("Failed to resolve NuGet root via dotnet CLI: {Message}", ex.Message);
        }

        return new NugetPackagesRootResolution(
            NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages")),
            "user-profile-default");
    }

    static string NormalizePath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return Path.GetFullPath(expanded);
    }

    static string ClassifyNugetSmokeFailure(string message)
    {
        var transientMarkers = new[]
        {
            "XARLP7000",
            "Xamarin.Tools.Zip.ZipException",
            "being used by another process",
            "The process cannot access the file",
            "An existing connection was forcibly closed",
            "Unable to load the service index",
            "The SSL connection could not be established",
            "timed out"
        };

        return transientMarkers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase))
            ? "transient"
            : "deterministic";
    }

    void RunNugetSmokeWithRetry(AbsolutePath project, IList<NugetSmokeRetryTelemetry> retryTelemetry, int maxAttempts)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var output = RunProcessCaptureAllChecked(
                    "dotnet",
                    $"run --project \"{project}\" " +
                    $"--configuration {Configuration} --no-restore --no-build " +
                    $"-- --smoke-test",
                    workingDirectory: RootDirectory,
                    timeoutMs: 60_000);

                // Guardrail: this Chromium teardown error indicates WebView2 lifecycle issues.
                // It does not fail the smoke test by itself, so we explicitly fail the lane to prevent regressions.
                if (output.Contains("Failed to unregister class Chrome_WidgetWin_0", StringComparison.Ordinal) ||
                    output.Contains("ui\\gfx\\win\\window_impl.cc:124", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "NuGet smoke produced a Chromium teardown error: " +
                        "Failed to unregister class Chrome_WidgetWin_0 (window_impl.cc:124).");
                }

                retryTelemetry.Add(new NugetSmokeRetryTelemetry(
                    Attempt: attempt,
                    Classification: attempt == 1 ? "none" : "transient",
                    Outcome: "success",
                    Message: null));
                return;
            }
            catch (Exception ex)
            {
                var message = ex.ToString();
                var classification = ClassifyNugetSmokeFailure(message);
                var isFinalAttempt = attempt >= maxAttempts || string.Equals(classification, "deterministic", StringComparison.Ordinal);
                var outcome = isFinalAttempt ? "failed" : "retrying";

                retryTelemetry.Add(new NugetSmokeRetryTelemetry(
                    Attempt: attempt,
                    Classification: classification,
                    Outcome: outcome,
                    Message: ex.Message));

                if (isFinalAttempt)
                {
                    throw;
                }

                var delayMs = 1000 * attempt;
                Serilog.Log.Warning(
                    "NuGet smoke attempt {Attempt}/{MaxAttempts} failed ({Classification}). Retrying after {DelayMs}ms...",
                    attempt,
                    maxAttempts,
                    classification,
                    delayMs);
                Thread.Sleep(delayMs);
            }
        }
    }
}
