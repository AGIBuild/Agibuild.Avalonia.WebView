namespace Agibuild.Fulora.Adapters.Abstractions;

internal readonly record struct WebViewBackendCapabilities(
    IDragDropAdapter? DragDrop,
    IAsyncPreloadScriptAdapter? AsyncPreloadScript)
{
    public static WebViewBackendCapabilities None => default;
}
