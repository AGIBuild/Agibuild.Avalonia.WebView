namespace Agibuild.Fulora.Plugin.Biometric;

/// <summary>
/// Android biometric provider using BiometricPrompt API.
/// Supports fingerprint and face — stub until AndroidX Biometric library binding is added.
/// </summary>
public sealed class AndroidBiometricProvider : IBiometricPlatformProvider
{
    /// <inheritdoc />
    public Task<BiometricAvailability> CheckAvailabilityAsync(CancellationToken ct = default)
        => Task.FromResult(new BiometricAvailability(
            OperatingSystem.IsAndroid(), "fingerprint", OperatingSystem.IsAndroid() ? null : "wrong_platform"));

    /// <inheritdoc />
    public Task<BiometricResult> AuthenticateAsync(string reason, CancellationToken ct = default)
        => Task.FromResult(new BiometricResult(false, "not_implemented",
            "Android BiometricPrompt integration pending — requires AndroidX Biometric library binding"));
}
