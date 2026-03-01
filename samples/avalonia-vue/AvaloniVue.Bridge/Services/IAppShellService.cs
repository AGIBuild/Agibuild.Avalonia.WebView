using Agibuild.Fulora;
using AvaloniVue.Bridge.Models;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Provides application metadata and dynamic page registry.
/// The Vue frontend queries this service on startup to build navigation.
/// </summary>
[JsExport]
public interface IAppShellService
{
    Task<List<PageDefinition>> GetPages();
    Task<AppInfo> GetAppInfo();
}
