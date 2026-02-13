namespace AvaloniReact.Bridge.Models;

/// <summary>Static system and platform information.</summary>
public record SystemInfo(
    string OsName,
    string OsVersion,
    string DotnetVersion,
    string AvaloniaVersion,
    string MachineName,
    int ProcessorCount,
    long TotalMemoryMb,
    string WebViewEngine);
