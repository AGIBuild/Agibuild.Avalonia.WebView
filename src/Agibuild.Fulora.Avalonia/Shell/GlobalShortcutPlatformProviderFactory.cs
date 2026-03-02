namespace Agibuild.Fulora.Shell;

/// <summary>
/// Factory selecting the correct <see cref="IGlobalShortcutPlatformProvider"/> for the current platform.
/// </summary>
public static class GlobalShortcutPlatformProviderFactory
{
    /// <summary>
    /// Creates a platform provider: SharpHook-based on supported desktop platforms,
    /// or <see cref="NullGlobalShortcutProvider"/> when global shortcuts are unavailable.
    /// </summary>
    public static IGlobalShortcutPlatformProvider Create()
    {
        var provider = new SharpHookGlobalShortcutProvider();
        return provider.IsSupported ? provider : new NullGlobalShortcutProvider();
    }
}
