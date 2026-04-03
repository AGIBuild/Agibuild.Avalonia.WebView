using System.Reflection;
using System.Text.Json;

namespace Agibuild.Fulora;

/// <summary>
/// Provides the Bridge DevTools overlay for debugging bridge calls.
/// Serves the overlay HTML and pushes events to the WebView in real-time.
/// </summary>
public sealed class BridgeDevToolsService : IDisposable
{
    private readonly BridgeEventCollector _collector;
    private readonly IFuloraDiagnosticsSink _diagnosticsSink;
    private readonly DevToolsPanelTracer _tracer;
    private IDisposable? _subscription;
    private Func<string, Task<string?>>? _invokeScript;
    private volatile bool _disposed;

    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Initializes the DevTools service with an optional existing tracer and buffer capacity.</summary>
    public BridgeDevToolsService(IBridgeTracer? existingTracer = null, int bufferCapacity = 500)
    {
        _collector = new BridgeEventCollector(bufferCapacity);
        _diagnosticsSink = new CollectorDiagnosticsSink(_collector);
        _tracer = new DevToolsPanelTracer(_collector, existingTracer);
    }

    /// <summary>
    /// The tracer to wire into <see cref="RuntimeBridgeService"/>.
    /// Pass this as the tracer parameter when constructing the bridge service.
    /// </summary>
    public IBridgeTracer Tracer => _tracer;

    /// <summary>The underlying event collector for testing or direct access.</summary>
    public IBridgeEventCollector Collector => _collector;

    /// <summary>Unified diagnostics sink that projects normalized events into the DevTools collector.</summary>
    public IFuloraDiagnosticsSink DiagnosticsSink => _diagnosticsSink;

    /// <summary>
    /// Returns the self-contained DevTools overlay HTML from embedded resources.
    /// </summary>
    public static string GetOverlayHtml()
    {
        var assembly = typeof(BridgeDevToolsService).Assembly;
        using var stream = assembly.GetManifestResourceStream("Agibuild.Fulora.DevToolsPanel.html")
            ?? throw new InvalidOperationException("DevTools overlay HTML resource not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Starts pushing events to the WebView overlay in real-time.
    /// Call this after the overlay WebView has loaded the DevTools HTML.
    /// </summary>
    /// <param name="invokeScript">
    /// Function to invoke JavaScript in the overlay WebView.
    /// Typically <c>webView.InvokeScriptAsync</c>.
    /// </param>
    public void StartPushing(Func<string, Task<string?>> invokeScript)
    {
        ArgumentNullException.ThrowIfNull(invokeScript);
        _invokeScript = invokeScript;

        var existing = _collector.GetEvents();
        if (existing.Count > 0)
        {
            var json = JsonSerializer.Serialize(existing, EventJsonOptions);
            _ = invokeScript($"window.__bridgeDevToolsLoadEvents({json})");
        }

        _subscription?.Dispose();
        _subscription = _collector.Subscribe(evt =>
        {
            if (_disposed || _invokeScript is null) return;
            var json = JsonSerializer.Serialize(evt, EventJsonOptions);
            _ = _invokeScript($"window.__bridgeDevToolsAddEvent({json})");
        });
    }

    /// <summary>Stops pushing events and clears the subscription.</summary>
    public void StopPushing()
    {
        _subscription?.Dispose();
        _subscription = null;
        _invokeScript = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopPushing();
    }

    private sealed class CollectorDiagnosticsSink : IFuloraDiagnosticsSink
    {
        private readonly BridgeEventCollector _collector;

        public CollectorDiagnosticsSink(BridgeEventCollector collector)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        }

        public void OnEvent(FuloraDiagnosticsEvent diagnosticEvent)
        {
            ArgumentNullException.ThrowIfNull(diagnosticEvent);

            if (!TryMapEvent(diagnosticEvent, out var direction, out var phase, out var serviceName, out var methodName))
            {
                return;
            }

            _collector.Add(new BridgeDevToolsEvent
            {
                Timestamp = diagnosticEvent.TimestampUtc,
                Direction = direction,
                Phase = phase,
                ServiceName = serviceName,
                MethodName = methodName,
                ElapsedMs = diagnosticEvent.DurationMs,
                ErrorMessage = diagnosticEvent.Attributes.TryGetValue("message", out var message) ? message : diagnosticEvent.ErrorType,
                ResultJson = diagnosticEvent.Attributes.TryGetValue("resultType", out var resultType) ? resultType : null,
                ParamsJson = diagnosticEvent.Attributes.TryGetValue("params", out var payload) ? payload : null,
                Truncated = false
            });
        }

        private static bool TryMapEvent(
            FuloraDiagnosticsEvent diagnosticEvent,
            out BridgeCallDirection direction,
            out BridgeCallPhase phase,
            out string serviceName,
            out string methodName)
        {
            var eventName = diagnosticEvent.EventName;
            if (eventName.StartsWith("bridge.export.", StringComparison.Ordinal))
            {
                direction = BridgeCallDirection.Export;
                phase = eventName switch
                {
                    "bridge.export.start" => BridgeCallPhase.Start,
                    "bridge.export.end" => BridgeCallPhase.End,
                    "bridge.export.error" => BridgeCallPhase.Error,
                    _ => default
                };
                serviceName = diagnosticEvent.Service ?? diagnosticEvent.Component;
                methodName = diagnosticEvent.Method ?? string.Empty;
                return eventName is "bridge.export.start" or "bridge.export.end" or "bridge.export.error";
            }

            if (eventName.StartsWith("bridge.import.", StringComparison.Ordinal))
            {
                direction = BridgeCallDirection.Import;
                phase = eventName switch
                {
                    "bridge.import.start" => BridgeCallPhase.Start,
                    "bridge.import.end" => BridgeCallPhase.End,
                    _ => default
                };
                serviceName = diagnosticEvent.Service ?? diagnosticEvent.Component;
                methodName = diagnosticEvent.Method ?? string.Empty;
                return eventName is "bridge.import.start" or "bridge.import.end";
            }

            if (eventName.StartsWith("bridge.service.", StringComparison.Ordinal))
            {
                direction = BridgeCallDirection.Lifecycle;
                phase = eventName switch
                {
                    "bridge.service.exposed" => BridgeCallPhase.ServiceExposed,
                    "bridge.service.removed" => BridgeCallPhase.ServiceRemoved,
                    _ => default
                };
                serviceName = diagnosticEvent.Service ?? diagnosticEvent.Component;
                methodName = diagnosticEvent.Method ?? string.Empty;
                return eventName is "bridge.service.exposed" or "bridge.service.removed";
            }

            if (TrySplitEventName(eventName, out var layer, out var domain, out var terminalPhase) &&
                terminalPhase is "completed" or "denied" or "failed" or "dropped")
            {
                direction = BridgeCallDirection.Lifecycle;
                phase = terminalPhase == "completed" ? BridgeCallPhase.End : BridgeCallPhase.Error;
                serviceName = $"{layer}.{domain}";
                methodName = diagnosticEvent.Status ?? string.Empty;
                return true;
            }

            direction = default;
            phase = default;
            serviceName = string.Empty;
            methodName = string.Empty;
            return false;
        }

        private static bool TrySplitEventName(string eventName, out string layer, out string domain, out string phase)
        {
            var parts = eventName.Split('.', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                layer = string.Empty;
                domain = string.Empty;
                phase = string.Empty;
                return false;
            }

            layer = parts[0];
            domain = parts[1];
            phase = parts[2];
            return true;
        }
    }
}
