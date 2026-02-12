using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public class BridgeTracerTests
{
    // ==================== NullBridgeTracer ====================

    [Fact]
    public void NullBridgeTracer_singleton_is_not_null()
    {
        Assert.NotNull(NullBridgeTracer.Instance);
    }

    [Fact]
    public void NullBridgeTracer_methods_do_not_throw()
    {
        var tracer = NullBridgeTracer.Instance;
        tracer.OnExportCallStart("svc", "method", "{}");
        tracer.OnExportCallEnd("svc", "method", 42, "string");
        tracer.OnExportCallError("svc", "method", 10, new Exception("test"));
        tracer.OnImportCallStart("svc", "method", null);
        tracer.OnImportCallEnd("svc", "method", 5);
        tracer.OnServiceExposed("svc", 3, true);
        tracer.OnServiceRemoved("svc");
    }

    // ==================== LoggingBridgeTracer ====================

    [Fact]
    public void LoggingBridgeTracer_requires_logger()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingBridgeTracer(null!));
    }

    [Fact]
    public void LoggingBridgeTracer_methods_do_not_throw()
    {
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingBridgeTracer>();
        var tracer = new LoggingBridgeTracer(logger);

        tracer.OnExportCallStart("svc", "method", "{}");
        tracer.OnExportCallEnd("svc", "method", 42, "string");
        tracer.OnExportCallError("svc", "method", 10, new Exception("test"));
        tracer.OnImportCallStart("svc", "method", null);
        tracer.OnImportCallEnd("svc", "method", 5);
        tracer.OnServiceExposed("svc", 3, false);
        tracer.OnServiceRemoved("svc");
    }

    // ==================== Custom tracer ====================

    [Fact]
    public void Custom_tracer_receives_events()
    {
        var tracer = new RecordingTracer();

        tracer.OnExportCallStart("Calc", "Add", "{\"a\":1}");
        tracer.OnExportCallEnd("Calc", "Add", 5, "int");
        tracer.OnServiceExposed("Calc", 2, true);

        Assert.Equal(3, tracer.Events.Count);
        Assert.Contains("ExportStart:Calc.Add", tracer.Events);
        Assert.Contains("ExportEnd:Calc.Add:5ms", tracer.Events);
        Assert.Contains("Exposed:Calc:2:SG", tracer.Events);
    }

    private sealed class RecordingTracer : IBridgeTracer
    {
        public List<string> Events { get; } = new();

        public void OnExportCallStart(string serviceName, string methodName, string? paramsJson)
            => Events.Add($"ExportStart:{serviceName}.{methodName}");

        public void OnExportCallEnd(string serviceName, string methodName, long elapsedMs, string? resultType)
            => Events.Add($"ExportEnd:{serviceName}.{methodName}:{elapsedMs}ms");

        public void OnExportCallError(string serviceName, string methodName, long elapsedMs, Exception error)
            => Events.Add($"ExportError:{serviceName}.{methodName}:{error.Message}");

        public void OnImportCallStart(string serviceName, string methodName, string? paramsJson)
            => Events.Add($"ImportStart:{serviceName}.{methodName}");

        public void OnImportCallEnd(string serviceName, string methodName, long elapsedMs)
            => Events.Add($"ImportEnd:{serviceName}.{methodName}:{elapsedMs}ms");

        public void OnServiceExposed(string serviceName, int methodCount, bool isSourceGenerated)
            => Events.Add($"Exposed:{serviceName}:{methodCount}:{(isSourceGenerated ? "SG" : "Reflection")}");

        public void OnServiceRemoved(string serviceName)
            => Events.Add($"Removed:{serviceName}");
    }
}
