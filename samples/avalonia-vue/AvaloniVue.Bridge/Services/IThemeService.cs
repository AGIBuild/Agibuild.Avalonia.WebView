using Agibuild.Fulora;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Allows C# to trigger theme changes in the Vue UI.
/// Demonstrates [JsImport] — C# calls into JavaScript.
/// </summary>
[JsImport]
public interface IThemeService
{
    Task SetTheme(string theme);
}
