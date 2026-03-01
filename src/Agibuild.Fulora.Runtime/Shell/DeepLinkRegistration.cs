using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Source of a deep-link activation event.
/// </summary>
public enum DeepLinkActivationSource
{
    /// <summary>Activation originated from OS protocol handler launch.</summary>
    ProtocolLaunch = 0,
    /// <summary>Activation forwarded from secondary instance.</summary>
    SecondaryForward = 1,
    /// <summary>Activation injected programmatically by host.</summary>
    HostInjected = 2
}

/// <summary>
/// Canonical deep-link activation envelope normalized from platform-specific payloads.
/// </summary>
public sealed class DeepLinkActivationEnvelope
{
    /// <summary>
    /// Creates a canonical deep-link activation envelope.
    /// </summary>
    public DeepLinkActivationEnvelope(
        Guid activationId,
        string route,
        Uri rawUri,
        DeepLinkActivationSource source,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrEmpty(route);
        ArgumentNullException.ThrowIfNull(rawUri);
        if (!rawUri.IsAbsoluteUri)
            throw new ArgumentException("Raw URI must be absolute.", nameof(rawUri));

        ActivationId = activationId;
        Route = route;
        RawUri = rawUri;
        Source = source;
        OccurredAtUtc = occurredAtUtc;
        IdempotencyKey = ComputeIdempotencyKey(route, rawUri, source);
    }

    /// <summary>Unique identifier for this activation event.</summary>
    public Guid ActivationId { get; }

    /// <summary>Canonical route derived from normalized URI (scheme + host + path, lowered).</summary>
    public string Route { get; }

    /// <summary>Original unmodified URI from the platform.</summary>
    public Uri RawUri { get; }

    /// <summary>Origin of the activation.</summary>
    public DeepLinkActivationSource Source { get; }

    /// <summary>UTC timestamp when the activation occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Deterministic idempotency key derived from stable canonical fields.</summary>
    public string IdempotencyKey { get; }

    private static string ComputeIdempotencyKey(string route, Uri rawUri, DeepLinkActivationSource source)
    {
        var query = rawUri.IsAbsoluteUri ? rawUri.Query : string.Empty;
        return $"{route}|{query}|{(int)source}";
    }
}

/// <summary>
/// Declaration of a supported deep-link scheme and route pattern for an app identity.
/// </summary>
public sealed class DeepLinkRouteDeclaration
{
    /// <summary>
    /// Creates a deep-link route declaration for an app identity.
    /// </summary>
    public DeepLinkRouteDeclaration(string appIdentity, string scheme, string? hostPattern = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(appIdentity);
        ArgumentException.ThrowIfNullOrEmpty(scheme);

        var trimmedIdentity = appIdentity.Trim();
        if (trimmedIdentity.Length == 0)
            throw new ArgumentException("App identity cannot be whitespace.", nameof(appIdentity));

        AppIdentity = trimmedIdentity;
        Scheme = scheme.ToLowerInvariant();
        HostPattern = hostPattern;
    }

    /// <summary>App identity this declaration belongs to.</summary>
    public string AppIdentity { get; }

    /// <summary>Lowered URI scheme accepted by this route (e.g. "myapp").</summary>
    public string Scheme { get; }

    /// <summary>Optional host pattern filter (null = accept any host).</summary>
    public string? HostPattern { get; }
}

/// <summary>
/// Status of a deep-link registration validation.
/// </summary>
public enum DeepLinkRegistrationStatus
{
    /// <summary>Registration accepted.</summary>
    Accepted = 0,
    /// <summary>Registration rejected due to validation failure.</summary>
    Rejected = 1
}

/// <summary>
/// Result of a deep-link route registration attempt.
/// </summary>
public sealed record DeepLinkRegistrationResult(
    DeepLinkRegistrationStatus Status,
    string? Reason = null)
{
    /// <summary>Creates an accepted registration result.</summary>
    public static DeepLinkRegistrationResult Accepted() => new(DeepLinkRegistrationStatus.Accepted);
    /// <summary>Creates a rejected registration result with reason.</summary>
    public static DeepLinkRegistrationResult Rejected(string reason) => new(DeepLinkRegistrationStatus.Rejected, reason);
}

/// <summary>
/// Status of a deep-link activation ingress attempt.
/// </summary>
public enum DeepLinkActivationIngressStatus
{
    /// <summary>Activation dispatched to orchestration.</summary>
    Dispatched = 0,
    /// <summary>Activation denied by policy.</summary>
    PolicyDenied = 1,
    /// <summary>Activation suppressed as duplicate within replay window.</summary>
    Duplicate = 2,
    /// <summary>Activation rejected: no matching route declaration.</summary>
    NoMatchingRoute = 3
}

