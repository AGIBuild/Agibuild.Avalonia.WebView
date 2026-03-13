namespace Agibuild.Fulora.Shell;

/// <summary>
/// No-op provider for platforms that do not support global shortcuts (Wayland, mobile, etc.).
/// </summary>
public sealed class NullGlobalShortcutProvider : IGlobalShortcutPlatformProvider
{
    /// <inheritdoc />
    public bool IsSupported => false;

    /// <inheritdoc />
    public event Action<string>? ShortcutActivated
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool Register(string id, ShortcutKey key, ShortcutModifiers modifiers) => false;

    /// <inheritdoc />
    public bool Unregister(string id) => false;

    /// <inheritdoc />
    public void Dispose() { }
}
