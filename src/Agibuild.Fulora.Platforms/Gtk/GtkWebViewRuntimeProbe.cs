using System.Runtime.InteropServices;
using Agibuild.Fulora.Adapters.Abstractions;

namespace Agibuild.Fulora.Platforms;

internal static class GtkWebViewRuntimeProbe
{
    private const string ShimUnavailableReason = "The WebKitGTK native shim could not be discovered.";
    private const string RuntimeUnavailableReason = "The WebKitGTK runtime could not be discovered.";

    public static WebViewPlatformProbeResult Probe()
        => Probe(
            OperatingSystem.IsLinux(),
            static () => AgibuildWebViewGtkNativeLibrary.ResolveExistingPath(),
            static path => NativeLibrary.TryLoad(path, out var handle) && Release(handle));

    internal static WebViewPlatformProbeResult Probe(
        bool isLinux,
        Func<string?> resolveShimPath,
        Func<string, bool> tryLoadLibrary)
    {
        ArgumentNullException.ThrowIfNull(resolveShimPath);
        ArgumentNullException.ThrowIfNull(tryLoadLibrary);

        if (!isLinux)
        {
            return WebViewPlatformProbeResult.UnsupportedPlatform(
                "WebKitGTK is only supported on Linux.");
        }

        string? shimPath;
        try
        {
            shimPath = resolveShimPath();
        }
        catch (DllNotFoundException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(ShimUnavailableReason);
        }
        catch (EntryPointNotFoundException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(ShimUnavailableReason);
        }
        catch (BadImageFormatException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(ShimUnavailableReason);
        }

        if (string.IsNullOrWhiteSpace(shimPath))
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(ShimUnavailableReason);
        }

        try
        {
            return tryLoadLibrary(shimPath)
                ? WebViewPlatformProbeResult.Available()
                : WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason);
        }
        catch (DllNotFoundException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason);
        }
        catch (EntryPointNotFoundException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason);
        }
        catch (BadImageFormatException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason);
        }
    }

    private static bool Release(IntPtr handle)
    {
        NativeLibrary.Free(handle);
        return true;
    }
}
