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

/// <summary>
/// Typed desktop-host capability service exposed to JavaScript.
/// </summary>
[JsExport]
public interface IDesktopHostService
{
    Task<DesktopClipboardProbeResult> ReadClipboardText();
    Task<DesktopClipboardWriteResult> WriteClipboardText(string text);
}

public enum DesktopCapabilityOutcome
{
    Allow = 0,
    Deny = 1,
    Failure = 2
}

public sealed class DesktopClipboardProbeResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public string? ClipboardText { get; init; }
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}

public sealed class DesktopClipboardWriteResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}
