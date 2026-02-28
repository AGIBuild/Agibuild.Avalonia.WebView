using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Agibuild.Fulora;

/// <summary>
/// JSON-RPC 2.0 service for bidirectional JS ↔ C# method calls over the WebMessage bridge.
/// </summary>
internal sealed class WebViewRpcService : IWebViewRpcService
{
    /// <summary>
    /// Shared JSON options for bridge payload serialization: camelCase naming + case-insensitive deserialization.
    /// RPC envelope types (RpcRequest, RpcResponse, etc.) use source-generated RpcJsonContext and are unaffected.
    /// </summary>
    private static readonly JsonSerializerOptions BridgeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ConcurrentDictionary<string, Func<JsonElement?, Task<object?>>> _handlers = new();
    private readonly ConcurrentDictionary<string, Func<JsonElement?, CancellationToken, Task<object?>>> _cancellableHandlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingCalls = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCancellations = new();
    private readonly ConcurrentDictionary<string, ActiveEnumerator> _activeEnumerators = new();
    private readonly Func<string, Task<string?>> _invokeScript;
    private readonly ILogger _logger;

    internal WebViewRpcService(Func<string, Task<string?>> invokeScript, ILogger logger)
    {
        _invokeScript = invokeScript;
        _logger = logger;
    }

    // ==================== Handler registration ====================

    public void Handle(string method, Func<JsonElement?, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[method] = handler;
    }

    public void Handle(string method, Func<JsonElement?, object?> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[method] = args => Task.FromResult(handler(args));
    }

    public void Handle(string method, Func<JsonElement?, CancellationToken, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(handler);
        _cancellableHandlers[method] = handler;
        _handlers[method] = args => handler(args, CancellationToken.None);
    }

