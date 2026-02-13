using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Agibuild.Avalonia.WebView;

/// <summary>
/// Runtime implementation of <see cref="IBridgeService"/> that supports both:
/// <list type="bullet">
/// <item>Source-generated registrations (<see cref="IBridgeServiceRegistration{T}"/>) for AOT compatibility</item>
/// <item>Reflection-based fallback for interfaces without a source generator</item>
/// </list>
/// The generator produces assembly-level <see cref="BridgeRegistrationAttribute"/> and
/// <see cref="BridgeProxyAttribute"/> that this service discovers automatically.
/// </summary>
internal sealed class RuntimeBridgeService : IBridgeService, IDisposable
{
    private readonly IWebViewRpcService _rpc;
    private readonly Func<string, Task<string?>> _invokeScript;
    private readonly ILogger _logger;
    private readonly bool _enableDevTools;
    private readonly IBridgeTracer _tracer;

    private readonly ConcurrentDictionary<Type, ExposedService> _exportedServices = new();
    private readonly ConcurrentDictionary<Type, object> _importProxies = new();

    /// <summary>
    /// Shared JSON options: camelCase-insensitive for seamless JS ↔ C# mapping.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private volatile bool _disposed;

    internal RuntimeBridgeService(
        IWebViewRpcService rpc,
        Func<string, Task<string?>> invokeScript,
        ILogger logger,
        bool enableDevTools = false,
        IBridgeTracer? tracer = null)
    {
        _rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
        _invokeScript = invokeScript ?? throw new ArgumentNullException(nameof(invokeScript));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enableDevTools = enableDevTools;
        _tracer = tracer ?? NullBridgeTracer.Instance;
    }

    // ==================== Expose (JsExport) ====================

    public void Expose<T>(T implementation, BridgeOptions? options = null) where T : class
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(implementation);

        var interfaceType = typeof(T);
        ValidateJsExportAttribute(interfaceType);

        if (!_exportedServices.TryAdd(interfaceType, default!))
            throw new InvalidOperationException(
                $"Service '{interfaceType.Name}' has already been exposed. Call Remove<{interfaceType.Name}>() first.");

        try
        {
            // Try source-generated registration first (AOT safe, no reflection).
            var generated = FindGeneratedRegistration<T>();
            if (generated is not null)
            {
                if (options?.RateLimit is not null)
                {
                    // Pass a rate-limiting RPC wrapper so every Handle() call is intercepted.
                    var wrapper = new RateLimitingRpcWrapper(_rpc, options.RateLimit);
                    generated.RegisterHandlers(wrapper, implementation);
                }
                else
                {
                    generated.RegisterHandlers(_rpc, implementation);
                }

                _ = _invokeScript(generated.GetJsStub());
                _exportedServices[interfaceType] = new ExposedService(
                    generated.ServiceName,
                    generated.MethodNames.ToList(),
                    generated.UnregisterHandlers);

                _tracer.OnServiceExposed(generated.ServiceName, generated.MethodNames.Count, isSourceGenerated: true);
                _logger.LogDebug("Bridge: exposed {Service} with {Count} methods (source-generated)",
                    generated.ServiceName, generated.MethodNames.Count);
                return;
            }

            // Fallback: reflection-based registration.
            ExposeViaReflection(implementation, interfaceType, options?.RateLimit);
        }
        catch
        {
            _exportedServices.TryRemove(interfaceType, out _);
            throw;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Reflection-based fallback path; source-generated path is preferred for AOT/trim scenarios.")]
    private void ExposeViaReflection<T>(T implementation, Type interfaceType, RateLimit? rateLimit = null) where T : class
    {
        var serviceName = GetServiceName<JsExportAttribute>(interfaceType);
        var registeredMethods = new List<string>();

        try
        {
            foreach (var method in interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var rpcMethodName = $"{serviceName}.{ToCamelCase(method.Name)}";
                var handler = CreateHandler(method, implementation);
                if (rateLimit is not null)
                    handler = WrapWithRateLimit(handler, rateLimit);
                _rpc.Handle(rpcMethodName, handler);
                registeredMethods.Add(rpcMethodName);
            }

            var jsStub = GenerateJsStub(serviceName, interfaceType);
            _ = _invokeScript(jsStub);

            _exportedServices[interfaceType] = new ExposedService(serviceName, registeredMethods);

            _tracer.OnServiceExposed(serviceName, registeredMethods.Count, isSourceGenerated: false);
            _logger.LogDebug("Bridge: exposed {Service} with {Count} methods (reflection)",
                serviceName, registeredMethods.Count);
        }
        catch
        {
            foreach (var m in registeredMethods)
                _rpc.RemoveHandler(m);
            throw;
        }
    }

    // ==================== GetProxy (JsImport) ====================

    public T GetProxy<T>() where T : class
    {
        ThrowIfDisposed();

        var interfaceType = typeof(T);
        ValidateJsImportAttribute(interfaceType);

        return (T)_importProxies.GetOrAdd(interfaceType, _ =>
        {
            // Try source-generated proxy first (AOT safe, no DispatchProxy).
            var generatedProxy = FindAndCreateGeneratedProxy<T>();
            if (generatedProxy is not null)
            {
                _logger.LogDebug("Bridge: using source-generated proxy for {Service}", typeof(T).Name);
                return generatedProxy;
            }

            // Fallback: DispatchProxy-based.
            return CreateImportProxy<T>();
        });
    }

    // ==================== Remove ====================

    public void Remove<T>() where T : class
    {
        ThrowIfDisposed();

        if (_exportedServices.TryRemove(typeof(T), out var service))
        {
            if (service.GeneratedUnregister is not null)
            {
                service.GeneratedUnregister(_rpc);
            }
            else
            {
                foreach (var method in service.RegisteredMethods)
                    _rpc.RemoveHandler(method);
            }

            _tracer.OnServiceRemoved(service.ServiceName);
            _logger.LogDebug("Bridge: removed {Service}", service.ServiceName);
        }
    }

    // ==================== Disposal ====================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _exportedServices)
        {
            foreach (var method in kvp.Value.RegisteredMethods)
                _rpc.RemoveHandler(method);
        }
        _exportedServices.Clear();
        _importProxies.Clear();

        _logger.LogDebug("Bridge: disposed");
    }

