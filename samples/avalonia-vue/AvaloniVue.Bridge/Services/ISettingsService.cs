using Agibuild.Fulora;
using AvaloniVue.Bridge.Models;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Manages application settings with persistence.
/// Demonstrates bidirectional state synchronization via Bridge.
/// </summary>
[JsExport]
public interface ISettingsService
{
    Task<AppSettings> GetSettings();
    Task<AppSettings> UpdateSettings(AppSettings settings);
}
