namespace Agibuild.Fulora.Shell;

/// <summary>
/// No-op provider for platforms that do not support global shortcuts (Wayland, mobile, etc.).
/// </summary>
public sealed class NullGlobalShortcutProvider : IGlobalShortcutPlatformProvider
{
    public bool IsSupported => false;

    public event Action<string>? ShortcutActivated
    {
        add { }
        remove { }
    }

    public bool Register(string id, ShortcutKey key, ShortcutModifiers modifiers) => false;
    public bool Unregister(string id) => false;
    public void Dispose() { }
}