    // ==================== Private helpers ====================

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Task<T>.Result property is guaranteed to exist by the runtime.")]
    private Func<JsonElement?, Task<object?>> CreateHandler(MethodInfo method, object target)
    {
        var parameters = method.GetParameters();

        return async args =>
        {
            try
            {
                var invokeArgs = DeserializeParameters(parameters, args);
                var result = method.Invoke(target, invokeArgs);

                // Handle Task and Task<T> return types.
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);

                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        // Task<T> — extract the Result.
                        var resultProperty = taskType.GetProperty("Result");
                        return resultProperty?.GetValue(task);
                    }

                    // Task (void) — no return value.
                    return null;
                }

                return result;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                // Unwrap the reflection-induced wrapper.
                throw ex.InnerException;
            }
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Reflection-based fallback; source-generated path is preferred for AOT/trim.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Value types always have a parameterless constructor.")]
    private static object?[] DeserializeParameters(ParameterInfo[] parameters, JsonElement? args)
    {
        if (parameters.Length == 0)
            return [];

        if (args is null || args.Value.ValueKind == JsonValueKind.Null || args.Value.ValueKind == JsonValueKind.Undefined)
        {
            // All parameters must be optional or nullable for this to work.
            var defaults = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                defaults[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }
            return defaults;
        }

        // Named parameters (object format).
        if (args.Value.ValueKind == JsonValueKind.Object)
        {
            var result = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var camelName = ToCamelCase(p.Name!);

                if (args.Value.TryGetProperty(camelName, out var prop))
                {
                    result[i] = prop.Deserialize(p.ParameterType, JsonOptions);
                }
                else if (args.Value.TryGetProperty(p.Name!, out var exactProp))
                {
                    // Fallback: exact name match.
                    result[i] = exactProp.Deserialize(p.ParameterType, JsonOptions);
                }
                else if (p.HasDefaultValue)
                {
                    result[i] = p.DefaultValue;
                }
                else
                {
                    result[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                }
            }
            return result;
        }

        // Positional parameters (array format) — fallback.
        if (args.Value.ValueKind == JsonValueKind.Array)
        {
            var result = new object?[parameters.Length];
            int i = 0;
            foreach (var element in args.Value.EnumerateArray())
            {
                if (i >= parameters.Length) break;
                result[i] = element.Deserialize(parameters[i].ParameterType, JsonOptions);
                i++;
            }
            // Fill remaining with defaults.
            for (; i < parameters.Length; i++)
            {
                result[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }
            return result;
        }

        // Single parameter shorthand.
        if (parameters.Length == 1)
        {
            return [args.Value.Deserialize(parameters[0].ParameterType, JsonOptions)];
        }

        return new object?[parameters.Length];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2091",
        Justification = "DispatchProxy fallback; source-generated proxy is preferred for AOT/trim.")]
    private T CreateImportProxy<T>() where T : class
    {
        var interfaceType = typeof(T);
        var serviceName = GetServiceName<JsImportAttribute>(interfaceType);

        var proxy = DispatchProxy.Create<T, BridgeImportProxy>();
        var bridgeProxy = (BridgeImportProxy)(object)proxy;
        bridgeProxy.Initialize(_rpc, serviceName);

        _logger.LogDebug("Bridge: created import proxy for {Service}", serviceName);
        return proxy;
    }

    private static string GenerateJsStub(
        string serviceName,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type interfaceType)
    {
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var methodLines = new List<string>();

        foreach (var m in methods)
        {
            var camelName = ToCamelCase(m.Name);
            methodLines.Add(
                $"        {camelName}: function(params) {{ return window.agWebView.rpc.invoke('{serviceName}.{camelName}', params); }}");
        }

        return $$"""
            (function() {
                if (!window.agWebView) window.agWebView = {};
                if (!window.agWebView.bridge) window.agWebView.bridge = {};
                window.agWebView.bridge.{{serviceName}} = {
            {{string.Join(",\n", methodLines)}}
                };
            })();
            """;
    }

    private static string GetServiceName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttr>(Type interfaceType) where TAttr : Attribute
    {
        var attr = interfaceType.GetCustomAttribute<TAttr>();
        var nameProperty = typeof(TAttr).GetProperty("Name");
        var customName = nameProperty?.GetValue(attr) as string;

        if (!string.IsNullOrEmpty(customName))
            return customName;

        var name = interfaceType.Name;
        return name.StartsWith('I') && name.Length > 1 && char.IsUpper(name[1])
            ? name[1..]
            : name;
    }

    internal static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static void ValidateJsExportAttribute(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
            throw new InvalidOperationException(
                $"Type '{interfaceType.Name}' must be an interface to use with Bridge.Expose<T>().");

        if (interfaceType.GetCustomAttribute<JsExportAttribute>() is null)
            throw new InvalidOperationException(
                $"Interface '{interfaceType.Name}' must be decorated with [JsExport] to use with Bridge.Expose<T>().");
    }

    private static void ValidateJsImportAttribute(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
            throw new InvalidOperationException(
                $"Type '{interfaceType.Name}' must be an interface to use with Bridge.GetProxy<T>().");

        if (interfaceType.GetCustomAttribute<JsImportAttribute>() is null)
            throw new InvalidOperationException(
                $"Interface '{interfaceType.Name}' must be decorated with [JsImport] to use with Bridge.GetProxy<T>().");
    }

    // ==================== Source-generated code discovery ====================

    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "RegistrationType is a source-generated type known to have a parameterless constructor.")]
    private static IBridgeServiceRegistration<T>? FindGeneratedRegistration<T>() where T : class
    {
        var interfaceType = typeof(T);

        // Scan calling assembly for [assembly: BridgeRegistration(typeof(T), typeof(Reg))]
        foreach (var assembly in new[] { interfaceType.Assembly, Assembly.GetCallingAssembly() })
        {
            foreach (var attr in assembly.GetCustomAttributes<BridgeRegistrationAttribute>())
            {
                if (attr.InterfaceType == interfaceType)
                {
                    return (IBridgeServiceRegistration<T>)Activator.CreateInstance(attr.RegistrationType)!;
                }
            }
        }

        return null;
    }

    private static T? FindGeneratedProxy<T>() where T : class
    {
        var interfaceType = typeof(T);

        foreach (var assembly in new[] { interfaceType.Assembly, Assembly.GetCallingAssembly() })
        {
            foreach (var attr in assembly.GetCustomAttributes<BridgeProxyAttribute>())
            {
                if (attr.InterfaceType == interfaceType)
                {
                    // Generated proxy has a constructor taking IWebViewRpcService.
                    // But we don't have the RPC reference here — need to pass it in.
                    return null; // Handled separately below.
                }
            }
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "ProxyType is a source-generated type known to have a constructor taking IWebViewRpcService.")]
    private T? FindAndCreateGeneratedProxy<T>() where T : class
    {
        var interfaceType = typeof(T);

        foreach (var assembly in new[] { interfaceType.Assembly, Assembly.GetCallingAssembly() })
        {
            foreach (var attr in assembly.GetCustomAttributes<BridgeProxyAttribute>())
            {
                if (attr.InterfaceType == interfaceType)
                {
                    return (T)Activator.CreateInstance(attr.ProxyType, _rpc)!;
                }
            }
        }

        return null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RuntimeBridgeService));
    }

    // ==================== Rate limiter ====================

    /// <summary>
    /// Wraps an RPC handler with sliding-window rate limiting.
    /// Returns JSON-RPC error -32029 ("Rate limit exceeded") when throttled.
    /// </summary>
    private static Func<JsonElement?, Task<object?>> WrapWithRateLimit(
        Func<JsonElement?, Task<object?>> inner, RateLimit limit)
    {
        var timestamps = new Queue<long>();
        var windowTicks = limit.Window.Ticks;

        return args =>
        {
            var now = Environment.TickCount64;

            lock (timestamps)
            {
                // Evict expired entries.
                while (timestamps.Count > 0 && now - timestamps.Peek() > windowTicks / TimeSpan.TicksPerMillisecond)
                    timestamps.Dequeue();

                if (timestamps.Count >= limit.MaxCalls)
                    throw new WebViewRpcException(-32029, "Rate limit exceeded");

                timestamps.Enqueue(now);
            }

            return inner(args);
        };
    }

    // ==================== Inner types ====================

    private sealed record ExposedService(string ServiceName, List<string> RegisteredMethods, Action<IWebViewRpcService>? GeneratedUnregister = null);

    /// <summary>
    /// Wraps an <see cref="IWebViewRpcService"/> to apply rate limiting to every handler registered through it.
    /// Passes all other operations (RemoveHandler, Invoke) directly through to the inner service.
    /// </summary>
    private sealed class RateLimitingRpcWrapper : IWebViewRpcService
    {
        private readonly IWebViewRpcService _inner;
        private readonly RateLimit _limit;

        public RateLimitingRpcWrapper(IWebViewRpcService inner, RateLimit limit)
        {
            _inner = inner;
            _limit = limit;
        }

        public void Handle(string method, Func<JsonElement?, Task<object?>> handler)
            => _inner.Handle(method, WrapWithRateLimit(handler, _limit));

        public void Handle(string method, Func<JsonElement?, object?> handler)
        {
            // Convert sync handler to async, wrap with rate limit, register.
            Func<JsonElement?, Task<object?>> asyncHandler = args => Task.FromResult(handler(args));
            _inner.Handle(method, WrapWithRateLimit(asyncHandler, _limit));
        }

        public void RemoveHandler(string method) => _inner.RemoveHandler(method);
        public Task<JsonElement> InvokeAsync(string method, object? args = null) => _inner.InvokeAsync(method, args);
        public Task<T?> InvokeAsync<T>(string method, object? args = null) => _inner.InvokeAsync<T>(method, args);
    }
}

