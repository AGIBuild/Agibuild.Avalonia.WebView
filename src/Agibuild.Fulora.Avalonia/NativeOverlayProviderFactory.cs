namespace Agibuild.Fulora.NativeOverlay;

/// <summary>
/// Creates the platform-appropriate <see cref="INativeOverlayProvider"/> implementation.
/// </summary>
public static class NativeOverlayProviderFactory
{
    /// <summary>Returns a native overlay provider for the current operating system.</summary>
    public static INativeOverlayProvider Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsNativeOverlayProvider();
        if (OperatingSystem.IsMacOS())
            return new MacOsNativeOverlayProvider();
        if (OperatingSystem.IsLinux())
            return new LinuxNativeOverlayProvider();

        throw new PlatformNotSupportedException("Native overlay is not supported on this platform.");
    }
}
