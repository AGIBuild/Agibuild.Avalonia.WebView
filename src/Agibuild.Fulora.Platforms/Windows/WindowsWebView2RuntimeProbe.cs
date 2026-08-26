using Agibuild.Fulora.Adapters.Abstractions;
using Microsoft.Web.WebView2.Core;

namespace Agibuild.Fulora.Platforms;

internal static class WindowsWebView2RuntimeProbe
{
    private const string RuntimeUnavailableReason = "The WebView2 Runtime could not be discovered.";

    public static WebViewPlatformProbeResult Probe()
        => Probe(
            OperatingSystem.IsWindows(),
            static () => CoreWebView2Environment.GetAvailableBrowserVersionString());

    internal static WebViewPlatformProbeResult Probe(
        bool isWindows,
        Func<string?> getAvailableBrowserVersion)
    {
        ArgumentNullException.ThrowIfNull(getAvailableBrowserVersion);

        if (!isWindows)
        {
            return WebViewPlatformProbeResult.UnsupportedPlatform(
                "WebView2 is only supported on Windows.");
        }

        try
        {
            return string.IsNullOrWhiteSpace(getAvailableBrowserVersion())
                ? WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason)
                : WebViewPlatformProbeResult.Available();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return WebViewPlatformProbeResult.RuntimeUnavailable(RuntimeUnavailableReason);
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
}
