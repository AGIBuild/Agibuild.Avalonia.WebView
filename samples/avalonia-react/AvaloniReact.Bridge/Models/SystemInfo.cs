using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>Static system and platform information.</summary>
public record SystemInfo(
    [property: JsonPropertyName("osName")] string OsName,
    [property: JsonPropertyName("osVersion")] string OsVersion,
    [property: JsonPropertyName("dotnetVersion")] string DotnetVersion,
    [property: JsonPropertyName("avaloniaVersion")] string AvaloniaVersion,
    [property: JsonPropertyName("machineName")] string MachineName,
    [property: JsonPropertyName("processorCount")] int ProcessorCount,
    [property: JsonPropertyName("totalMemoryMb")] long TotalMemoryMb,
    [property: JsonPropertyName("webViewEngine")] string WebViewEngine);
