using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class WebViewRpcServiceCoverageTests
{
    [Fact]
    public void RegisterEnumerator_next_requests_produce_responses_and_auto_dispose_on_finish()
    {
        var rpc = CreateRpc(out var scripts);
        var moveCount = 0;
        var disposed = false;

        rpc.RegisterEnumerator(
            "tok",
            () =>
            {
                moveCount++;
                return Task.FromResult<(object? Value, bool Finished)>(
                    moveCount == 1 ? ("value-1", false) : (null, true));
            },
            () =>
            {
                disposed = true;
                return Task.CompletedTask;
            });

        Assert.True(rpc.TryProcessMessage("""{"jsonrpc":"2.0","id":"n1","method":"$/enumerator/next/tok"}"""));
        WaitUntil(() => scripts.Any(s => s.Contains("n1")));
        Assert.Contains(scripts, s => s.Contains("n1") && s.Contains("finished"));

        Assert.True(rpc.TryProcessMessage("""{"jsonrpc":"2.0","id":"n2","method":"$/enumerator/next/tok"}"""));
        WaitUntil(() => scripts.Any(s => s.Contains("n2")));
        Assert.True(disposed);
    }

    [Fact]
    public async Task DisposeEnumerator_swallow_dispose_exception()
    {
        var rpc = CreateRpc(out _);
        rpc.RegisterEnumerator(
            "bad",
            () => Task.FromResult<(object?, bool)>((null, true)),
            () => throw new InvalidOperationException("dispose-failed"));

        var ex = await Record.ExceptionAsync(() => rpc.DisposeEnumerator("bad"));
        Assert.Null(ex);
    }

    [Fact]
    public void Notification_without_method_is_not_handled()
    {
        var rpc = CreateRpc(out _);
        var handled = rpc.TryProcessMessage("""{"jsonrpc":"2.0","params":{"x":1}}""");
        Assert.False(handled);
    }

    private static WebViewRpcService CreateRpc(out List<string> scripts)
    {
        var captured = new List<string>();
        scripts = captured;
        return new WebViewRpcService(
            script =>
            {
                captured.Add(script);
                return Task.FromResult<string?>(null);
            },
            NullLogger.Instance);
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMilliseconds = 3000)
    {
        Assert.True(
            SpinWait.SpinUntil(condition, TimeSpan.FromMilliseconds(timeoutMilliseconds)),
            "Timed out while waiting for RPC response.");
    }
}
