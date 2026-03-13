namespace Agibuild.Fulora.Plugin.Biometric;

/// <summary>
/// iOS biometric provider using LocalAuthentication framework (LAContext).
/// Supports Face ID and Touch ID — stub until ObjCRuntime native binding is added.
/// </summary>
public sealed class IosBiometricProvider : IBiometricPlatformProvider
{
    /// <inheritdoc />
    public Task<BiometricAvailability> CheckAvailabilityAsync(CancellationToken ct = default)
        => Task.FromResult(new BiometricAvailability(
            OperatingSystem.IsIOS(), "faceid", OperatingSystem.IsIOS() ? null : "wrong_platform"));

    /// <inheritdoc />
    public Task<BiometricResult> AuthenticateAsync(string reason, CancellationToken ct = default)
        => Task.FromResult(new BiometricResult(false, "not_implemented",
            "iOS LAContext integration pending — requires ObjCRuntime native binding"));
}
