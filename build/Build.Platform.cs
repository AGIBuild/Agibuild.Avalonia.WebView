using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class BuildTask
{
    Target Start => _ => _
        .Description("Launches the E2E integration test desktop app.")
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(E2EDesktopProject));

            DotNetRun(s => s
                .SetProjectFile(E2EDesktopProject)
                .SetConfiguration(Configuration));
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
                // 3. Start emulator — UseShellExecute=true so the GUI window appears in foreground on macOS
                Serilog.Log.Information("Starting Android emulator: {Avd}...", avdName);
                var emulatorProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = emulatorPath,
                        Arguments = $"-avd {avdName} -no-snapshot-load -no-audio",
                        UseShellExecute = true,
                    }
                };
                emulatorProcess.Start();
            }

            // 4. Wait for device to fully boot (always, even if emulator was already running)
            WaitForAndroidBoot(adbPath);

            // 5. Build and install the Android test app
            //    Use -t:Install so .NET SDK handles Fast Deployment correctly
            //    (assemblies are deployed to .__override__ on device, not embedded in APK in Debug).
            Serilog.Log.Information("Building and installing Android test app...");
            RunProcess("dotnet", $"build \"{E2EAndroidProject}\" --configuration {Configuration} -t:Install");

            // 6. Launch the app (with retry to handle activity manager startup delay)
            const string packageName = "com.CompanyName.Agibuild.Fulora.Integration.Tests";
            Serilog.Log.Information("Launching {Package}...", packageName);
            LaunchAndroidApp(adbPath, packageName);

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

                var jsonDoc = JsonDocument.Parse(listJson);
                var devicesObj = jsonDoc.RootElement.GetProperty("devices");

                string? foundUdid = null;
                string? foundName = null;
                string? foundRuntime = null;

                foreach (var runtime in devicesObj.EnumerateObject())
                {
                    if (!runtime.Name.Contains("iOS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var device in runtime.Value.EnumerateArray())
                    {
                        var name = device.GetProperty("name").GetString() ?? "";
                        var udid = device.GetProperty("udid").GetString() ?? "";
                        var isAvailable = device.TryGetProperty("isAvailable", out var avail) && avail.GetBoolean();

                        if (!isAvailable) continue;

                        if (name.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                        {
                            foundUdid = udid;
                            foundName = name;
                            foundRuntime = runtime.Name;
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
                Serilog.Log.Information("Looking up simulator: {Name}...", deviceName);
                var listJson = RunProcess("xcrun", "simctl list devices available --json", timeoutMs: 15_000);
                var jsonDoc = JsonDocument.Parse(listJson);
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
                var stateJson = JsonDocument.Parse(deviceState);
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

                    try { RunProcess("open", "-a Simulator", timeoutMs: 5_000); }
                    catch { /* Simulator.app may already be open */ }

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

            // 6. Launch the app
            const string bundleId = "companyName.Agibuild.Fulora.Integration.Tests";
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

            if (!launchProcess.WaitForExit(15_000))
            {
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

    static void WaitForAndroidBoot(string adbPath, int timeoutMinutes = 3)
    {
        Serilog.Log.Information("Waiting for emulator to boot...");
        var timeout = TimeSpan.FromMinutes(timeoutMinutes);
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

        Assert.True(booted, $"Emulator did not boot within {timeoutMinutes} minutes.");
        Serilog.Log.Information("Emulator booted successfully ({Elapsed:F0}s).", stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Launch an Android app via monkey, retrying until the activity manager is available.
    /// After sys.boot_completed=1 there is a short window where system services are still initializing.
    /// </summary>
    static void LaunchAndroidApp(string adbPath, string packageName, int maxRetries = 5)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var output = RunProcess(adbPath,
                    $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");

                // monkey writes "Events injected: 1" to stdout on success
                if (output.Contains("Events injected", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Serilog.Log.Warning("monkey attempt {Attempt}/{Max}: unexpected output: {Output}",
                    attempt, maxRetries, output.Trim());
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("monkey attempt {Attempt}/{Max} failed: {Message}",
                    attempt, maxRetries, ex.Message);
            }

            if (attempt < maxRetries)
            {
                Serilog.Log.Information("Waiting 3s before retry...");
                Thread.Sleep(3000);
            }
        }

        throw new InvalidOperationException(
            $"Failed to launch {packageName} after {maxRetries} attempts. " +
            "The activity manager may not be available — is the emulator fully booted?");
    }
}
