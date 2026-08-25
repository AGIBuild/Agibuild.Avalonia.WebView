namespace Agibuild.Fulora.Adapters.Abstractions;

internal interface IWebViewPlatformProvider
{
    string Id { get; }

    int Priority { get; }

    bool CanHandleCurrentPlatform();

    IWebViewAdapter CreateAdapter();

    WebViewPlatformProbeResult ProbeCurrentPlatform()
        => CanHandleCurrentPlatform()
            ? WebViewPlatformProbeResult.Available()
            : WebViewPlatformProbeResult.UnsupportedPlatform(
                $"Provider '{Id}' cannot handle the current platform.");
}
