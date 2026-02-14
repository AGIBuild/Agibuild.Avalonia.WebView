namespace AvaloniReact.Bridge.Models;

/// <summary>Live runtime metrics that change over time.</summary>
public record RuntimeMetrics(
    double WorkingSetMb,
    double GcTotalMemoryMb,
    int ThreadCount,
    double UptimeSeconds);
