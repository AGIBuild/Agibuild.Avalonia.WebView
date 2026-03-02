using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Reads the Windows accent color from the registry (DWM ColorizationColor).
/// </summary>
internal static class WindowsAccentColorReader
{
    public static string? Read()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        return ReadFromRegistry();
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accentInt)
            {
                // DWM stores as ABGR (alpha-blue-green-red)
                var a = (byte)((accentInt >> 24) & 0xFF);
                var b = (byte)((accentInt >> 16) & 0xFF);
                var g = (byte)((accentInt >> 8) & 0xFF);
                var r = (byte)(accentInt & 0xFF);
                return $"#{r:X2}{g:X2}{b:X2}";
            }

            if (key?.GetValue("ColorizationColor") is int colorInt)
            {
                var r = (byte)((colorInt >> 16) & 0xFF);
                var g = (byte)((colorInt >> 8) & 0xFF);
                var b2 = (byte)(colorInt & 0xFF);
                return $"#{r:X2}{g:X2}{b2:X2}";
            }
        }
        catch
        {
            // Registry access may fail in sandboxed environments
        }

        return null;
    }
}
