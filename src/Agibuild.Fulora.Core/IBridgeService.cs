namespace Agibuild.Fulora;

/// <summary>
/// Type-safe bridge service for exposing C# services to JavaScript and importing JS services into C#.
/// <para>
/// Use <see cref="Expose{T}"/> with <see cref="JsExportAttribute"/> interfaces to register
/// C# implementations callable from JS.
/// </para>
/// <para>
/// Use <see cref="GetProxy{T}"/> with <see cref="JsImportAttribute"/> interfaces to obtain
/// a typed C# proxy that calls JS methods.
/// </para>
/// </summary>
public interface IBridgeService
{
    /// <summary>
    /// Registers a C# implementation of a <see cref="JsExportAttribute"/>-marked interface,
    /// making its methods callable from JavaScript via the bridge.
    /// </summary>
    /// <typeparam name="T">An interface decorated with <see cref="JsExportAttribute"/>.</typeparam>
    /// <param name="implementation">The C# object implementing <typeparamref name="T"/>.</param>
    /// <param name="options">Optional per-service bridge options (origin allowlist, etc.).</param>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is not decorated with <see cref="JsExportAttribute"/>,
    /// or the service has already been exposed without being removed first.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The bridge has been disposed.</exception>
    void Expose<T>(T implementation, BridgeOptions? options = null) where T : class;

    /// <summary>
    /// Returns a typed proxy for a <see cref="JsImportAttribute"/>-marked interface.
    /// Each method call on the proxy is forwarded to the corresponding JS implementation via RPC.
    /// </summary>
    /// <typeparam name="T">An interface decorated with <see cref="JsImportAttribute"/>.</typeparam>
    /// <returns>A proxy implementing <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is not decorated with <see cref="JsImportAttribute"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The bridge has been disposed.</exception>
    T GetProxy<T>() where T : class;

    /// <summary>
    /// Removes a previously exposed <see cref="JsExportAttribute"/> service,
    /// unregistering all its RPC handlers.
    /// </summary>
    /// <typeparam name="T">The interface type previously passed to <see cref="Expose{T}"/>.</typeparam>
    /// <exception cref="ObjectDisposedException">The bridge has been disposed.</exception>
    void Remove<T>() where T : class;
}

/// <summary>
/// Per-service options for <see cref="IBridgeService.Expose{T}"/>.
/// </summary>
public sealed class BridgeOptions
{
    /// <summary>
    /// Origin allowlist for this service. When <c>null</c>, inherits from
    /// the global <c>WebMessageBridgeOptions.AllowedOrigins</c>.
    /// </summary>
    public IReadOnlySet<string>? AllowedOrigins { get; init; }

    /// <summary>
    /// Rate limit for this service. When <c>null</c>, no rate limiting is applied.
    /// </summary>
    public RateLimit? RateLimit { get; init; }
}

/// <summary>
/// Defines a sliding-window rate limit: at most <see cref="MaxCalls"/> calls per <see cref="Window"/>.
/// </summary>
public sealed class RateLimit
{
    /// <summary>Maximum number of calls allowed within <see cref="Window"/>.</summary>
    public int MaxCalls { get; }

    /// <summary>Time window for the sliding-window rate limit.</summary>
    public TimeSpan Window { get; }

    /// <summary>Creates a new sliding-window rate limit.</summary>
    /// <param name="maxCalls">Maximum number of calls allowed within <paramref name="window"/>.</param>
    /// <param name="window">Time window for the rate limit.</param>
    public RateLimit(int maxCalls, TimeSpan window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxCalls, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        MaxCalls = maxCalls;
        Window = window;
    }
}
