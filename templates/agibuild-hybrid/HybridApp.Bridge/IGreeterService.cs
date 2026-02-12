using Agibuild.Avalonia.WebView;

namespace HybridApp.Bridge;

/// <summary>
/// C# service exposed to JavaScript.
/// JS can call: bridge.invoke("GreeterService.Greet", { name: "World" })
/// </summary>
[JsExport]
public interface IGreeterService
{
    Task<string> Greet(string name);
}

/// <summary>
/// JavaScript service callable from C#.
/// C# can call: proxy.ShowNotification("Hello!")
/// </summary>
[JsImport]
public interface INotificationService
{
    Task ShowNotification(string message);
}
