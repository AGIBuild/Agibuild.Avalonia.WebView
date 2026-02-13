using Agibuild.Avalonia.WebView;
using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Echo-style chat service demonstrating bidirectional communication
/// with complex types (message history, timestamps).
/// </summary>
[JsExport]
public interface IChatService
{
    Task<ChatResponse> SendMessage(ChatRequest request);
    Task<List<ChatMessage>> GetHistory();
    Task ClearHistory();
}
