using System.Diagnostics;
using System.Runtime.InteropServices;
using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

public class SystemInfoService : ISystemInfoService
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public Task<SystemInfo> GetSystemInfo()
    {
        var info = new SystemInfo(
            OsName: GetOsName(),
            OsVersion: Environment.OSVersion.VersionString,
            DotnetVersion: RuntimeInformation.FrameworkDescription,
            AvaloniaVersion: GetAvaloniaVersion(),
            MachineName: Environment.MachineName,
            ProcessorCount: Environment.ProcessorCount,
            TotalMemoryMb: GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024),
            WebViewEngine: GetWebViewEngine());

        return Task.FromResult(info);
    }

    public Task<RuntimeMetrics> GetRuntimeMetrics()
    {
        var process = Process.GetCurrentProcess();
        var metrics = new RuntimeMetrics(
            WorkingSetMb: Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1),
            GcTotalMemoryMb: Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 1),
            ThreadCount: process.Threads.Count,
            UptimeSeconds: Math.Round((DateTime.UtcNow - StartTime).TotalSeconds, 1));

        return Task.FromResult(metrics);
    }

    private static string GetOsName()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return RuntimeInformation.OSDescription;
    }

    private static string GetAvaloniaVersion()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Avalonia");
        return asm?.GetName().Version?.ToString() ?? "unknown";
    }

    private static string GetWebViewEngine()
    {
        if (OperatingSystem.IsWindows()) return "WebView2 (Chromium)";
        if (OperatingSystem.IsMacOS()) return "WKWebView (WebKit)";
        if (OperatingSystem.IsLinux()) return "WebKitGTK";
        return "unknown";
    }
}
