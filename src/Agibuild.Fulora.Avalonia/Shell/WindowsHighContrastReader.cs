using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Detects Windows high-contrast mode via SystemParametersInfo.
/// </summary>
internal static class WindowsHighContrastReader
{
    public static bool IsEnabled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        return CheckHighContrast();
    }

    [SupportedOSPlatform("windows")]
    private static bool CheckHighContrast()
    {
        try
        {
            var hc = new HIGHCONTRAST { cbSize = (uint)Marshal.SizeOf<HIGHCONTRAST>() };
            if (SystemParametersInfo(0x0042 /* SPI_GETHIGHCONTRAST */, hc.cbSize, ref hc, 0))
                return (hc.dwFlags & 0x00000001 /* HCF_HIGHCONTRASTON */) != 0;
        }
        catch
        {
            // P/Invoke may fail in sandboxed environments
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref HIGHCONTRAST pvParam, uint fWinIni);

    [StructLayout(LayoutKind.Sequential)]
    [SupportedOSPlatform("windows")]
    private struct HIGHCONTRAST
    {
        public uint cbSize;
        public uint dwFlags;
        public nint lpszDefaultScheme;
    }
}
