using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Sample [JsExport] interface demonstrating a local storage service
/// that would be provided by the LocalStorage bridge plugin.
/// In a real app, install the <c>Agibuild.Fulora.Plugin.LocalStorage</c> NuGet
/// and use <c>bridge.UsePlugin&lt;LocalStoragePlugin&gt;();</c> instead.
/// </summary>
[JsExport]
public interface ILocalStorageDemoService
{
    Task<string?> Get(string key);
    Task Set(string key, string value);
    Task Remove(string key);
    Task Clear();
    Task<string[]> GetKeys();
}
