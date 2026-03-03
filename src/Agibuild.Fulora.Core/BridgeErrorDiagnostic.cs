namespace Agibuild.Fulora;

/// <summary>
/// Specific bridge call error codes with actionable diagnostic information.
/// </summary>
public enum BridgeErrorCode
{
    /// <summary>Unknown or unclassified error.</summary>
    Unknown = -1,
    /// <summary>The requested bridge service is not registered.</summary>
    ServiceNotFound = 1001,
    /// <summary>The requested method does not exist on the service.</summary>
    MethodNotFound = 1002,
    /// <summary>Parameters do not match the expected method signature.</summary>
    ParameterMismatch = 1003,
    /// <summary>JSON serialization or deserialization failed.</summary>
    SerializationError = 1004,
    /// <summary>An unhandled exception occurred during method invocation.</summary>
    InvocationError = 1005,
    /// <summary>The bridge call exceeded the configured timeout.</summary>
    TimeoutError = 1006,
    /// <summary>The bridge call was cancelled via CancellationToken / AbortSignal.</summary>
    CancellationError = 1007,
    /// <summary>The caller lacks the required permission for this operation.</summary>
    PermissionDenied = 1008,
    /// <summary>The caller has exceeded the configured rate limit.</summary>
    RateLimitExceeded = 1009,
}

/// <summary>
/// Rich error diagnostic with error code, message, and optional actionable hint.
/// </summary>
public sealed record BridgeErrorDiagnostic(
    BridgeErrorCode Code,
    string Message,
    string? Hint = null)
{
    /// <summary>
    /// Creates a diagnostic for a service not found error.
    /// </summary>
    public static BridgeErrorDiagnostic ServiceNotFound(string serviceName) => new(
        BridgeErrorCode.ServiceNotFound,
        $"Service '{serviceName}' is not registered.",
        $"Did you forget to call bridge.Expose<I{serviceName}>() or bridge.UsePlugin<...>()?");

    /// <summary>Creates a diagnostic for a method not found error.</summary>
    public static BridgeErrorDiagnostic MethodNotFound(string serviceName, string methodName) => new(
        BridgeErrorCode.MethodNotFound,
        $"Method '{methodName}' not found on service '{serviceName}'.",
        $"Check that '{methodName}' is declared on the [JsExport] interface for {serviceName} and the service is re-exposed after changes.");

    /// <summary>Creates a diagnostic for a parameter mismatch error.</summary>
    public static BridgeErrorDiagnostic ParameterMismatch(string serviceName, string methodName, string details) => new(
        BridgeErrorCode.ParameterMismatch,
        $"Parameter mismatch calling '{serviceName}.{methodName}': {details}",
        "Verify the TypeScript call signature matches the C# interface. Re-run 'fulora generate' to update types.");

    /// <summary>Creates a diagnostic for a JSON serialization error.</summary>
    public static BridgeErrorDiagnostic SerializationError(string serviceName, string methodName, string details) => new(
        BridgeErrorCode.SerializationError,
        $"Serialization error for '{serviceName}.{methodName}': {details}",
        $"For {serviceName}.{methodName}: check that all parameter and return types are JSON-serializable. Ensure the [JsonSerializable] context includes these types.");

    /// <summary>Creates a diagnostic for a method invocation error.</summary>
    public static BridgeErrorDiagnostic InvocationError(string serviceName, string methodName, string exceptionMessage) => new(
        BridgeErrorCode.InvocationError,
        $"Error invoking '{serviceName}.{methodName}': {exceptionMessage}",
        null);

    /// <summary>Creates a diagnostic for a call timeout error.</summary>
    public static BridgeErrorDiagnostic Timeout(string serviceName, string methodName) => new(
        BridgeErrorCode.TimeoutError,
        $"Call to '{serviceName}.{methodName}' timed out.",
        $"For {serviceName}.{methodName}: consider increasing the timeout or investigating why the method is taking too long.");

    /// <summary>Creates a diagnostic for a cancelled request.</summary>
    public static BridgeErrorDiagnostic Cancellation(string serviceName, string methodName) => new(
        BridgeErrorCode.CancellationError,
        $"Call to '{serviceName}.{methodName}' was cancelled.",
        null);

    /// <summary>
    /// Maps to JSON-RPC error code in the range -32000 to -32099 for server implementation-defined errors.
    /// </summary>
    internal static int ToJsonRpcCode(BridgeErrorCode code) => code switch
    {
        BridgeErrorCode.ServiceNotFound => -32601,
        BridgeErrorCode.MethodNotFound => -32601,
        BridgeErrorCode.ParameterMismatch => -32602,
        BridgeErrorCode.SerializationError => -32602,
        BridgeErrorCode.InvocationError => -32603,
        BridgeErrorCode.TimeoutError => -32603,
        BridgeErrorCode.CancellationError => -32800,
        BridgeErrorCode.PermissionDenied => -32603,
        BridgeErrorCode.RateLimitExceeded => -32029,
        _ => -32603,
    };
}
