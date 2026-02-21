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
    Task<DesktopMenuApplyResult> ApplyMenuModel(DesktopMenuModel model);
    Task<DesktopTrayUpdateResult> UpdateTrayState(DesktopTrayState state);
    Task<DesktopSystemActionResult> ExecuteSystemAction(DesktopSystemAction action);
    Task<DesktopSystemIntegrationEventsResult> DrainSystemIntegrationEvents();
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

public sealed class DesktopMenuModel
{
    public IReadOnlyList<DesktopMenuItem> Items { get; init; } = [];
}

public sealed class DesktopMenuItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyList<DesktopMenuItem> Children { get; init; } = [];
}

public sealed class DesktopTrayState
{
    public bool IsVisible { get; init; }
    public string? Tooltip { get; init; }
    public string? IconPath { get; init; }
}

public enum DesktopSystemAction
{
    Quit = 0,
    Restart = 1,
    FocusMainWindow = 2,
    ShowAbout = 3
}

public sealed class DesktopMenuApplyResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public int AppliedTopLevelItems { get; init; }
    public string? ProfileIdentity { get; init; }
    public string? ProfilePermissionState { get; init; }
    public string? PruningStage { get; init; }
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}

public sealed class DesktopTrayUpdateResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public bool IsVisible { get; init; }
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}

public sealed class DesktopSystemActionResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public DesktopSystemAction Action { get; init; }
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}

public enum DesktopSystemIntegrationEventKind
{
    TrayInteracted = 0,
    MenuItemInvoked = 1
}

public sealed class DesktopSystemIntegrationEvent
{
    public DesktopSystemIntegrationEventKind Kind { get; init; }
    public string? ItemId { get; init; }
    public string? Context { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class DesktopSystemIntegrationEventsResult
{
    public DesktopCapabilityOutcome Outcome { get; init; }
    public IReadOnlyList<DesktopSystemIntegrationEvent> Events { get; init; } = [];
    public string? DenyReason { get; init; }
    public string? Error { get; init; }
}
