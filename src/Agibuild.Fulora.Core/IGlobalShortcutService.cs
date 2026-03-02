namespace Agibuild.Fulora;

/// <summary>
/// Provides OS-level global hotkey registration, lifecycle management, and activation event dispatch.
/// Registered via <c>Bridge.Expose&lt;IGlobalShortcutService&gt;(impl)</c>.
/// </summary>
[JsExport]
public interface IGlobalShortcutService
{
    /// <summary>Registers a global shortcut with the OS.</summary>
    Task<GlobalShortcutResult> Register(GlobalShortcutBinding binding);

    /// <summary>Unregisters a previously registered global shortcut.</summary>
    Task<GlobalShortcutResult> Unregister(string shortcutId);

    /// <summary>Returns whether a shortcut with the given ID is currently registered.</summary>
    Task<bool> IsRegistered(string shortcutId);

    /// <summary>Returns all currently registered shortcut bindings.</summary>
    Task<GlobalShortcutBinding[]> GetRegistered();

    /// <summary>Push event fired when a registered global shortcut is triggered by the user.</summary>
    IBridgeEvent<GlobalShortcutTriggeredEvent> ShortcutTriggered { get; }
}
