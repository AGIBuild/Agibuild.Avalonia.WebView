namespace Agibuild.Fulora.Plugin.HttpClient;

/// <summary>
/// HTTP response DTO returned by <see cref="IHttpClientService"/> methods.
/// </summary>
public sealed class HttpBridgeResponse
{
    /// <summary>HTTP status code (e.g., 200, 404).</summary>
    public int StatusCode { get; init; }
    /// <summary>Response body as string.</summary>
    public string? Body { get; init; }
    /// <summary>Response headers (content + message headers merged).</summary>
    public Dictionary<string, string> Headers { get; init; } = new();
    /// <summary>Whether the response indicates success (2xx status).</summary>
    public bool IsSuccess { get; init; }
}
