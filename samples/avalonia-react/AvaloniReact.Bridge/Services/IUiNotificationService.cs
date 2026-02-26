using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Allows C# to trigger toast notifications in the React UI.
/// Demonstrates [JsImport] — C# calls into JavaScript.
/// </summary>
[JsImport]
public interface IUiNotificationService
{
    Task ShowNotification(string message, string type);
}
