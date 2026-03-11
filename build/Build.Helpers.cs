using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;

partial class BuildTask
{
    static AbsolutePath? ResolveFirstExistingPath(params AbsolutePath?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    async Task<IReadOnlyList<AbsolutePath>> GetProjectsToBuildAsync()
    {
        var projects = new List<AbsolutePath>
        {
            // Core libs (always built)
            SrcDirectory / "Agibuild.Fulora.Core" / "Agibuild.Fulora.Core.csproj",
            SrcDirectory / "Agibuild.Fulora.Adapters.Abstractions" / "Agibuild.Fulora.Adapters.Abstractions.csproj",
            SrcDirectory / "Agibuild.Fulora.Runtime" / "Agibuild.Fulora.Runtime.csproj",
            SrcDirectory / "Agibuild.Fulora.DependencyInjection" / "Agibuild.Fulora.DependencyInjection.csproj",

            // Platform adapters (always built — stub adapters compile on all platforms)
            SrcDirectory / "Agibuild.Fulora.Adapters.Windows" / "Agibuild.Fulora.Adapters.Windows.csproj",
            SrcDirectory / "Agibuild.Fulora.Adapters.Gtk" / "Agibuild.Fulora.Adapters.Gtk.csproj",
        };

        // macOS adapter (native shim requires macOS host)
        if (OperatingSystem.IsMacOS())
        {
            projects.Add(SrcDirectory / "Agibuild.Fulora.Adapters.MacOS" / "Agibuild.Fulora.Adapters.MacOS.csproj");
        }

        // Android adapter (requires workload + Android SDK)
        if (await HasDotNetWorkloadAsync("android") && HasAndroidSdkInstalled())
        {
            projects.Add(SrcDirectory / "Agibuild.Fulora.Adapters.Android" / "Agibuild.Fulora.Adapters.Android.csproj");
        }
        else
        {
            Serilog.Log.Warning("Android workload or SDK not detected — skipping Android adapter build.");
        }

        // iOS adapter (requires macOS host + workload + Xcode iOS SDK)
        if (OperatingSystem.IsMacOS() && await HasDotNetWorkloadAsync("ios") && await HasAppleIosSdkInstalledAsync())
        {
            projects.Add(SrcDirectory / "Agibuild.Fulora.Adapters.iOS" / "Agibuild.Fulora.Adapters.iOS.csproj");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Serilog.Log.Warning("iOS workload or SDK not detected — skipping iOS adapter build.");
        }

        // Main packable project
        projects.Add(SrcDirectory / "Agibuild.Fulora.Avalonia" / "Agibuild.Fulora.Avalonia.csproj");

        // Test projects
        projects.Add(TestsDirectory / "Agibuild.Fulora.Testing" / "Agibuild.Fulora.Testing.csproj");
        projects.Add(TestsDirectory / "Agibuild.Fulora.UnitTests" / "Agibuild.Fulora.UnitTests.csproj");
        projects.Add(IntegrationTestsProject);

        return projects;
    }

    string ResolvePackedAgibuildVersion(string packageId)
    {
        var versionPattern = new Regex(
            $"^{Regex.Escape(packageId)}\\.(?<v>\\d+\\.\\d+\\.\\d+(?:-[0-9A-Za-z\\.]+)?(?:\\+[0-9A-Za-z\\.]+)?)\\.nupkg$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        var packages = PackageOutputDirectory
            .GlobFiles("*.nupkg")
            .Where(p => !p.Name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
            .Select(p => new FileInfo(p))
            .Select(p => new { File = p, Match = versionPattern.Match(p.Name) })
            .Where(x => x.Match.Success)
            .OrderByDescending(x => x.File.LastWriteTimeUtc)
            .ToList();

        Assert.NotEmpty(packages, $"No packed nupkg found for {packageId} in {PackageOutputDirectory}.");

        var chosen = packages.First();
        Serilog.Log.Information("Using packed nupkg: {File}", chosen.File.Name);
        return chosen.Match.Groups["v"].Value;
    }

    static async Task<bool> HasDotNetWorkloadAsync(string platformKeyword)
    {
        try
        {
            var output = await RunProcessAsync("dotnet", ["workload", "list"], timeout: TimeSpan.FromSeconds(30));
            return output.Split('\n')
                .Any(line =>
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.Length == 0 || trimmed.StartsWith('-') || trimmed.StartsWith("Installed") || trimmed.StartsWith("Workload") || trimmed.StartsWith("Use "))
                        return false;
                    var id = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    return id.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase)
                        || id.Split('-').Any(part => part.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase));
                });
        }
        catch
        {
            return false;
        }
    }

    bool HasAndroidSdkInstalled()
    {
        if (string.IsNullOrWhiteSpace(AndroidSdkRoot))
            return false;

        var sdkRoot = (AbsolutePath)AndroidSdkRoot;
        var adbPath = sdkRoot / "platform-tools" / (OperatingSystem.IsWindows() ? "adb.exe" : "adb");
        return Directory.Exists(sdkRoot) && File.Exists(adbPath);
    }

    static async Task<bool> HasAppleIosSdkInstalledAsync()
    {
        try
        {
            await RunProcessCheckedAsync("xcrun", ["--sdk", "iphoneos", "--show-sdk-path"], timeout: TimeSpan.FromSeconds(10));
            await RunProcessCheckedAsync("xcodebuild", ["-version"], timeout: TimeSpan.FromSeconds(10));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
