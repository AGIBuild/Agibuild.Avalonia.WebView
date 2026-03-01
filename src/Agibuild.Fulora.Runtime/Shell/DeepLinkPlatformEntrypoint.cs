namespace Agibuild.Fulora.Shell;

/// <summary>
/// Platform support status for deep-link native registration.
/// </summary>
public enum DeepLinkPlatformSupport
{
    /// <summary>Platform supports deep-link activation via protocol handler.</summary>
    Supported = 0,
    /// <summary>Platform does not support deep-link activation natively.</summary>
    NotSupported = 1
}

/// <summary>
/// Describes a platform's deep-link activation entry mapping.
/// </summary>
public sealed record DeepLinkPlatformEntryDescriptor(
    string PlatformName,
    DeepLinkPlatformSupport Support,
    string? EntrypointDescription = null)
{
    /// <summary>Windows: protocol activation via command-line args or UWP activation.</summary>
    public static DeepLinkPlatformEntryDescriptor Windows { get; } = new("Windows", DeepLinkPlatformSupport.Supported, "Protocol activation via command-line URI argument or AppLifecycle activation.");

    /// <summary>macOS: protocol activation via NSApplicationDelegate openURLs.</summary>
    public static DeepLinkPlatformEntryDescriptor MacOS { get; } = new("macOS", DeepLinkPlatformSupport.Supported, "Protocol activation via NSApplicationDelegate application:openURLs: callback.");

    /// <summary>iOS: protocol activation via UIApplicationDelegate openURL.</summary>
    public static DeepLinkPlatformEntryDescriptor iOS { get; } = new("iOS", DeepLinkPlatformSupport.Supported, "Protocol activation via UIApplicationDelegate application:openURL:options: callback.");

    /// <summary>Android: protocol activation via Intent with ACTION_VIEW.</summary>
    public static DeepLinkPlatformEntryDescriptor Android { get; } = new("Android", DeepLinkPlatformSupport.Supported, "Protocol activation via Intent with ACTION_VIEW and data URI.");

    /// <summary>Linux/GTK: protocol activation via XDG MIME handler or DBus activation.</summary>
    public static DeepLinkPlatformEntryDescriptor Linux { get; } = new("Linux", DeepLinkPlatformSupport.Supported, "Protocol activation via xdg-open / .desktop handler or DBus Activate signal.");

    /// <summary>Returns all known platform descriptors.</summary>
    public static IReadOnlyList<DeepLinkPlatformEntryDescriptor> All { get; } =
    [
        Windows,
        MacOS,
        iOS,
        Android,
        Linux
    ];
}

/// <summary>
/// Static helper for platform entrypoints to ingest deep-link activation
/// into <see cref="IDeepLinkRegistrationService"/>.
/// </summary>
public static class DeepLinkPlatformEntrypoint
{
    /// <summary>
    /// Converts a raw platform-provided URI string into a typed ingestion call.
    /// Platform startup code should call this with the URI received from the OS.
    /// </summary>
    public static async Task<DeepLinkActivationIngressResult> HandlePlatformActivationAsync(
        IDeepLinkRegistrationService service,
        string rawUriString,
        DeepLinkActivationSource source = DeepLinkActivationSource.ProtocolLaunch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(rawUriString);

        if (!Uri.TryCreate(rawUriString, UriKind.Absolute, out var uri))
            return DeepLinkActivationIngressResult.NoMatchingRoute();

        return await service.IngestActivationAsync(uri, source, cancellationToken).ConfigureAwait(false);
    }
}
