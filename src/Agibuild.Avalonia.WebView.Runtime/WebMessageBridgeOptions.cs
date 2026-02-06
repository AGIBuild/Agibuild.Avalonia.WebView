namespace Agibuild.Avalonia.WebView;

public sealed class WebMessageBridgeOptions
{
    /// <summary>
    /// Allowed origins for WebMessage. Exact string match (e.g. "https://example.com").
    /// </summary>
    public IReadOnlySet<string> AllowedOrigins { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public int ProtocolVersion { get; init; } = 1;

    public IWebMessageDropDiagnosticsSink? DropDiagnosticsSink { get; init; }
}

internal sealed class DefaultWebMessagePolicy : IWebMessagePolicy
{
    private readonly IReadOnlySet<string> _allowedOrigins;
    private readonly int _protocolVersion;
    private readonly Guid _expectedChannelId;

    public DefaultWebMessagePolicy(IReadOnlySet<string> allowedOrigins, int protocolVersion, Guid expectedChannelId)
    {
        _allowedOrigins = allowedOrigins;
        _protocolVersion = protocolVersion;
        _expectedChannelId = expectedChannelId;
    }

    public WebMessagePolicyDecision Evaluate(in WebMessageEnvelope envelope)
    {
        if (_allowedOrigins.Count > 0 && !_allowedOrigins.Contains(envelope.Origin))
        {
            return WebMessagePolicyDecision.Deny(WebMessageDropReason.OriginNotAllowed);
        }

        if (envelope.ProtocolVersion != _protocolVersion)
        {
            return WebMessagePolicyDecision.Deny(WebMessageDropReason.ProtocolMismatch);
        }

        if (envelope.ChannelId != _expectedChannelId)
        {
            return WebMessagePolicyDecision.Deny(WebMessageDropReason.ChannelMismatch);
        }

        return WebMessagePolicyDecision.Allow();
    }
}

