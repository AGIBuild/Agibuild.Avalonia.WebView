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
    #region Round 6: Refactoring coverage — new helpers and error paths

    [Fact]
    public void RpcMethodHelpers_SplitRpcMethodAsMethod_with_dot_returns_correct_parts()
    {
        var (service, method) = RpcMethodHelpers.SplitRpcMethodAsMethod("ServiceName.MethodName");
        Assert.Equal("ServiceName", service);
        Assert.Equal("MethodName", method);
    }

    [Fact]
    public void RpcMethodHelpers_SplitRpcMethodAsMethod_without_dot_returns_method_only()
    {
        var (service, method) = RpcMethodHelpers.SplitRpcMethodAsMethod("MethodOnly");
        Assert.Equal("", service);
        Assert.Equal("MethodOnly", method);
    }

    [Fact]
    public void RpcMethodHelpers_SplitRpcMethod_with_dot()
    {
        var (service, method) = RpcMethodHelpers.SplitRpcMethod("Svc.Method");
        Assert.Equal("Svc", service);
        Assert.Equal("Method", method);
    }

    [Fact]
    public void RpcMethodHelpers_SplitRpcMethod_without_dot()
    {
        var (service, method) = RpcMethodHelpers.SplitRpcMethod("NoDot");
        Assert.Equal("NoDot", service);
        Assert.Equal("", method);
    }

    [Fact]
    public void FuloraException_preserves_error_code_and_message()
    {
        var ex = new FuloraException(FuloraErrorCodes.RpcError, "test message");
        Assert.Equal(FuloraErrorCodes.RpcError, ex.ErrorCode);
        Assert.Equal("test message", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void FuloraException_preserves_inner_exception()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new FuloraException(FuloraErrorCodes.AiContentBlocked, "outer", inner);
        Assert.Equal(FuloraErrorCodes.AiContentBlocked, ex.ErrorCode);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public async Task WebViewRpcService_handler_throwing_FuloraException_returns_structured_error()
    {
        string? sentResponse = null;
        var rpc = new WebViewRpcService(
            s => { sentResponse = s; return Task.FromResult<string?>(null); },
            NullLoggerFactory.Instance.CreateLogger("test"));

        rpc.Handle("TestSvc.failMethod", (JsonElement? _) =>
        {
            throw new FuloraException(FuloraErrorCodes.AiBudgetExceeded, "Budget exceeded");
        });

        var requestJson = """{"jsonrpc":"2.0","id":"err-1","method":"TestSvc.failMethod","params":null}""";
        rpc.TryProcessMessage(requestJson);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.NotNull(sentResponse);
        Assert.Contains("AI_BUDGET_EXCEEDED", sentResponse!);
        Assert.Contains("Budget exceeded", sentResponse);
    }

    [Fact]
    public void UiThreadHelper_SafeDispatch_dispatched_when_not_disposed()
    {
        var dispatcher = new TestDispatcher();
        var invoked = false;
        UiThreadHelper.SafeDispatch(dispatcher, false, false, () => invoked = true);
        Assert.True(invoked);
    }

    [Fact]
    public void UiThreadHelper_SafeDispatch_ignored_when_disposed_with_logger()
    {
        var dispatcher = new TestDispatcher();
        var logger = NullLoggerFactory.Instance.CreateLogger("test");
        var invoked = false;
        UiThreadHelper.SafeDispatch(dispatcher, true, false, () => invoked = true,
            logger, "Event ignored: disposed");
        Assert.False(invoked);
    }

    [Fact]
    public void UiThreadHelper_SafeDispatch_ignored_when_destroyed_without_logger()
    {
        var dispatcher = new TestDispatcher();
        var invoked = false;
        UiThreadHelper.SafeDispatch(dispatcher, false, true, () => invoked = true);
        Assert.False(invoked);
    }

    [Fact]
    public void UiThreadHelper_SafeDispatch_invokes_async_when_not_on_ui_thread()
    {
        var dispatcher = new FakeOffThreadDispatcher();
        var invoked = false;
        UiThreadHelper.SafeDispatch(dispatcher, false, false, () => invoked = true);
        Assert.False(invoked);
        Assert.True(dispatcher.InvokeAsyncCalled);
    }

    // FakeOffThreadDispatcher lives in BranchCoverageRound3Tests.Helpers.cs.

    #endregion
}
