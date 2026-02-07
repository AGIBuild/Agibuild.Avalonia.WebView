using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "ci",
    GitHubActionsImage.MacOsLatest,
    On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest],
    InvokedTargets = [nameof(Ci)],
    AutoGenerate = false)]
class _Build : NukeBuild
{
    public static int Main() => Execute<_Build>(x => x.Build);

    // ──────────────────────────────── Parameters ────────────────────────────────

    [Parameter("Configuration (Debug / Release). Default: Release on CI, Debug locally.")]
    readonly string Configuration = IsServerBuild ? "Release" : "Debug";

    [Parameter("NuGet package version override. When set, overrides MinVer auto-calculated version.")]
    readonly string? PackageVersion = null;

    [Parameter("NuGet source URL for publish. Default: https://api.nuget.org/v3/index.json")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter("NuGet API key for publish.")]
    [Secret]
    readonly string? NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");

    [Parameter("Minimum line coverage percentage (0-100). Default: 90")]
    readonly int CoverageThreshold = 90;

    [Parameter("Android AVD name for emulator. Default: auto-detect first available AVD.")]
    readonly string? AndroidAvd = null;

    [Parameter("iOS Simulator device name. Default: auto-detect first available iPhone simulator.")]
    readonly string? iOSSimulator = null;

    [Parameter("Android SDK root path. Default: ~/Library/Android/sdk (macOS) or ANDROID_HOME env var.")]
    readonly string AndroidSdkRoot = ResolveAndroidSdkRoot();