/// <summary>
/// Result of attempting to ingest a deep-link activation.
/// </summary>
public sealed record DeepLinkActivationIngressResult(
    DeepLinkActivationIngressStatus Status,
    string? Reason = null)
{
    /// <summary>Creates a dispatched ingress result.</summary>
    public static DeepLinkActivationIngressResult Dispatched() => new(DeepLinkActivationIngressStatus.Dispatched);
    /// <summary>Creates a policy-denied ingress result.</summary>
    public static DeepLinkActivationIngressResult PolicyDenied(string reason) => new(DeepLinkActivationIngressStatus.PolicyDenied, reason);
    /// <summary>Creates a duplicate-suppressed ingress result.</summary>
    public static DeepLinkActivationIngressResult Duplicate() => new(DeepLinkActivationIngressStatus.Duplicate, "duplicate-within-replay-window");
    /// <summary>Creates a no-matching-route ingress result.</summary>
    public static DeepLinkActivationIngressResult NoMatchingRoute() => new(DeepLinkActivationIngressStatus.NoMatchingRoute, "no-matching-route");
}

/// <summary>
/// Policy decision for deep-link activation admission.
/// </summary>
public sealed record DeepLinkAdmissionDecision(bool IsAllowed, string? DenyReason = null)
{
    /// <summary>Creates an allow decision.</summary>
    public static DeepLinkAdmissionDecision Allow() => new(true);
    /// <summary>Creates a deny decision with reason.</summary>
    public static DeepLinkAdmissionDecision Deny(string reason) => new(false, reason);
}

/// <summary>
/// Policy interface for deep-link activation admission.
/// </summary>
public interface IDeepLinkAdmissionPolicy
{
    /// <summary>
    /// Evaluates whether a canonical activation envelope should be admitted for orchestration dispatch.
    /// </summary>
    DeepLinkAdmissionDecision Evaluate(DeepLinkActivationEnvelope envelope);
}

/// <summary>
/// Diagnostic event type for deep-link activation lifecycle.
/// </summary>
public enum DeepLinkDiagnosticEventType
{
    /// <summary>Route registration attempted.</summary>
    RegistrationAttempt = 0,
    /// <summary>Activation normalized and route matched.</summary>
    ActivationNormalized = 1,
    /// <summary>Activation denied by policy.</summary>
    PolicyDenied = 2,
    /// <summary>Activation suppressed as duplicate.</summary>
    DuplicateSuppressed = 3,
    /// <summary>Activation dispatched to orchestration.</summary>
    Dispatched = 4,
    /// <summary>No matching route for activation.</summary>
    NoMatchingRoute = 5
}

/// <summary>
/// Structured diagnostic event for deep-link registration and activation lifecycle.
/// </summary>
public sealed class DeepLinkDiagnosticEventArgs : EventArgs
{
    /// <summary>Creates a deep-link diagnostic event.</summary>
    public DeepLinkDiagnosticEventArgs(
        Guid correlationId,
        DeepLinkDiagnosticEventType eventType,
        string outcome,
        string? reason = null,
        string? route = null,
        Uri? rawUri = null)
    {
        CorrelationId = correlationId;
        EventType = eventType;
        Outcome = outcome;
        Reason = reason;
        Route = route;
        RawUri = rawUri;
    }

    /// <summary>Correlation identifier for the activation lifecycle.</summary>
    public Guid CorrelationId { get; }

    /// <summary>Type of diagnostic event.</summary>
    public DeepLinkDiagnosticEventType EventType { get; }

    /// <summary>Deterministic outcome string.</summary>
    public string Outcome { get; }

    /// <summary>Optional reason for deny/suppress/reject outcomes.</summary>
    public string? Reason { get; }

    /// <summary>Canonical route if available.</summary>
    public string? Route { get; }

    /// <summary>Raw URI if available.</summary>
    public Uri? RawUri { get; }
}

/// <summary>
/// Host-facing service for deep-link route registration and native activation ingress.
/// </summary>
public interface IDeepLinkRegistrationService
{
    /// <summary>
    /// Registers a deep-link route declaration for an app identity.
    /// </summary>
    DeepLinkRegistrationResult RegisterRoute(DeepLinkRouteDeclaration declaration);

    /// <summary>
    /// Ingests a native activation payload, normalizing it and dispatching through the orchestration pipeline.
    /// </summary>
    Task<DeepLinkActivationIngressResult> IngestActivationAsync(
        Uri rawUri,
        DeepLinkActivationSource source,
        CancellationToken cancellationToken = default);
}
