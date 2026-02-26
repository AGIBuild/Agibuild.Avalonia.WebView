namespace Agibuild.Fulora;

/// <summary>
/// Marks an interface whose C# implementation is exposed to JavaScript.
/// The Source Generator (or RuntimeBridgeService) registers RPC handlers for each method,
/// making them callable from JS via <c>window.agWebView.bridge.{ServiceName}.{methodName}()</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class JsExportAttribute : Attribute
{
    /// <summary>
    /// Custom service name used as the RPC namespace prefix.
    /// When <c>null</c>, the name is derived from the interface by removing the leading "I"
    /// (e.g. <c>IAppService</c> → <c>AppService</c>).
    /// </summary>
    public string? Name { get; set; }
}
