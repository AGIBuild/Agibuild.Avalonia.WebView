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

        var scriptTask = ThreadingTestHelper.RunOffThread(() => webView.InvokeScriptAsync("return 1;"));

        dispatcher.RunAll();

        await Assert.ThrowsAsync<WebViewScriptException>(() => scriptTask);
    }

}

