using AvaloniReact.Bridge.Models;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Echo-style chat service with in-memory history.
/// Generates contextual responses to demonstrate complex type serialization.
/// </summary>
public class ChatService : IChatService
{
    private readonly List<ChatMessage> _history = [];

    public async Task<ChatResponse> SendMessage(ChatRequest request)
    {
        // Small delay to simulate processing
        await Task.Delay(300);

        var userMsg = new ChatMessage(
            Guid.NewGuid().ToString("N"),
            "user",
            request.Message,
            DateTime.UtcNow);
        _history.Add(userMsg);

        var reply = GenerateReply(request.Message);
        var assistantMsg = new ChatMessage(
            reply.Id,
            "assistant",
            reply.Message,
            reply.Timestamp);
        _history.Add(assistantMsg);

        return reply;
    }

    public Task<List<ChatMessage>> GetHistory() =>
        Task.FromResult(_history.ToList());

    public Task ClearHistory()
    {
        _history.Clear();
        return Task.CompletedTask;
    }

    private static ChatResponse GenerateReply(string input)
    {
        var lower = input.ToLowerInvariant().Trim();
        var reply = lower switch
        {
            _ when lower.Contains("hello") || lower.Contains("hi") =>
                "Hello! I'm the Agibuild Bridge demo assistant. I run in C# and communicate with you via JSON-RPC over the WebView bridge. Ask me anything about the system!",
            _ when lower.Contains("time") =>
                $"The current server time is {DateTime.Now:HH:mm:ss} ({TimeZoneInfo.Local.StandardName}).",
            _ when lower.Contains("memory") || lower.Contains("ram") =>
                $"Current process memory: {GC.GetTotalMemory(false) / (1024 * 1024.0):F1} MB managed, {Environment.WorkingSet / (1024 * 1024.0):F1} MB working set.",
            _ when lower.Contains("os") || lower.Contains("system") =>
                $"Running on {Environment.OSVersion} with {Environment.ProcessorCount} processors. .NET {Environment.Version}.",
            _ when lower.Contains("bridge") =>
                "The Bridge uses JSON-RPC 2.0 over WebMessage. C# services marked with [JsExport] are callable from JavaScript, and [JsImport] interfaces let C# call into JS!",
            _ when lower.Contains("help") =>
                "Try asking about: time, memory, os, bridge, or just say hello! This demo shows how C# and JavaScript communicate seamlessly via the Agibuild WebView Bridge.",
            _ =>
                $"You said: \"{input}\"\n\nThis is an echo response from the C# ChatService. The message traveled: React → JSON-RPC → WebMessage → C# Bridge → back to React.",
        };

        return new ChatResponse(
            Guid.NewGuid().ToString("N"),
            reply,
            DateTime.UtcNow);
    }
}
