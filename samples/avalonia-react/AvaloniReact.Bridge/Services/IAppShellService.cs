using Agibuild.Avalonia.WebView;
using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Provides application metadata and dynamic page registry.
/// The React frontend queries this service on startup to build navigation.
/// </summary>
[JsExport]
public interface IAppShellService
{
    Task<List<PageDefinition>> GetPages();
    Task<AppInfo> GetAppInfo();
}
