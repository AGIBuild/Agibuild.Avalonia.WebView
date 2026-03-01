using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Activation request payload forwarded from secondary instance to current primary owner.
/// </summary>
public sealed class WebViewShellActivationRequest
{
    /// <summary>
    /// Creates an activation payload for deep-link dispatch.
    /// </summary>
    public WebViewShellActivationRequest(
        Uri deepLinkUri,
        IReadOnlyDictionary<string, string>? metadata = null,
        DateTimeOffset? receivedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(deepLinkUri);
        if (!deepLinkUri.IsAbsoluteUri)
            throw new ArgumentException("Deep link URI must be absolute.", nameof(deepLinkUri));

        DeepLinkUri = deepLinkUri;
        Metadata = metadata is null
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal))
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(metadata, StringComparer.Ordinal));
        ReceivedAtUtc = receivedAtUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deep-link URI delivered by the launcher/secondary instance.
    /// </summary>
    public Uri DeepLinkUri { get; }

    /// <summary>
    /// Activation metadata bag with stable ordinal-key semantics.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// UTC timestamp when activation payload is created.
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; }
}

/// <summary>
/// Forward status for secondary-instance activation dispatch.
/// </summary>
public enum WebViewShellActivationForwardStatus
{
    /// <summary>
    /// Activation payload was delivered to active primary handler.
    /// </summary>
    Delivered = 0,
    /// <summary>
    /// No active primary owner exists for this app identity.
    /// </summary>
    NoActivePrimary = 1
}

/// <summary>
/// Forward result for activation dispatch.
/// </summary>
public sealed record WebViewShellActivationForwardResult(
    WebViewShellActivationForwardStatus Status,
    string? Reason = null)
{
    /// <summary>
    /// Creates a delivered result.
    /// </summary>
    public static WebViewShellActivationForwardResult Delivered()
        => new(WebViewShellActivationForwardStatus.Delivered);

    /// <summary>
    /// Creates a no-primary result with deterministic reason code.
    /// </summary>
    public static WebViewShellActivationForwardResult NoActivePrimary()
        => new(WebViewShellActivationForwardStatus.NoActivePrimary, "no-active-primary");
}

/// <summary>
/// Registration handle returned by <see cref="WebViewShellActivationCoordinator.Register"/>.
/// </summary>
public sealed class WebViewShellActivationRegistration : IDisposable
{
    private readonly string _appIdentity;
    private readonly Guid _registrationId;
    private bool _disposed;

    internal WebViewShellActivationRegistration(string appIdentity, Guid registrationId, bool isPrimary)
    {
        _appIdentity = appIdentity;
        _registrationId = registrationId;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Whether this registration is the current primary owner.
    /// </summary>
    public bool IsPrimary { get; }

    /// <summary>
    /// Forwards activation to active primary owner.
    /// </summary>
    public Task<WebViewShellActivationForwardResult> ForwardAsync(
        WebViewShellActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return WebViewShellActivationCoordinator.ForwardAsync(_appIdentity, request, cancellationToken);
    }

    /// <summary>
    /// Releases this registration. If it is the active primary owner, ownership is removed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        WebViewShellActivationCoordinator.Release(_appIdentity, _registrationId);
    }
}

/// <summary>
/// Deterministic in-process single-instance activation coordinator.
/// </summary>
public static class WebViewShellActivationCoordinator
{
    private static readonly ConcurrentDictionary<string, PrimaryOwner> Owners = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers an activation endpoint for the app identity.
    /// The first active registration becomes primary; subsequent registrations are secondary.
    /// </summary>
    public static WebViewShellActivationRegistration Register(
        string appIdentity,
        Func<WebViewShellActivationRequest, CancellationToken, Task> primaryHandler)
    {
        ArgumentException.ThrowIfNullOrEmpty(appIdentity);
        ArgumentNullException.ThrowIfNull(primaryHandler);

        var normalizedIdentity = appIdentity.Trim();
        if (normalizedIdentity.Length == 0)
            throw new ArgumentException("App identity cannot be empty.", nameof(appIdentity));

        var registrationId = Guid.NewGuid();
        var owner = new PrimaryOwner(registrationId, primaryHandler);
        var isPrimary = Owners.TryAdd(normalizedIdentity, owner);
        return new WebViewShellActivationRegistration(normalizedIdentity, registrationId, isPrimary);
    }

    internal static async Task<WebViewShellActivationForwardResult> ForwardAsync(
        string appIdentity,
        WebViewShellActivationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(appIdentity);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Owners.TryGetValue(appIdentity, out var owner))
            return WebViewShellActivationForwardResult.NoActivePrimary();

        await owner.Handler(request, cancellationToken).ConfigureAwait(false);
        return WebViewShellActivationForwardResult.Delivered();
    }

    internal static void Release(string appIdentity, Guid registrationId)
    {
        if (Owners.TryGetValue(appIdentity, out var owner) && owner.RegistrationId == registrationId)
            Owners.TryRemove(appIdentity, out _);
    }

    private sealed record PrimaryOwner(
        Guid RegistrationId,
        Func<WebViewShellActivationRequest, CancellationToken, Task> Handler);
}
