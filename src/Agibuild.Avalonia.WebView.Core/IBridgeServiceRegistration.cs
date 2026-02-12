using System.Text.Json;

namespace Agibuild.Avalonia.WebView;

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
    public Type InterfaceType { get; }
    public Type RegistrationType { get; }

    public BridgeRegistrationAttribute(Type interfaceType, Type registrationType)
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
    public Type InterfaceType { get; }
    public Type ProxyType { get; }

    public BridgeProxyAttribute(Type interfaceType, Type proxyType)
    {
        InterfaceType = interfaceType;
        ProxyType = proxyType;
    }
}