/// <summary>
/// <see cref="DispatchProxy"/> implementation for <see cref="JsImportAttribute"/> interfaces.
/// Routes every method call to the RPC service.
/// </summary>
public class BridgeImportProxy : DispatchProxy
{
    private IWebViewRpcService? _rpc;
    private string _serviceName = "";

    internal void Initialize(IWebViewRpcService rpc, string serviceName)
    {
        _rpc = rpc;
        _serviceName = serviceName;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (_rpc is null || targetMethod is null)
            throw new InvalidOperationException("Bridge proxy has not been initialized.");

        var methodName = $"{_serviceName}.{RuntimeBridgeService.ToCamelCase(targetMethod.Name)}";
        var parameters = targetMethod.GetParameters();

        // Build named params object.
        Dictionary<string, object?>? namedParams = null;
        if (args is not null && args.Length > 0)
        {
            namedParams = new Dictionary<string, object?>(args.Length);
            for (int i = 0; i < args.Length && i < parameters.Length; i++)
            {
                namedParams[RuntimeBridgeService.ToCamelCase(parameters[i].Name!)] = args[i];
            }
        }

        var returnType = targetMethod.ReturnType;

        // Task (void)
        if (returnType == typeof(Task))
        {
            return _rpc.InvokeAsync(methodName, namedParams);
        }

        // Task<T>
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var invokeMethod = typeof(IWebViewRpcService)
                .GetMethod(nameof(IWebViewRpcService.InvokeAsync), 1, [typeof(string), typeof(object)])!
                .MakeGenericMethod(resultType);
            return invokeMethod.Invoke(_rpc, [methodName, namedParams]);
        }

        // Synchronous return — wrap in InvokeAsync.
        var task = _rpc.InvokeAsync(methodName, namedParams);
        // For non-async methods, block (not ideal but the interface contract should use Task).
        task.GetAwaiter().GetResult();
        return null;
    }
}
