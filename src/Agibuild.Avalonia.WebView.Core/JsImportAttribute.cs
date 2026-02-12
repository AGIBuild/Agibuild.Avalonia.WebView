namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Marks an interface whose methods are implemented in JavaScript.
/// The Source Generator (or RuntimeBridgeService) creates a C# proxy that forwards
/// each call to JS via <c>rpc.InvokeAsync("{ServiceName}.{methodName}", params)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class JsImportAttribute : Attribute
{
    /// <summary>
    /// Custom service name used as the RPC namespace prefix.
    /// When <c>null</c>, the name is derived from the interface by removing the leading "I"
    /// (e.g. <c>IUiController</c> → <c>UiController</c>).
    /// </summary>
    public string? Name { get; set; }
}
