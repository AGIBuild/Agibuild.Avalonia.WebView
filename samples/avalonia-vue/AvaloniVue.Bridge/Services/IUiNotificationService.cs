using Agibuild.Fulora;

namespace AvaloniVue.Bridge.Services;

/// <summary>
/// Allows C# to trigger toast notifications in the Vue UI.
/// Demonstrates [JsImport] — C# calls into JavaScript.
/// </summary>
[JsImport]
public interface IUiNotificationService
{
    Task ShowNotification(string message, string type);
}
