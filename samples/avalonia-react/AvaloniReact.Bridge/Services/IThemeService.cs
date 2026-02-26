using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Allows C# to trigger theme changes in the React UI.
/// Demonstrates [JsImport] — C# calls into JavaScript.
/// </summary>
[JsImport]
public interface IThemeService
{
    Task SetTheme(string theme);
}
