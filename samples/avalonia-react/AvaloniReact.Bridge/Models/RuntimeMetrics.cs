using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>Live runtime metrics that change over time.</summary>
public record RuntimeMetrics(
    [property: JsonPropertyName("workingSetMb")] double WorkingSetMb,
    [property: JsonPropertyName("gcTotalMemoryMb")] double GcTotalMemoryMb,
    [property: JsonPropertyName("threadCount")] int ThreadCount,
    [property: JsonPropertyName("uptimeSeconds")] double UptimeSeconds);
