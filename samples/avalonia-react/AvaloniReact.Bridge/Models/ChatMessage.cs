namespace AvaloniReact.Bridge.Models;

/// <summary>A single chat message in the conversation history.</summary>
public record ChatMessage(string Id, string Role, string Content, DateTime Timestamp);

/// <summary>Request to send a chat message.</summary>
public record ChatRequest(string Message);

/// <summary>Response from the chat service.</summary>
public record ChatResponse(string Id, string Message, DateTime Timestamp);
