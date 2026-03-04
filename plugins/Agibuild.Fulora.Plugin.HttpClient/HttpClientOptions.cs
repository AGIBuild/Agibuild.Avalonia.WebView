namespace Agibuild.Fulora.Plugin.HttpClient;

/// <summary>
/// Configuration options for <see cref="HttpClientService"/>.
/// </summary>
public sealed class HttpClientOptions
{
    /// <summary>Base URL for resolving relative request URLs.</summary>
    public string? BaseUrl { get; init; }
    /// <summary>Request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    /// <summary>Default headers applied to every request.</summary>
    public Dictionary<string, string> DefaultHeaders { get; init; } = new();
    /// <summary>Request interceptors run in order before sending.</summary>
    public IReadOnlyList<IHttpRequestInterceptor> Interceptors { get; init; } = [];
}

/// <summary>
/// Request interceptor that can modify outgoing HTTP requests.
/// </summary>
public interface IHttpRequestInterceptor
{
    /// <summary>Intercepts and optionally modifies the outgoing request.</summary>
    Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request);
}

/// <summary>
/// Provides auth tokens for request header injection.
/// </summary>
public interface IAuthTokenProvider
{
    /// <summary>Returns the current auth token for header injection.</summary>
    Task<string?> GetTokenAsync();
}
