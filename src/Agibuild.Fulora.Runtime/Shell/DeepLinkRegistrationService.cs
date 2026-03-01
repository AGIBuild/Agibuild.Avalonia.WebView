using System.Collections.Concurrent;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Default implementation of <see cref="IDeepLinkRegistrationService"/>.
/// Provides route registration, URI normalization, policy admission, idempotency enforcement,
/// and dispatch to <see cref="WebViewShellActivationCoordinator"/>.
/// </summary>
public sealed class DeepLinkRegistrationService : IDeepLinkRegistrationService
{
    private readonly ConcurrentDictionary<string, List<DeepLinkRouteDeclaration>> _routes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _replayWindow = new(StringComparer.Ordinal);
    private readonly IDeepLinkAdmissionPolicy? _admissionPolicy;
    private readonly TimeSpan _replayWindowDuration;
    private readonly object _routeLock = new();

    /// <summary>
    /// Raised when a diagnostic event occurs during registration or activation lifecycle.
    /// </summary>
    public event EventHandler<DeepLinkDiagnosticEventArgs>? DiagnosticEvent;

    /// <summary>
    /// Creates a new deep-link registration service.
    /// </summary>
    /// <param name="admissionPolicy">Optional policy for activation admission. When null, all admitted.</param>
    /// <param name="replayWindowDuration">Duration of the idempotency replay window. Defaults to 5 seconds.</param>
    public DeepLinkRegistrationService(
        IDeepLinkAdmissionPolicy? admissionPolicy = null,
        TimeSpan? replayWindowDuration = null)
    {
        _admissionPolicy = admissionPolicy;
        _replayWindowDuration = replayWindowDuration ?? TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc/>
    public DeepLinkRegistrationResult RegisterRoute(DeepLinkRouteDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        var correlationId = Guid.NewGuid();

        lock (_routeLock)
        {
            var list = _routes.GetOrAdd(declaration.AppIdentity, _ => []);
            var duplicate = list.Find(d =>
                d.Scheme == declaration.Scheme &&
                d.HostPattern == declaration.HostPattern);

            if (duplicate is not null)
            {
                EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.RegistrationAttempt, "rejected", "duplicate-declaration");
                return DeepLinkRegistrationResult.Rejected("duplicate-declaration");
            }

            list.Add(declaration);
        }

        EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.RegistrationAttempt, "accepted");
        return DeepLinkRegistrationResult.Accepted();
    }

    /// <inheritdoc/>
    public async Task<DeepLinkActivationIngressResult> IngestActivationAsync(
        Uri rawUri,
        DeepLinkActivationSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawUri);
        if (!rawUri.IsAbsoluteUri)
            throw new ArgumentException("Raw URI must be absolute.", nameof(rawUri));

        cancellationToken.ThrowIfCancellationRequested();

        var envelope = Normalize(rawUri, source);
        var correlationId = envelope.ActivationId;

        var matchingAppIdentity = FindMatchingAppIdentity(envelope);
        if (matchingAppIdentity is null)
        {
            EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.NoMatchingRoute, "no-match", route: envelope.Route, rawUri: rawUri);
            return DeepLinkActivationIngressResult.NoMatchingRoute();
        }

        EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.ActivationNormalized, "matched", route: envelope.Route, rawUri: rawUri);

        if (_admissionPolicy is not null)
        {
            var decision = _admissionPolicy.Evaluate(envelope);
            if (!decision.IsAllowed)
            {
                EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.PolicyDenied, "denied", decision.DenyReason, envelope.Route, rawUri);
                return DeepLinkActivationIngressResult.PolicyDenied(decision.DenyReason ?? "policy-denied");
            }
        }

        if (!TryAcquireIdempotencySlot(envelope.IdempotencyKey))
        {
            EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.DuplicateSuppressed, "duplicate", route: envelope.Route, rawUri: rawUri);
            return DeepLinkActivationIngressResult.Duplicate();
        }

        var metadata = new Dictionary<string, string>
        {
            ["deeplink.source"] = source.ToString(),
            ["deeplink.idempotencyKey"] = envelope.IdempotencyKey,
            ["deeplink.route"] = envelope.Route
        };
        var request = new WebViewShellActivationRequest(rawUri, metadata);
        var forwardResult = await WebViewShellActivationCoordinator.ForwardAsync(
            matchingAppIdentity, request, cancellationToken).ConfigureAwait(false);

        if (forwardResult.Status == WebViewShellActivationForwardStatus.Delivered)
        {
            EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.Dispatched, "delivered", route: envelope.Route, rawUri: rawUri);
            return DeepLinkActivationIngressResult.Dispatched();
        }

        EmitDiagnostic(correlationId, DeepLinkDiagnosticEventType.NoMatchingRoute, "no-active-primary", route: envelope.Route, rawUri: rawUri);
        return DeepLinkActivationIngressResult.NoMatchingRoute();
    }

    internal static DeepLinkActivationEnvelope Normalize(Uri rawUri, DeepLinkActivationSource source)
    {
        var scheme = rawUri.Scheme.ToLowerInvariant();
        var host = rawUri.Host.ToLowerInvariant();
        var path = rawUri.AbsolutePath.TrimEnd('/');
        if (path.Length == 0) path = "/";

        var route = $"{scheme}://{host}{path}";

        return new DeepLinkActivationEnvelope(
            Guid.NewGuid(),
            route,
            rawUri,
            source,
            DateTimeOffset.UtcNow);
    }

    private string? FindMatchingAppIdentity(DeepLinkActivationEnvelope envelope)
    {
        var uriScheme = envelope.RawUri.Scheme.ToLowerInvariant();
        var uriHost = envelope.RawUri.Host.ToLowerInvariant();

        lock (_routeLock)
        {
            foreach (var (appIdentity, declarations) in _routes)
            {
                foreach (var decl in declarations)
                {
                    if (decl.Scheme != uriScheme)
                        continue;

                    if (decl.HostPattern is not null &&
                        !string.Equals(decl.HostPattern, uriHost, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return appIdentity;
                }
            }
        }

        return null;
    }

    private bool TryAcquireIdempotencySlot(string idempotencyKey)
    {
        var now = DateTimeOffset.UtcNow;

        PruneExpiredSlots(now);

        return _replayWindow.TryAdd(idempotencyKey, now);
    }

    private void PruneExpiredSlots(DateTimeOffset now)
    {
        foreach (var kvp in _replayWindow)
        {
            if (now - kvp.Value > _replayWindowDuration)
                _replayWindow.TryRemove(kvp.Key, out _);
        }
    }

    private void EmitDiagnostic(
        Guid correlationId,
        DeepLinkDiagnosticEventType eventType,
        string outcome,
        string? reason = null,
        string? route = null,
        Uri? rawUri = null)
    {
        DiagnosticEvent?.Invoke(this, new DeepLinkDiagnosticEventArgs(
            correlationId, eventType, outcome, reason, route, rawUri));
    }
}
