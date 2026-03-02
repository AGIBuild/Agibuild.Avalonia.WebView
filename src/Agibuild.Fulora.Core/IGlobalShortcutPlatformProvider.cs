namespace Agibuild.Fulora;

/// <summary>
/// Platform abstraction for OS-level global hotkey registration.
/// Each platform provides its own implementation (Windows RegisterHotKey, macOS CGEvent, etc.).
/// </summary>
public interface IGlobalShortcutPlatformProvider : IDisposable
{
    /// <summary>Whether the current platform supports global shortcut registration.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Registers a global hotkey at the OS level.
    /// Returns true if registration succeeded, false if the key combo is already taken by another app.
    /// </summary>
    bool Register(string id, ShortcutKey key, ShortcutModifiers modifiers);

    /// <summary>Unregisters a previously registered global hotkey.</summary>
    bool Unregister(string id);

    /// <summary>Raised when a registered global hotkey is activated by the user.</summary>
    event Action<string>? ShortcutActivated;
}
