using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Reads the macOS accent color from system defaults.
/// Uses <c>defaults read -g AppleAccentColor</c> which maps to known macOS accent colors.
/// </summary>
internal static class MacOSAccentColorReader
{
    // macOS accent color index → approximate hex color
    private static readonly string[] KnownAccentColors =
    [
        "#FF5257", // Red (0)
        "#F7821B", // Orange (1)
        "#FFC600", // Yellow (2)
        "#62BA46", // Green (3)
        "#007AFF", // Blue (4, also default = -1 or missing)
        "#A550A7", // Purple (5)
        "#F74F9E", // Pink (6)
        "#8C8C8C"  // Graphite (7, actually -2)
    ];

    public static string? Read()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return null;

        try
        {
            var psi = new ProcessStartInfo("defaults", "read -g AppleAccentColor")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) return DefaultBlue();

            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(1000);

            if (proc.ExitCode != 0)
                return DefaultBlue();

            if (int.TryParse(output, out var index))
            {
                // -1 or missing → blue (default)
                // -2 → graphite
                if (index == -1) return KnownAccentColors[4];
                if (index == -2) return KnownAccentColors[7];
                if (index >= 0 && index < KnownAccentColors.Length)
                    return KnownAccentColors[index];
            }

            return DefaultBlue();
        }
        catch
        {
            return null;
        }
    }

    private static string DefaultBlue() => KnownAccentColors[4];
}
