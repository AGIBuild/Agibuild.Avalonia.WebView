namespace Agibuild.Fulora.Plugin.Biometric;

/// <summary>
/// Linux biometric provider — reports platform not supported (no standard biometric API).
/// </summary>
public sealed class LinuxBiometricProvider : IBiometricPlatformProvider
{
    /// <inheritdoc />
    public Task<BiometricAvailability> CheckAvailabilityAsync(CancellationToken ct = default)
        => Task.FromResult(new BiometricAvailability(false, null, "platform_not_supported"));

    /// <inheritdoc />
    public Task<BiometricResult> AuthenticateAsync(string reason, CancellationToken ct = default)
        => Task.FromResult(new BiometricResult(false, "not_available", "Biometric authentication is not supported on Linux"));
}
