using System.Text.Json.Serialization;

namespace AvaloniReact.Bridge.Models;

/// <summary>A single chat message in the conversation history.</summary>
public record ChatMessage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp);

/// <summary>Request to send a chat message.</summary>
public record ChatRequest(
    [property: JsonPropertyName("message")] string Message);

/// <summary>Response from the chat service.</summary>
public record ChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp);