    static string ResolveAndroidSdkRoot()
    {
        // Try ANDROID_HOME first, then ANDROID_SDK_ROOT (both commonly used).
        // Must check for non-empty because Nuke resolves empty env-var strings as valid values.
        var home = Environment.GetEnvironmentVariable("ANDROID_HOME");
        if (!string.IsNullOrEmpty(home) && Directory.Exists(home))
            return home;

        var sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot))
            return sdkRoot;

        // Fallback: default macOS Android Studio install location
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk");
    }

    // ──────────────────────────────── Paths ──────────────────────────────────────

    AbsolutePath SrcDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackageOutputDirectory => ArtifactsDirectory / "packages";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

    AbsolutePath SolutionFile => RootDirectory / "Agibuild.Avalonia.WebView.sln";
    AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";
    AbsolutePath CoverageReportDirectory => ArtifactsDirectory / "coverage-report";

    // Pack project
    AbsolutePath PackProject =>
        SrcDirectory / "Agibuild.Avalonia.WebView" / "Agibuild.Avalonia.WebView.csproj";

    // Test projects
    AbsolutePath UnitTestsProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.UnitTests" / "Agibuild.Avalonia.WebView.UnitTests.csproj";

    AbsolutePath IntegrationTestsProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests.Automation"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj";

    AbsolutePath E2EDesktopProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Desktop"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Desktop.csproj";

    AbsolutePath E2EAndroidProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Android"
        / "Agibuild.Avalonia.WebView.Integration.Tests.Android.csproj";

    AbsolutePath E2EiOSProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.Tests"
        / "Agibuild.Avalonia.WebView.Integration.Tests.iOS"
        / "Agibuild.Avalonia.WebView.Integration.Tests.iOS.csproj";

    AbsolutePath NugetPackageTestProject =>
        TestsDirectory / "Agibuild.Avalonia.WebView.Integration.NugetPackageTests"
        / "Agibuild.Avalonia.WebView.Integration.NugetPackageTests.csproj";

    // ──────────────────────────────── Targets ────────────────────────────────────

    Target Clean => _ => _
        .Description("Cleans bin/obj directories and the artifacts folder.")
        .Executes(() =>
        {
            SrcDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Description("Restores NuGet packages for the solution.")
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(SolutionFile));
        });

    Target Build => _ => _
        .Description("Builds all platform-appropriate projects.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            foreach (var project in GetProjectsToBuild())
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }
        });

    Target UnitTests => _ => _
        .Description("Runs unit tests.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(UnitTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=unit-tests.trx"));
        });

    Target Coverage => _ => _
        .Description("Runs unit tests with code coverage and enforces minimum threshold.")
        .DependsOn(Build)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            // Run tests with coverlet data collector.
            DotNetTest(s => s
                .SetProjectFile(UnitTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(CoverageDirectory)
                .SetLoggers("trx;LogFileName=unit-tests.trx")
                .SetSettingsFile(RootDirectory / "coverlet.runsettings"));

            // Find the generated cobertura XML.
            var coverageFiles = CoverageDirectory.GlobFiles("**/coverage.cobertura.xml");
            if (!coverageFiles.Any())
            {
                Assert.Fail("No coverage files found. Ensure coverlet.collector is referenced in the test project.");
            }

            var coverageFile = coverageFiles.First();

            // Generate HTML report + enforce threshold via ReportGenerator.
            DotNet(
                $"reportgenerator " +
                $"\"-reports:{coverageFile}\" " +
                $"\"-targetdir:{CoverageReportDirectory}\" " +
                $"\"-reporttypes:Html;Cobertura;TextSummary\" " +
                $"\"-assemblyfilters:+Agibuild.Avalonia.WebView.*;-Agibuild.Avalonia.WebView.Testing;-Agibuild.Avalonia.WebView.UnitTests\"",
                workingDirectory: RootDirectory);

            // Parse the Cobertura XML to extract line coverage and validate threshold.
            var mergedCoberturaFile = CoverageReportDirectory / "Cobertura.xml";
            var coberturaPath = (string)(File.Exists(mergedCoberturaFile) ? mergedCoberturaFile : coverageFile);
            var doc = XDocument.Load(coberturaPath);
            var lineRateAttr = doc.Root?.Attribute("line-rate")?.Value;

            if (lineRateAttr is null || !double.TryParse(lineRateAttr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lineRate))
            {
                Assert.Fail("Unable to parse line-rate from coverage report.");
                return; // unreachable, but keeps compiler happy
            }

            var lineCoveragePct = lineRate * 100;
            Serilog.Log.Information("Line coverage: {Coverage:F2}% (threshold: {Threshold}%)", lineCoveragePct, CoverageThreshold);
            Serilog.Log.Information("HTML report: {Path}", CoverageReportDirectory / "index.html");

            if (lineCoveragePct < CoverageThreshold)
            {
                Assert.Fail(
                    $"Line coverage {lineCoveragePct:F2}% is below the required threshold of {CoverageThreshold}%. " +
                    $"Review the report at {CoverageReportDirectory / "index.html"}");
            }

            Serilog.Log.Information("Coverage gate PASSED: {Coverage:F2}% >= {Threshold}%", lineCoveragePct, CoverageThreshold);

            // Write coverage summary to GitHub Actions Job Summary when running on CI.
            var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrEmpty(summaryPath))
            {
                var textSummaryFile = CoverageReportDirectory / "Summary.txt";
                var summaryContent = File.Exists(textSummaryFile)
                    ? File.ReadAllText(textSummaryFile)
                    : $"Line coverage: {lineCoveragePct:F2}%";

                var markdown =
                    $"## Code Coverage Report\n\n" +
                    $"| Metric | Value |\n" +
                    $"|--------|-------|\n" +
                    $"| **Line Coverage** | **{lineCoveragePct:F2}%** |\n" +
                    $"| Threshold | {CoverageThreshold}% |\n" +
                    $"| Status | {(lineCoveragePct >= CoverageThreshold ? "PASSED" : "FAILED")} |\n\n" +
                    $"<details><summary>Full Summary</summary>\n\n```\n{summaryContent}\n```\n\n</details>\n";

                File.AppendAllText(summaryPath, markdown);
            }
        });

    Target IntegrationTests => _ => _
        .Description("Runs automated integration tests.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(IntegrationTestsProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=integration-tests.trx"));
        });

    Target Test => _ => _
        .Description("Runs all tests (unit + integration).")
        .DependsOn(UnitTests, IntegrationTests);

    Target Pack => _ => _
        .Description("Creates the NuGet package (.nupkg).")
        .DependsOn(Build)
        .Produces(PackageOutputDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackageOutputDirectory.CreateOrCleanDirectory();

            DotNetPack(s =>
            {
                var settings = s
                    .SetProject(PackProject)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetOutputDirectory(PackageOutputDirectory);

                if (!string.IsNullOrEmpty(PackageVersion))
                    settings = settings.SetProperty("MinVerVersionOverride", PackageVersion);

                return settings;
            });
        });

    Target ValidatePackage => _ => _
        .Description("Validates the NuGet package contains all expected assemblies and files.")
        .DependsOn(Pack)
        .Executes(() =>
        {
            var nupkgFiles = PackageOutputDirectory.GlobFiles("*.nupkg")
                .Where(f => !f.Name.EndsWith(".symbols.nupkg"))
                .ToList();

            Assert.NotEmpty(nupkgFiles, "No .nupkg files found in output directory.");
            var nupkgPath = nupkgFiles.First();
            Serilog.Log.Information("Validating package: {Package}", nupkgPath.Name);

            using var archive = ZipFile.OpenRead(nupkgPath);
            var entries = archive.Entries
                .Select(e => e.FullName.Replace('\\', '/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ── Required assemblies (always present) ──────────────────────────────
            var requiredFiles = new Dictionary<string, string>
            {
                // Core lib assemblies
                ["lib/net10.0/Agibuild.Avalonia.WebView.dll"] = "Main assembly",
                ["lib/net10.0/Agibuild.Avalonia.WebView.Core.dll"] = "Core contracts",
                ["lib/net10.0/Agibuild.Avalonia.WebView.Adapters.Abstractions.dll"] = "Adapter abstractions",
                ["lib/net10.0/Agibuild.Avalonia.WebView.Runtime.dll"] = "Runtime host",
                ["lib/net10.0/Agibuild.Avalonia.WebView.DependencyInjection.dll"] = "DI extensions",

                // Platform adapters (all in lib/net10.0/ to avoid runtimeTargets replacing runtime assets)
                ["lib/net10.0/Agibuild.Avalonia.WebView.Adapters.Windows.dll"] = "Windows adapter",
                ["lib/net10.0/Agibuild.Avalonia.WebView.Adapters.Gtk.dll"] = "Linux GTK adapter",

                // Non-assembly required files
                ["buildTransitive/Agibuild.Avalonia.WebView.targets"] = "MSBuild targets",
                ["LICENSE.txt"] = "License file",
                ["README.md"] = "Package readme",
            };

            // ── Conditionally expected assemblies ─────────────────────────────────
            // Android adapter DLL existence check (requires Android workload to build)
            var androidAdapterPath = SrcDirectory
                / "Agibuild.Avalonia.WebView.Adapters.Android" / "bin" / Configuration
                / "net10.0-android" / "Agibuild.Avalonia.WebView.Adapters.Android.dll";

            // iOS adapter DLL existence check (requires iOS workload to build)
            var iosAdapterPath = SrcDirectory
                / "Agibuild.Avalonia.WebView.Adapters.iOS" / "bin" / Configuration
                / "net10.0-ios" / "Agibuild.Avalonia.WebView.Adapters.iOS.dll";

            var conditionalFiles = new Dictionary<string, (string Description, bool ShouldExist)>
            {
                ["lib/net10.0/Agibuild.Avalonia.WebView.Adapters.MacOS.dll"] =
                    ("macOS adapter", OperatingSystem.IsMacOS()),
                ["runtimes/osx/native/libAgibuildWebViewWk.dylib"] =
                    ("macOS native shim", OperatingSystem.IsMacOS()),
                ["runtimes/android/lib/net10.0-android36.0/Agibuild.Avalonia.WebView.Adapters.Android.dll"] =
                    ("Android adapter", File.Exists(androidAdapterPath)),
                ["runtimes/ios/lib/net10.0-ios18.0/Agibuild.Avalonia.WebView.Adapters.iOS.dll"] =
                    ("iOS adapter", File.Exists(iosAdapterPath)),
            };

            var errors = new List<string>();

            // Check required files
            foreach (var (path, description) in requiredFiles)
            {
                if (entries.Contains(path))
                {
                    Serilog.Log.Information("  OK: {Path} ({Description})", path, description);
                }
                else
                {
                    errors.Add($"MISSING (required): {path} — {description}");
                    Serilog.Log.Error("  MISSING: {Path} ({Description})", path, description);
                }
            }

            // Check conditional files
            foreach (var (path, (description, shouldExist)) in conditionalFiles)
            {
                if (entries.Contains(path))
                {
                    Serilog.Log.Information("  OK: {Path} ({Description})", path, description);
                }
                else if (shouldExist)
                {
                    errors.Add($"MISSING (expected on this platform): {path} — {description}");
                    Serilog.Log.Error("  MISSING: {Path} ({Description})", path, description);
                }
                else
                {
                    Serilog.Log.Information("  SKIP: {Path} ({Description} — not built on this platform)", path, description);
                }
            }

            // Check for unexpected Agibuild DLLs (detect accidental inclusions)
            var knownDlls = requiredFiles.Keys
                .Concat(conditionalFiles.Keys)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unexpectedDlls = entries
                .Where(e => e.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                         && e.Contains("Agibuild.", StringComparison.OrdinalIgnoreCase)
                         && !knownDlls.Contains(e))
                .ToList();

            foreach (var dll in unexpectedDlls)
            {
                errors.Add($"UNEXPECTED: {dll} — not in the expected manifest");
                Serilog.Log.Warning("  UNEXPECTED: {Path}", dll);
            }

            // Validate nuspec metadata
            var nuspecEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry is not null)
            {
                using var stream = nuspecEntry.Open();
                var nuspecDoc = XDocument.Load(stream);
                var ns = nuspecDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                var metadata = nuspecDoc.Root?.Element(ns + "metadata");

                var id = metadata?.Element(ns + "id")?.Value;
                var version = metadata?.Element(ns + "version")?.Value;
                var description = metadata?.Element(ns + "description")?.Value;

                if (string.IsNullOrEmpty(id))
                    errors.Add("NUSPEC: Missing <id>");
                if (string.IsNullOrEmpty(version) || version == "0.0.0-dev" || version == "1.0.0")
                    errors.Add($"NUSPEC: Invalid <version>: '{version}' — MinVer may not have run correctly");
                if (string.IsNullOrEmpty(description))
                    errors.Add("NUSPEC: Missing <description>");

                Serilog.Log.Information("  Nuspec: id={Id}, version={Version}", id, version);
            }
            else
            {
                errors.Add("NUSPEC: No .nuspec file found in package");
            }

            // Summary
            Serilog.Log.Information("Package validation: {Total} entries, {Errors} error(s)",
                entries.Count, errors.Count);

            if (errors.Count > 0)
            {
                Assert.Fail(
                    "Package validation failed:\n" +
                    string.Join("\n", errors.Select(e => $"  - {e}")));
            }

            Serilog.Log.Information("Package validation PASSED.");
        });

    Target Publish => _ => _
        .Description("Pushes the NuGet package to the configured source.")
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            var packages = PackageOutputDirectory.GlobFiles("*.nupkg")
                .Where(p => !p.Name.EndsWith(".symbols.nupkg"));

            foreach (var package in packages)
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .EnableSkipDuplicate());
            }
        });

    Target Start => _ => _
        .Description("Launches the E2E integration test desktop app.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(E2EDesktopProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target StartAndroid => _ => _
        .Description("Starts an Android emulator, builds the Android IT test app, and installs it.")
        .Executes(() =>
        {
            var emulatorPath = Path.Combine(AndroidSdkRoot, "emulator", "emulator");
            var adbPath = Path.Combine(AndroidSdkRoot, "platform-tools", "adb");

            Assert.FileExists(emulatorPath, $"Android emulator not found at {emulatorPath}. Set --android-sdk-root.");
            Assert.FileExists(adbPath, $"adb not found at {adbPath}. Set --android-sdk-root.");

            // 1. Resolve AVD name
            var avdName = AndroidAvd;
            if (string.IsNullOrEmpty(avdName))
            {
                Serilog.Log.Information("No --android-avd specified, detecting available AVDs...");
                var listResult = RunProcess(emulatorPath, "-list-avds");
                var avds = listResult
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(l => !l.StartsWith("INFO", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Assert.NotEmpty(avds, "No Android AVDs found. Create one via Android Studio or `avdmanager`.");
                avdName = avds.First();
                Serilog.Log.Information("Auto-selected AVD: {Avd}", avdName);
            }

            // 2. Check if emulator is already running
            var devicesOutput = RunProcess(adbPath, "devices");
            var hasRunningEmulator = devicesOutput
                .Split('\n')
                .Any(l => l.StartsWith("emulator-", StringComparison.Ordinal) && l.Contains("device"));

            if (hasRunningEmulator)
            {
                Serilog.Log.Information("Android emulator is already running, skipping launch.");
            }
            else
            {
                // 3. Start emulator in background
                Serilog.Log.Information("Starting Android emulator: {Avd}...", avdName);
                var emulatorProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = emulatorPath,
                        Arguments = $"-avd {avdName} -no-snapshot-load -no-audio",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };
                emulatorProcess.Start();

                // 4. Wait for device to boot
                Serilog.Log.Information("Waiting for emulator to boot...");
                var timeout = TimeSpan.FromMinutes(3);
                var stopwatch = Stopwatch.StartNew();
                var booted = false;

                while (stopwatch.Elapsed < timeout)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        var bootResult = RunProcess(adbPath, "shell getprop sys.boot_completed");
                        if (bootResult.Trim() == "1")
                        {
                            booted = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Device not ready yet
                    }
                }

                Assert.True(booted, $"Emulator did not boot within {timeout.TotalMinutes} minutes.");
                Serilog.Log.Information("Emulator booted successfully ({Elapsed:F0}s).", stopwatch.Elapsed.TotalSeconds);
            }

            // 5. Build and install the Android test app
            //    Use -t:Install so .NET SDK handles Fast Deployment correctly
            //    (assemblies are deployed to .__override__ on device, not embedded in APK in Debug).
            Serilog.Log.Information("Building and installing Android test app...");
            RunProcess("dotnet", $"build \"{E2EAndroidProject}\" --configuration {Configuration} -t:Install");

            // 6. Launch the app
            const string packageName = "com.CompanyName.Agibuild.Avalonia.WebView.Integration.Tests";
            Serilog.Log.Information("Launching {Package}...", packageName);
            RunProcess(adbPath, $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");

            Serilog.Log.Information("Android test app deployed and launched successfully.");
        });

    Target StartIOS => _ => _
        .Description("Builds the iOS IT test app, deploys it to an iOS Simulator, and launches it.")
        .Executes(() =>
        {
            if (!OperatingSystem.IsMacOS())
            {
                Assert.Fail("StartIOS requires macOS with Xcode installed.");
            }

            // 1. Resolve simulator device
            var deviceName = iOSSimulator;
            string deviceUdid;

            if (string.IsNullOrEmpty(deviceName))
            {
                Serilog.Log.Information("No --i-o-s-simulator specified, detecting available simulators...");
                var listJson = RunProcess("xcrun", "simctl list devices available --json", timeoutMs: 15_000);

                // Parse JSON to find first iPhone simulator.
                // The JSON has structure: { "devices": { "com.apple.CoreSimulator.SimRuntime.iOS-XX-X": [ { "name": "...", "udid": "...", "state": "..." } ] } }
                var jsonDoc = System.Text.Json.JsonDocument.Parse(listJson);
                var devicesObj = jsonDoc.RootElement.GetProperty("devices");

                string? foundUdid = null;
                string? foundName = null;
                string? foundRuntime = null;

                foreach (var runtime in devicesObj.EnumerateObject())
                {
                    // Only consider iOS runtimes
                    if (!runtime.Name.Contains("iOS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var device in runtime.Value.EnumerateArray())
                    {
                        var name = device.GetProperty("name").GetString() ?? "";
                        var udid = device.GetProperty("udid").GetString() ?? "";
                        var isAvailable = device.TryGetProperty("isAvailable", out var avail) && avail.GetBoolean();

                        if (!isAvailable) continue;

                        // Prefer iPhone devices
                        if (name.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                        {
                            foundUdid = udid;
                            foundName = name;
                            foundRuntime = runtime.Name;
                            // Keep searching to pick the latest runtime (they're typically ordered)
                        }
                    }
                }

                if (foundUdid is null)
                {
                    Assert.Fail("No available iPhone simulator found. Create one in Xcode > Settings > Platforms.");
                    return;
                }

                deviceUdid = foundUdid;
                Serilog.Log.Information("Auto-selected simulator: {Name} ({Udid}) [{Runtime}]", foundName, deviceUdid, foundRuntime);
            }
            else
            {
                // Look up UDID by name
                Serilog.Log.Information("Looking up simulator: {Name}...", deviceName);
                var listJson = RunProcess("xcrun", "simctl list devices available --json", timeoutMs: 15_000);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(listJson);
                var devicesObj = jsonDoc.RootElement.GetProperty("devices");

                string? foundUdid = null;
                foreach (var runtime in devicesObj.EnumerateObject())
                {
                    if (!runtime.Name.Contains("iOS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var device in runtime.Value.EnumerateArray())
                    {
                        var name = device.GetProperty("name").GetString() ?? "";
                        var udid = device.GetProperty("udid").GetString() ?? "";
                        var isAvailable = device.TryGetProperty("isAvailable", out var avail) && avail.GetBoolean();

                        if (isAvailable && name.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            foundUdid = udid;
                        }
                    }
                }

                if (foundUdid is null)
                {
                    Assert.Fail($"Simulator '{deviceName}' not found or not available. Check `xcrun simctl list devices available`.");
                    return;
                }

                deviceUdid = foundUdid;
                Serilog.Log.Information("Found simulator: {Name} ({Udid})", deviceName, deviceUdid);
            }

            // 2. Boot the simulator if not already booted
            var deviceState = RunProcess("xcrun", $"simctl list devices --json", timeoutMs: 10_000);
            if (!deviceState.Contains($"\"{deviceUdid}\"") || !deviceState.Contains("\"state\" : \"Booted\""))
            {
                // Check specific device state
                var stateJson = System.Text.Json.JsonDocument.Parse(deviceState);
                var allDevices = stateJson.RootElement.GetProperty("devices");
                var isBooted = false;

                foreach (var runtime in allDevices.EnumerateObject())
                {
                    foreach (var device in runtime.Value.EnumerateArray())
                    {
                        var udid = device.GetProperty("udid").GetString();
                        if (udid == deviceUdid)
                        {
                            var state = device.GetProperty("state").GetString();
                            isBooted = string.Equals(state, "Booted", StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                    if (isBooted) break;
                }

                if (!isBooted)
                {
                    Serilog.Log.Information("Booting simulator {Udid}...", deviceUdid);
                    RunProcess("xcrun", $"simctl boot {deviceUdid}", timeoutMs: 30_000);

                    // Open Simulator.app so the user can see it
                    try { RunProcess("open", "-a Simulator", timeoutMs: 5_000); }
                    catch { /* Simulator.app may already be open */ }

                    // Wait briefly for boot to settle
                    Thread.Sleep(3000);
                    Serilog.Log.Information("Simulator booted.");
                }
                else
                {
                    Serilog.Log.Information("Simulator is already booted.");
                }
            }

            // 3. Build the iOS test app for the simulator
            Serilog.Log.Information("Building iOS test app...");
            DotNetBuild(s => s
                .SetProjectFile(E2EiOSProject)
                .SetConfiguration(Configuration)
                .SetRuntime("iossimulator-arm64"));

            // 4. Find the .app bundle
            var appDir = (AbsolutePath)(Path.GetDirectoryName(E2EiOSProject)!)
                         / "bin" / Configuration / "net10.0-ios" / "iossimulator-arm64";
            var appBundles = appDir.GlobDirectories("*.app").ToList();

            if (!appBundles.Any())
            {
                // Try alternative output path
                appDir = (AbsolutePath)(Path.GetDirectoryName(E2EiOSProject)!)
                         / "bin" / Configuration / "net10.0-ios" / "iossimulator-x64";
                appBundles = appDir.GlobDirectories("*.app").ToList();
            }

            Assert.NotEmpty(appBundles, $"No .app bundle found in {appDir}. Build may have failed.");
            var appBundle = appBundles.First();
            Serilog.Log.Information("Found app bundle: {App}", appBundle.Name);

            // 5. Install the app on the simulator
            Serilog.Log.Information("Installing app on simulator...");
            RunProcess("xcrun", $"simctl install {deviceUdid} \"{appBundle}\"", timeoutMs: 120_000);

            // 6. Launch the app (use Process.Start directly to avoid blocking on stdout)
            const string bundleId = "companyName.Agibuild.Avalonia.WebView.Integration.Tests";
            Serilog.Log.Information("Launching {BundleId}...", bundleId);
            var launchProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xcrun",
                    Arguments = $"simctl launch {deviceUdid} {bundleId}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
            launchProcess.Start();

            // Wait for the launch process to finish (it prints the PID on success)
            if (!launchProcess.WaitForExit(15_000))
            {
                // simctl launch sometimes hangs; kill it but the app may still have launched
                try { launchProcess.Kill(); } catch { /* ignore */ }
                Serilog.Log.Warning("simctl launch timed out, but the app may still be running. Check the simulator.");
            }
            else if (launchProcess.ExitCode == 0)
            {
                var launchOutput = launchProcess.StandardOutput.ReadToEnd().Trim();
                Serilog.Log.Information("App launched successfully. {Output}", launchOutput);
            }
            else
            {
                var launchError = launchProcess.StandardError.ReadToEnd().Trim();
                Serilog.Log.Warning("simctl launch exited with code {Code}: {Error}", launchProcess.ExitCode, launchError);
            }

            Serilog.Log.Information("iOS test app deployed and launched on simulator.");
        });

    Target NugetPackageTest => _ => _
        .Description("Packs, restores, builds, and runs the NuGet package integration smoke test end-to-end.")
        .DependsOn(ValidatePackage)
        .Executes(() =>
        {
            // 1. Purge all cached versions so *-* resolves to the freshly packed build
            var cacheBase = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages", "agibuild.avalonia.webview");
            if (Directory.Exists(cacheBase))
            {
                Serilog.Log.Information("Clearing NuGet cache: {Path}", cacheBase);
                Directory.Delete(cacheBase, recursive: true);
            }

            // 2. Clean previous build outputs to avoid stale DLLs
            var testProjectDir = TestsDirectory / "Agibuild.Avalonia.WebView.Integration.NugetPackageTests";
            var testBinDir = testProjectDir / "bin";
            var testObjDir = testProjectDir / "obj";
            if (Directory.Exists(testBinDir)) testBinDir.DeleteDirectory();
            if (Directory.Exists(testObjDir)) testObjDir.DeleteDirectory();

            // 3. Restore — nuget.config in the test project dir points to artifacts/packages
            Serilog.Log.Information("Restoring NuGet package test project...");
            DotNetRestore(s => s
                .SetProjectFile(NugetPackageTestProject));

            // 4. Build
            DotNetBuild(s => s
                .SetProjectFile(NugetPackageTestProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());

            // 5. Run the smoke test (headless — auto-closes after verification)
            Serilog.Log.Information("Running NuGet package smoke test...");
            var resultFile = testProjectDir / "bin" / Configuration / "net10.0" / "smoke-test-result.txt";
            if (File.Exists(resultFile)) File.Delete(resultFile);

            DotNet(
                $"run --project \"{NugetPackageTestProject}\" " +
                $"--configuration {Configuration} --no-restore --no-build " +
                $"-- --smoke-test",
                workingDirectory: RootDirectory,
                timeout: 60_000);

            // 6. Verify the result file
            if (!File.Exists(resultFile))
            {
                Assert.Fail($"Smoke test result file not found at {resultFile}.");
            }

            var result = File.ReadAllText(resultFile).Trim();
            Serilog.Log.Information("Smoke test result: {Result}", result);

            if (!result.StartsWith("PASSED", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"NuGet package smoke test FAILED: {result}");
            }

            Serilog.Log.Information("NuGet package integration test PASSED.");
        });

    Target Ci => _ => _
        .Description("Full CI pipeline: compile → coverage → pack → validate.")
        .DependsOn(Coverage, ValidatePackage);

    Target CiPublish => _ => _
        .Description("Full CI/CD pipeline: compile → coverage → pack → validate → publish.")
        .DependsOn(Coverage, ValidatePackage, Publish);

    // ──────────────────────────────── Helpers ────────────────────────────────────

    static string RunProcess(string fileName, string arguments, int timeoutMs = 30_000)
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

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeoutMs}ms.");
        }

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            Serilog.Log.Warning("Process stderr: {Error}", error.Trim());
        }

        return output;
    }

    IEnumerable<AbsolutePath> GetProjectsToBuild()
    {
        // Core libs (always built)
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Core" / "Agibuild.Avalonia.WebView.Core.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Abstractions" / "Agibuild.Avalonia.WebView.Adapters.Abstractions.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Runtime" / "Agibuild.Avalonia.WebView.Runtime.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.DependencyInjection" / "Agibuild.Avalonia.WebView.DependencyInjection.csproj";

        // Platform adapters (always built — stub adapters compile on all platforms)
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Windows" / "Agibuild.Avalonia.WebView.Adapters.Windows.csproj";
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Gtk" / "Agibuild.Avalonia.WebView.Adapters.Gtk.csproj";

        // macOS adapter (native shim requires macOS host)
        if (OperatingSystem.IsMacOS())
        {
            yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.MacOS" / "Agibuild.Avalonia.WebView.Adapters.MacOS.csproj";
        }

        // Android adapter (requires Android workload — build failure is non-fatal for Pack)
        yield return SrcDirectory / "Agibuild.Avalonia.WebView.Adapters.Android" / "Agibuild.Avalonia.WebView.Adapters.Android.csproj";

        // Main packable project
        yield return SrcDirectory / "Agibuild.Avalonia.WebView" / "Agibuild.Avalonia.WebView.csproj";

        // Test projects
        yield return TestsDirectory / "Agibuild.Avalonia.WebView.Testing" / "Agibuild.Avalonia.WebView.Testing.csproj";
        yield return TestsDirectory / "Agibuild.Avalonia.WebView.UnitTests" / "Agibuild.Avalonia.WebView.UnitTests.csproj";
    }
}
