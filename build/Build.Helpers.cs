using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

    IEnumerable<AbsolutePath> GetProjectsToBuild()
    {
        // Core libs (always built)
        yield return SrcDirectory / "Agibuild.Fulora.Core" / "Agibuild.Fulora.Core.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Abstractions" / "Agibuild.Fulora.Adapters.Abstractions.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Runtime" / "Agibuild.Fulora.Runtime.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.DependencyInjection" / "Agibuild.Fulora.DependencyInjection.csproj";

        // Platform adapters (always built — stub adapters compile on all platforms)
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Windows" / "Agibuild.Fulora.Adapters.Windows.csproj";
        yield return SrcDirectory / "Agibuild.Fulora.Adapters.Gtk" / "Agibuild.Fulora.Adapters.Gtk.csproj";

        // macOS adapter (native shim requires macOS host)
        if (OperatingSystem.IsMacOS())
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.MacOS" / "Agibuild.Fulora.Adapters.MacOS.csproj";
        }

        // Android adapter (requires Android workload)
        if (HasDotNetWorkload("android"))
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.Android" / "Agibuild.Fulora.Adapters.Android.csproj";
        }
        else
        {
            Serilog.Log.Warning("Android workload not detected — skipping Android adapter build.");
        }

        // iOS adapter (requires macOS host + iOS workload)
        if (OperatingSystem.IsMacOS() && HasDotNetWorkload("ios"))
        {
            yield return SrcDirectory / "Agibuild.Fulora.Adapters.iOS" / "Agibuild.Fulora.Adapters.iOS.csproj";
        }
        else if (OperatingSystem.IsMacOS())
        {
            Serilog.Log.Warning("iOS workload not detected — skipping iOS adapter build.");
        }

        // Main packable project
        yield return SrcDirectory / "Agibuild.Fulora.Avalonia" / "Agibuild.Fulora.Avalonia.csproj";

        // Test projects
        yield return TestsDirectory / "Agibuild.Fulora.Testing" / "Agibuild.Fulora.Testing.csproj";
        yield return TestsDirectory / "Agibuild.Fulora.UnitTests" / "Agibuild.Fulora.UnitTests.csproj";
        yield return IntegrationTestsProject;
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

    static bool HasDotNetWorkload(string platformKeyword)
    {
        try
        {
            var output = RunProcess("dotnet", "workload list", timeoutMs: 30_000);
            // Workload IDs can be 'android', 'maui-android', 'ios', 'maui-ios', etc.
            // Match any installed workload whose ID contains the platform keyword as a component.
            // Example: 'maui-android' matches keyword 'android'; 'ios' matches keyword 'ios'.
            return output.Split('\n')
                .Any(line =>
                {
                    var trimmed = line.TrimStart();
                    // Skip header/separator lines
                    if (trimmed.Length == 0 || trimmed.StartsWith('-') || trimmed.StartsWith("Installed") || trimmed.StartsWith("Workload") || trimmed.StartsWith("Use "))
                        return false;
                    // Extract the workload ID (first whitespace-delimited token)
                    var id = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    // Match exact ID or as a hyphen-delimited component
                    return id.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase)
                        || id.Split('-').Any(part => part.Equals(platformKeyword, StringComparison.OrdinalIgnoreCase));
                });
        }
        catch
        {
            return false;
        }
    }
}
