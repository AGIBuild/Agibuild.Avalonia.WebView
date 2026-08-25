namespace Agibuild.Fulora.Adapters.Abstractions;

internal sealed record WebViewPlatformProbeResult
{
    private WebViewPlatformProbeResult(
        bool isPlatformSupported,
        bool isRuntimeAvailable,
        string? unavailableReason)
    {
        var isAvailable = isPlatformSupported && isRuntimeAvailable;
        if (isAvailable)
        {
            if (unavailableReason is not null)
            {
                throw new ArgumentException(
                    "Available probe results cannot include an unavailable reason.",
                    nameof(unavailableReason));
            }
        }
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(unavailableReason);
        }

        IsPlatformSupported = isPlatformSupported;
        IsRuntimeAvailable = isRuntimeAvailable;
        UnavailableReason = unavailableReason;
    }

    public bool IsPlatformSupported { get; }

    public bool IsRuntimeAvailable { get; }

    public string? UnavailableReason { get; }

    public bool IsAvailable => IsPlatformSupported && IsRuntimeAvailable;

    public static WebViewPlatformProbeResult Available()
        => new(isPlatformSupported: true, isRuntimeAvailable: true, unavailableReason: null);

    public static WebViewPlatformProbeResult UnsupportedPlatform(string reason)
        => new(isPlatformSupported: false, isRuntimeAvailable: false, unavailableReason: reason);

    public static WebViewPlatformProbeResult RuntimeUnavailable(string reason)
        => new(isPlatformSupported: true, isRuntimeAvailable: false, unavailableReason: reason);
}
