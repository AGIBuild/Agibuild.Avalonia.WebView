using Agibuild.Fulora;
using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Exposes native system and runtime information inaccessible from web content.
/// Demonstrates [JsExport] with complex return types.
/// </summary>
[JsExport]
public interface ISystemInfoService
{
    Task<SystemInfo> GetSystemInfo();
    Task<RuntimeMetrics> GetRuntimeMetrics();
}
