using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1ScriptTests
{
    [Fact]
    public void InvokeScriptAsync_null_throws_ArgumentNullException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Assert.Throws<ArgumentNullException>(() => { _ = webView.InvokeScriptAsync(null!); });
    }

    [Fact]
    public async Task Script_failure_faults_with_WebViewScriptException()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter
        {
            ScriptException = new InvalidOperationException("native script error")
        };
        var webView = new WebViewCore(adapter, dispatcher);

        var scriptTask = RunOffThread(() => webView.InvokeScriptAsync("return 1;"));

        dispatcher.RunAll();

        await Assert.ThrowsAsync<WebViewScriptException>(() => scriptTask);
    }

    private static Task<string?> RunOffThread(Func<Task<string?>> func)
    {
        using var ready = new ManualResetEventSlim(false);
        var tcs = new TaskCompletionSource<Task<string?>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                ready.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Off-thread invocation did not start within timeout.");
        }
        return tcs.Task.Unwrap();
    }
}

