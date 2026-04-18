using System.Reflection;
using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class BranchCoverageRound3Tests
{
    #region Medium: WebViewRpcService notification handlers

    [Fact]
    public void HandleNotification_cancelRequest_cancels_active_cts()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // Register a pending cancellation via reflection
        var cancellationsField = typeof(WebViewRpcService).GetField(
            "_activeCancellations", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(cancellationsField);
        var cancellations = cancellationsField!.GetValue(rpc) as System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource>;
        Assert.NotNull(cancellations);

        var cts = new CancellationTokenSource();
        cancellations!["test-cancel-id"] = cts;

        // Send $/cancelRequest notification
        var cancelJson = """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"test-cancel-id"}}""";
        var handled = rpc.TryProcessMessage(cancelJson);
        Assert.True(handled);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void HandleNotification_cancelRequest_unknown_id_still_returns_true()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // Line 453: targetId is not null but key not found in _activeCancellations
        var cancelJson = """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"nonexistent"}}""";
        var handled = rpc.TryProcessMessage(cancelJson);
        Assert.True(handled);
    }

    [Fact]
    public void HandleNotification_cancelRequest_null_id_returns_true()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // Line 453: targetId is null → `targetId is not null` fails → skip cancel
        var cancelJson = """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":null}}""";
        var handled = rpc.TryProcessMessage(cancelJson);
        Assert.True(handled);
    }

    [Fact]
    public void HandleNotification_enumeratorAbort_dispatches()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // Line 459: $/enumerator/abort notification
        var abortJson = """{"jsonrpc":"2.0","method":"$/enumerator/abort","params":{"token":"test-token"}}""";
        var handled = rpc.TryProcessMessage(abortJson);
        Assert.True(handled);
    }

    [Fact]
    public void ResolvePendingCall_error_without_message_uses_default()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // Register a pending call via reflection
        var pendingField = typeof(WebViewRpcService).GetField(
            "_pendingCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(pendingField);
        var pending = pendingField!.GetValue(rpc) as System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>;
        Assert.NotNull(pending);

        var tcs = new TaskCompletionSource<JsonElement>();
        pending!["error-test-id"] = tcs;

        // Send response with error that has code but no message → line 478 covers `m.GetString() ?? "RPC error"`
        var errorJson = """{"jsonrpc":"2.0","id":"error-test-id","error":{"code":-32600}}""";
        rpc.TryProcessMessage(errorJson);

        Assert.True(tcs.Task.IsFaulted);
        var rpcEx = Assert.IsType<WebViewRpcException>(tcs.Task.Exception!.InnerException);
        Assert.Equal(-32600, rpcEx.Code);
    }

    [Fact]
    public void ResolvePendingCall_error_with_null_message_uses_default()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        var pendingField = typeof(WebViewRpcService).GetField(
            "_pendingCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var pending = pendingField!.GetValue(rpc) as System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>;

        var tcs = new TaskCompletionSource<JsonElement>();
        pending!["null-msg-id"] = tcs;

        // Error with explicit null message → GetString() returns null → ?? "RPC error"
        var errorJson = """{"jsonrpc":"2.0","id":"null-msg-id","error":{"code":-32600,"message":null}}""";
        rpc.TryProcessMessage(errorJson);

        Assert.True(tcs.Task.IsFaulted);
        var rpcEx = Assert.IsType<WebViewRpcException>(tcs.Task.Exception!.InnerException);
        Assert.Equal("RPC error", rpcEx.Message);
    }

    #endregion

    #region Round 4: RPC notification edge cases

    [Fact]
    public void HandleNotification_unknown_method_returns_false()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // A notification with an unknown method should reach line 459 with methodName != "$/enumerator/abort"
        var json = """{"jsonrpc":"2.0","method":"$/some/unknown/method","params":{}}""";
        var handled = rpc.TryProcessMessage(json);
        Assert.False(handled);
    }

    [Fact]
    public void HandleNotification_enumeratorAbort_without_params_falls_through()
    {
        var rpc = new WebViewRpcService(
            s => Task.FromResult<string?>(null),
            NullLoggerFactory.Instance.CreateLogger("test"));

        // $/enumerator/abort without "params" → TryGetProperty("params",...) returns false → falls through
        var json = """{"jsonrpc":"2.0","method":"$/enumerator/abort"}""";
        var handled = rpc.TryProcessMessage(json);
        Assert.False(handled);
    }

    #endregion

    #region Round 5 Tier 2: BridgeImportProxy.Invoke with null args

    [Fact]
    public void BridgeImportProxy_invoke_with_null_args_sends_null_params()
    {
        var proxy = DispatchProxy.Create<INoArgImport, BridgeImportProxy>();
        var bridgeProxy = (BridgeImportProxy)(object)proxy;

        string? capturedMethod = null;
        object? capturedParams = null;
        var mockRpc = new LambdaRpcService((method, p) =>
        {
            capturedMethod = method;
            capturedParams = p;
            return Task.FromResult(default(JsonElement));
        });
        bridgeProxy.Initialize(mockRpc, "TestSvc");

        var invokeMethod = typeof(BridgeImportProxy)
            .GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var targetMethod = typeof(INoArgImport).GetMethod("DoAsync")!;

        invokeMethod.Invoke(bridgeProxy, [targetMethod, null]);

        Assert.Equal("TestSvc.doAsync", capturedMethod);
        Assert.Null(capturedParams);
    }

    #endregion
}
