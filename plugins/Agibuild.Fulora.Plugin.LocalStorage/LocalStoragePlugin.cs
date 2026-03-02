using Agibuild.Fulora;

namespace Agibuild.Fulora.Plugin.LocalStorage;

/// <summary>
/// Bridge plugin manifest for the LocalStorage service.
/// Register with: <c>bridge.UsePlugin&lt;LocalStoragePlugin&gt;();</c>
/// </summary>
public sealed class LocalStoragePlugin : IBridgePlugin
{
    public static IEnumerable<BridgePluginServiceDescriptor> GetServices()
    {
        yield return BridgePluginServiceDescriptor.Create<ILocalStorageService>(
            _ => new LocalStorageService());
    }
}
