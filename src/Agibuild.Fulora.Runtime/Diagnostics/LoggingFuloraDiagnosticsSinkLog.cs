using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

// LoggerMessage source-generator helpers for LoggingFuloraDiagnosticsSink.
//
// Two methods are required because the source generator wants the LogLevel
// fixed at compile time, while OnEvent picks Warning vs Information at
// runtime based on the unified diagnostics envelope's status field. The
// pair share the same template so log queries and downstream sinks see
// identical structured payloads.
//
// EventId range 3600-3699 reserved for this sink.
public sealed partial class LoggingFuloraDiagnosticsSink
{
    private const string DiagnosticsTemplate =
        "Fulora diagnostics {EventName} layer={Layer} component={Component} "
        + "capabilityId={CapabilityId} operation={Operation} service={Service} "
        + "method={Method} status={Status} durationMs={DurationMs} errorType={ErrorType}";

    [LoggerMessage(EventId = 3600, Level = LogLevel.Information, Message = DiagnosticsTemplate)]
    private partial void LogDiagnosticsInformation(
        string eventName,
        string layer,
        string component,
        string? capabilityId,
        string? operation,
        string? service,
        string? method,
        string? status,
        long? durationMs,
        string? errorType);

    [LoggerMessage(EventId = 3601, Level = LogLevel.Warning, Message = DiagnosticsTemplate)]
    private partial void LogDiagnosticsWarning(
        string eventName,
        string layer,
        string component,
        string? capabilityId,
        string? operation,
        string? service,
        string? method,
        string? status,
        long? durationMs,
        string? errorType);
}
