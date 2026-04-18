using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

/// <summary>
/// Logger-backed diagnostics sink for unified event envelopes.
/// </summary>
public sealed partial class LoggingFuloraDiagnosticsSink : IFuloraDiagnosticsSink
{
    private readonly ILogger _logger;

    /// <summary>Create a new logger-backed diagnostics sink.</summary>
    public LoggingFuloraDiagnosticsSink(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void OnEvent(FuloraDiagnosticsEvent diagnosticEvent)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvent);

        var isWarningStatus =
            string.Equals(diagnosticEvent.Status, "error", StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticEvent.Status, "failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticEvent.Status, "denied", StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticEvent.Status, "dropped", StringComparison.OrdinalIgnoreCase);

        if (isWarningStatus)
        {
            LogDiagnosticsWarning(
                diagnosticEvent.EventName,
                diagnosticEvent.Layer,
                diagnosticEvent.Component,
                diagnosticEvent.CapabilityId,
                diagnosticEvent.Operation,
                diagnosticEvent.Service,
                diagnosticEvent.Method,
                diagnosticEvent.Status,
                diagnosticEvent.DurationMs,
                diagnosticEvent.ErrorType);
        }
        else
        {
            LogDiagnosticsInformation(
                diagnosticEvent.EventName,
                diagnosticEvent.Layer,
                diagnosticEvent.Component,
                diagnosticEvent.CapabilityId,
                diagnosticEvent.Operation,
                diagnosticEvent.Service,
                diagnosticEvent.Method,
                diagnosticEvent.Status,
                diagnosticEvent.DurationMs,
                diagnosticEvent.ErrorType);
        }
    }
}
