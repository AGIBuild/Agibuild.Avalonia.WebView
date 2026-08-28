using System.Runtime.InteropServices;

namespace Agibuild.Fulora.Platforms;

internal static class AgibuildWebViewGtkNativeLibrary
{
    internal const string FileName = "libAgibuildWebViewGtk.so";
    internal const string LogicalName = "AgibuildWebViewGtk";

    internal static string[] GetCandidatePaths(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        return
        [
            Path.Combine(baseDirectory, "runtimes", "linux-x64", "native", FileName),
            Path.Combine(baseDirectory, FileName),
        ];
    }

    internal static string? ResolveExistingPath(string? baseDirectory = null)
    {
        foreach (var candidate in GetCandidatePaths(baseDirectory ?? AppContext.BaseDirectory))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    internal static IntPtr LoadResolvedOrZero()
    {
        var path = ResolveExistingPath();
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }
}
