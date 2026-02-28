using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Agibuild.Fulora;

/// <summary>
/// Implemented by source-generated registration classes for <see cref="JsExportAttribute"/> interfaces.
/// Provides reflection-free handler registration, JS stub generation, and method metadata.
/// </summary>
/// <typeparam name="T">The <see cref="JsExportAttribute"/>-marked interface type.</typeparam>
public interface IBridgeServiceRegistration<in T> where T : class
{
    /// <summary>The RPC service name (e.g. "AppService").</summary>
    string ServiceName { get; }

    /// <summary>Full RPC method names (e.g. ["AppService.getCurrentUser", "AppService.saveSettings"]).</summary>
    IReadOnlyList<string> MethodNames { get; }

    /// <summary>Registers RPC handlers for all methods, calling the implementation directly (no reflection).</summary>
    void RegisterHandlers(IWebViewRpcService rpc, T implementation);

    /// <summary>Removes all RPC handlers registered by <see cref="RegisterHandlers"/>.</summary>
    void UnregisterHandlers(IWebViewRpcService rpc);

    /// <summary>Disconnects all event channels. Called by Remove.</summary>
    void DisconnectEvents(T implementation) { }

    /// <summary>Returns the JavaScript client stub code for this service.</summary>
    string GetJsStub();
}

/// <summary>
/// Assembly-level attribute linking a <see cref="JsExportAttribute"/> interface to its
/// source-generated <see cref="IBridgeServiceRegistration{T}"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BridgeRegistrationAttribute : Attribute
{
    /// <summary>The <see cref="JsExportAttribute"/> interface type.</summary>
    public Type InterfaceType { get; }

    /// <summary>The source-generated registration type for <see cref="InterfaceType"/>.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type RegistrationType { get; }

    /// <summary>
    /// Creates a new <see cref="BridgeRegistrationAttribute"/> linking an exported interface to its generated registration.
    /// </summary>
    /// <param name="interfaceType">The <see cref="JsExportAttribute"/> interface type.</param>
    /// <param name="registrationType">The generated registration type.</param>
    public BridgeRegistrationAttribute(
        Type interfaceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type registrationType)
    {
        InterfaceType = interfaceType;
        RegistrationType = registrationType;
    }
}

/// <summary>
/// Assembly-level attribute linking a <see cref="JsImportAttribute"/> interface to its
/// source-generated proxy implementation class.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BridgeProxyAttribute : Attribute
{
    /// <summary>The <see cref="JsImportAttribute"/> interface type.</summary>
    public Type InterfaceType { get; }

    /// <summary>The generated proxy type implementing <see cref="InterfaceType"/>.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ProxyType { get; }

    /// <summary>
    /// Creates a new <see cref="BridgeProxyAttribute"/> linking an imported interface to its generated proxy type.
    /// </summary>
    /// <param name="interfaceType">The <see cref="JsImportAttribute"/> interface type.</param>
    /// <param name="proxyType">The generated proxy type.</param>
    public BridgeProxyAttribute(
        Type interfaceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type proxyType)
    {
        InterfaceType = interfaceType;
        ProxyType = proxyType;
    }
}