    public void RemoveHandler(string method)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        _handlers.TryRemove(method, out _);
        _cancellableHandlers.TryRemove(method, out _);
    }

    // ==================== Cancellation support ====================

    internal void RegisterCancellation(string requestId, CancellationTokenSource cts)
    {
        _activeCancellations[requestId] = cts;
    }

    internal void UnregisterCancellation(string requestId)
    {
        if (_activeCancellations.TryRemove(requestId, out var cts))
        {
            cts.Dispose();
        }
    }

    // ==================== Enumerator support ====================

    internal void RegisterEnumerator(string token, Func<Task<(object? Value, bool Finished)>> moveNext, Func<Task> dispose)
    {
        _activeEnumerators[token] = new ActiveEnumerator(moveNext, dispose);
        _handlers[$"$/enumerator/next/{token}"] = async (JsonElement? args) =>
        {
            if (_activeEnumerators.TryGetValue(token, out var enumerator))
            {
                var (value, finished) = await enumerator.MoveNext();
                if (finished)
                {
                    await DisposeEnumerator(token);
                }
                return new EnumeratorNextResult { Values = finished ? [] : [value], Finished = finished };
            }
            return new EnumeratorNextResult { Values = [], Finished = true };
        };
    }

    internal async Task DisposeEnumerator(string token)
    {
        if (_activeEnumerators.TryRemove(token, out var enumerator))
        {
            _handlers.TryRemove($"$/enumerator/next/{token}", out _);
            try
            {
                await enumerator.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "RPC: failed to dispose enumerator {Token}", token);
            }
        }
    }

    // ==================== C# → JS calls ====================

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "RPC args serialization uses runtime types; callers are responsible for ensuring types are preserved.")]
    public async Task<JsonElement> InvokeAsync(string method, object? args = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);

        var id = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCalls[id] = tcs;

        try
        {
            var request = new RpcRequest
            {
                Id = id,
                Method = method,
                Params = args is null ? null : JsonSerializer.SerializeToElement(args, BridgeJsonOptions)
            };

            var json = JsonSerializer.Serialize(request, RpcJsonContext.Default.RpcRequest);
            // Send via injected JS runtime: window.agWebView.rpc._dispatch(json)
            var script = $"window.agWebView && window.agWebView.rpc && window.agWebView.rpc._dispatch({JsonSerializer.Serialize(json, RpcJsonContext.Default.String)})";
            await _invokeScript(script);

            // Wait for response (with timeout)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetException(
                new WebViewRpcException(-32000, $"RPC call '{method}' timed out.")));

            return await tcs.Task;
        }
        finally
        {
            _pendingCalls.TryRemove(id, out _);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Generic RPC deserialization; callers are responsible for ensuring T is preserved.")]
    public async Task<T?> InvokeAsync<T>(string method, object? args = null)
    {
        var result = await InvokeAsync(method, args);
        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
            return default;
        return result.Deserialize<T>(BridgeJsonOptions);
    }

    // ==================== JS → C# dispatch ====================

    /// <summary>
    /// Called by WebViewCore when a WebMessage with RPC envelope is received.
    /// Returns true if the message was handled as an RPC message.
    /// </summary>
    internal bool TryProcessMessage(string body)
    {
        if (string.IsNullOrEmpty(body)) return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("jsonrpc", out var ver) || ver.GetString() != "2.0")
                return false;

            // Handle notifications (no id field at all) — $/cancelRequest and $/enumerator/abort
            if (!root.TryGetProperty("id", out var idProp))
            {
                if (root.TryGetProperty("method", out var notifMethod))
                {
                    var methodName = notifMethod.GetString();
                    if (methodName == "$/cancelRequest" && root.TryGetProperty("params", out var cancelParams))
                    {
                        if (cancelParams.TryGetProperty("id", out var cancelId))
                        {
                            var targetId = cancelId.GetString();
                            if (targetId is not null && _activeCancellations.TryGetValue(targetId, out var cts))
                            {
                                cts.Cancel();
                            }
                        }
                        return true;
                    }

                    if (methodName == "$/enumerator/abort" && root.TryGetProperty("params", out var abortParams))
                    {
                        if (abortParams.TryGetProperty("token", out var tokenProp))
                        {
                            var token = tokenProp.GetString();
                            if (token is not null)
                            {
                                _ = DisposeEnumerator(token);
                            }
                        }
                        return true;
                    }
                }
                return false;
            }

            // Is it a response to a pending C#→JS call?
            {
                var id = idProp.GetString();
                if (id is not null && _pendingCalls.TryRemove(id, out var tcs))
                {
                    if (root.TryGetProperty("error", out var errorProp))
                    {
                        var code = errorProp.TryGetProperty("code", out var c) ? c.GetInt32() : -32603;
                        var msg = errorProp.TryGetProperty("message", out var m) ? m.GetString() ?? "RPC error" : "RPC error";
                        tcs.TrySetException(new WebViewRpcException(code, msg));
                    }
                    else if (root.TryGetProperty("result", out var resultProp))
                    {
                        tcs.TrySetResult(resultProp.Clone());
                    }
                    else
                    {
                        tcs.TrySetResult(default);
                    }
                    return true;
                }

                // It's a JS→C# request
                if (root.TryGetProperty("method", out var methodProp))
                {
                    var method = methodProp.GetString();
                    if (method is not null)
                    {
                        _ = DispatchRequestAsync(id!, method, root);
                        return true;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "RPC: failed to parse message");
        }

        return false;
    }

    private async Task DispatchRequestAsync(string id, string method, JsonElement root)
    {
        JsonElement? paramsProp = root.TryGetProperty("params", out var p) ? p.Clone() : null;

        try
        {
            if (_cancellableHandlers.TryGetValue(method, out var cancellableHandler))
            {
                using var cts = new CancellationTokenSource();
                RegisterCancellation(id, cts);
                try
                {
                    var result = await cancellableHandler(paramsProp, cts.Token);
                    await SendSuccessResponseAsync(id, result);
                }
                finally
                {
                    UnregisterCancellation(id);
                }
                return;
            }

            if (!_handlers.TryGetValue(method, out var handler))
            {
                await SendErrorResponseAsync(id, -32601, $"Method not found: {method}");
                return;
            }

            var handlerResult = await handler(paramsProp);
            await SendSuccessResponseAsync(id, handlerResult);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("RPC: handler for '{Method}' was cancelled", method);
            await SendErrorResponseAsync(id, -32800, "Request cancelled");
        }
        catch (WebViewRpcException rpcEx)
        {
            _logger.LogDebug(rpcEx, "RPC: handler for '{Method}' threw RPC error {Code}", method, rpcEx.Code);
            await SendErrorResponseAsync(id, rpcEx.Code, rpcEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RPC: handler for '{Method}' threw", method);
            await SendErrorResponseAsync(id, -32603, ex.Message);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "RPC result serialization uses runtime types; the handler is responsible for type safety.")]
    private async Task SendSuccessResponseAsync(string id, object? result)
    {
        var response = new RpcResponse
        {
            Id = id,
            Result = result is null ? null : JsonSerializer.SerializeToElement(result, BridgeJsonOptions)
        };
        var json = JsonSerializer.Serialize(response, RpcJsonContext.Default.RpcResponse);
        var script = $"window.agWebView && window.agWebView.rpc && window.agWebView.rpc._onResponse({JsonSerializer.Serialize(json, RpcJsonContext.Default.String)})";
        await _invokeScript(script);
    }

    private async Task SendErrorResponseAsync(string id, int code, string message)
    {
        var response = new RpcErrorResponse
        {
            Id = id,
            Error = new RpcError { Code = code, Message = message }
        };
        var json = JsonSerializer.Serialize(response, RpcJsonContext.Default.RpcErrorResponse);
        var script = $"window.agWebView && window.agWebView.rpc && window.agWebView.rpc._onResponse({JsonSerializer.Serialize(json, RpcJsonContext.Default.String)})";
        await _invokeScript(script);
    }

    // ==================== JS stub injection ====================

    internal const string JsStub = """
        (function() {
            if (window.agWebView && window.agWebView.rpc) return;
            if (!window.agWebView) window.agWebView = {};
            var pending = {};
            var handlers = {};
            var nextId = 0;
            function post(msg) {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(msg);
                } else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.agibuildWebView) {
                    window.webkit.messageHandlers.agibuildWebView.postMessage(msg);
                }
            }
            window.agWebView.rpc = {
                invoke: function(method, params, signal) {
                    return new Promise(function(resolve, reject) {
                        var id = '__js_' + (nextId++);
                        pending[id] = { resolve: resolve, reject: reject };
                        post(JSON.stringify({ jsonrpc: '2.0', id: id, method: method, params: params }));
                        if (signal) {
                            var onAbort = function() {
                                post(JSON.stringify({ jsonrpc: '2.0', method: '$/cancelRequest', params: { id: id } }));
                            };
                            if (signal.aborted) {
                                onAbort();
                            } else {
                                signal.addEventListener('abort', onAbort, { once: true });
                            }
                        }
                    });
                },
                handle: function(method, handler) {
                    handlers[method] = handler;
                },
                _dispatch: function(jsonStr) {
                    var msg = JSON.parse(jsonStr);
                    if (msg.method && handlers[msg.method]) {
                        try {
                            var result = handlers[msg.method](msg.params);
                            if (result && typeof result.then === 'function') {
                                result.then(function(r) {
                                    post(JSON.stringify({ jsonrpc: '2.0', id: msg.id, result: r }));
                                }).catch(function(e) {
                                    post(JSON.stringify({ jsonrpc: '2.0', id: msg.id, error: { code: -32603, message: e.message || 'Error' } }));
                                });
                            } else {
                                post(JSON.stringify({ jsonrpc: '2.0', id: msg.id, result: result }));
                            }
                        } catch(e) {
                            post(JSON.stringify({ jsonrpc: '2.0', id: msg.id, error: { code: -32603, message: e.message || 'Error' } }));
                        }
                    }
                },
                _onResponse: function(jsonStr) {
                    var msg = JSON.parse(jsonStr);
                    var p = pending[msg.id];
                    if (p) {
                        delete pending[msg.id];
                        if (msg.error) {
                            p.reject(new Error(msg.error.message || 'RPC error'));
                        } else {
                            p.resolve(msg.result);
                        }
                    }
                },
                _createAsyncIterable: function(method, params) {
                    var rpc = window.agWebView.rpc;
                    return {
                        [Symbol.asyncIterator]: function() {
                            var token = null;
                            var buffer = [];
                            var done = false;
                            var initPromise = rpc.invoke(method, params).then(function(r) {
                                token = r.token;
                                if (r.values) { for (var i = 0; i < r.values.length; i++) buffer.push(r.values[i]); }
                                if (r.finished) done = true;
                            });
                            return {
                                next: function() {
                                    return initPromise.then(function() {
                                        if (buffer.length > 0) return { value: buffer.shift(), done: false };
                                        if (done) return { value: undefined, done: true };
                                        return rpc.invoke('$/enumerator/next/' + token).then(function(r) {
                                            if (r.finished) { done = true; return { value: undefined, done: true }; }
                                            if (r.values && r.values.length > 0) return { value: r.values[0], done: false };
                                            return { value: undefined, done: true };
                                        });
                                    });
                                },
                                return: function() {
                                    if (token && !done) {
                                        done = true;
                                        post(JSON.stringify({ jsonrpc: '2.0', method: '$/enumerator/abort', params: { token: token } }));
                                    }
                                    return Promise.resolve({ value: undefined, done: true });
                                }
                            };
                        }
                    };
                }
            };
        })();
        """;

    // ==================== JSON-RPC DTOs ====================

    internal sealed class RpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Params { get; set; }
    }

    internal sealed class RpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Result { get; set; }
    }

    internal sealed class RpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    internal sealed class RpcErrorResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("error")]
        public RpcError? Error { get; set; }
    }

    internal sealed record ActiveEnumerator(
        Func<Task<(object? Value, bool Finished)>> MoveNext,
        Func<Task> Dispose);

    internal sealed class EnumeratorNextResult
    {
        [JsonPropertyName("values")]
        public object?[] Values { get; set; } = [];

        [JsonPropertyName("finished")]
        public bool Finished { get; set; }
    }

    internal sealed class EnumeratorInitResult
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = "";

        [JsonPropertyName("values")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object?[]? Values { get; set; }
    }
}

[JsonSerializable(typeof(WebViewRpcService.RpcRequest))]
[JsonSerializable(typeof(WebViewRpcService.RpcResponse))]
[JsonSerializable(typeof(WebViewRpcService.RpcErrorResponse))]
[JsonSerializable(typeof(WebViewRpcService.RpcError))]
[JsonSerializable(typeof(string))]
internal partial class RpcJsonContext : JsonSerializerContext
{
}
