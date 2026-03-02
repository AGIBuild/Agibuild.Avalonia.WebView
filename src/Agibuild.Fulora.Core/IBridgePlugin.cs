namespace Agibuild.Fulora;

/// <summary>
/// Marker interface for bridge plugins. Implementations provide a static manifest of
/// bridge services that can be registered in one call via <see cref="BridgePluginExtensions.UsePlugin{TPlugin}"/>.
/// <para>
/// Uses C# static abstract members for NativeAOT-safe compile-time discovery.
/// The implementing class is never instantiated — it acts purely as a manifest.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class MyPlugin : IBridgePlugin
/// {
///     public static IEnumerable&lt;BridgePluginServiceDescriptor&gt; GetServices()
///     {
///         yield return BridgePluginServiceDescriptor.Create&lt;IMyService&gt;(
///             sp => new MyServiceImpl());
///     }
/// }
/// // Usage: bridge.UsePlugin&lt;MyPlugin&gt;();
/// </code>
/// </example>
public interface IBridgePlugin
{
    /// <summary>
    /// Returns the service descriptors declared by this plugin.
    /// Each descriptor registers one bridge service.
    /// </summary>
    static abstract IEnumerable<BridgePluginServiceDescriptor> GetServices();
}

/// <summary>
/// Describes a single bridge service provided by a plugin.
/// Use <see cref="Create{T}"/> to create descriptors with compile-time type safety.
/// </summary>
public sealed class BridgePluginServiceDescriptor
{
    /// <summary>The <c>[JsExport]</c> interface type for this service.</summary>
    public Type InterfaceType { get; }

    internal Action<IBridgeService, IServiceProvider?> RegisterAction { get; }

    private BridgePluginServiceDescriptor(Type interfaceType, Action<IBridgeService, IServiceProvider?> registerAction)
    {
        InterfaceType = interfaceType;
        RegisterAction = registerAction;
    }

    /// <summary>
    /// Creates a typed service descriptor. The generic type is captured at compile time,
    /// making this safe for NativeAOT.
    /// </summary>
    /// <typeparam name="T">The <c>[JsExport]</c> interface type.</typeparam>
    /// <param name="factory">Factory that creates the service implementation. Receives an optional service provider for DI.</param>
    /// <param name="options">Optional per-service bridge options.</param>
    public static BridgePluginServiceDescriptor Create<T>(
        Func<IServiceProvider?, T> factory,
        BridgeOptions? options = null) where T : class
        => new(typeof(T), (bridge, sp) => bridge.Expose(factory(sp), options));
}

/// <summary>
/// Extension methods for registering bridge plugins.
/// </summary>
public static class BridgePluginExtensions
{
    /// <summary>
    /// Registers all services declared by <typeparamref name="TPlugin"/> on this bridge.
    /// </summary>
    /// <typeparam name="TPlugin">A type implementing <see cref="IBridgePlugin"/>.</typeparam>
    /// <param name="bridge">The bridge service to register plugin services on.</param>
    /// <param name="serviceProvider">Optional service provider for DI-aware plugin factories.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bridge"/> is null.</exception>
    public static void UsePlugin<TPlugin>(
        this IBridgeService bridge,
        IServiceProvider? serviceProvider = null)
        where TPlugin : IBridgePlugin
    {
        ArgumentNullException.ThrowIfNull(bridge);

        foreach (var descriptor in TPlugin.GetServices())
        {
            descriptor.RegisterAction(bridge, serviceProvider);
        }
    }
}
