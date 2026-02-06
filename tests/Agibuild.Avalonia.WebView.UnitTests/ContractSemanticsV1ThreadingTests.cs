using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1ThreadingTests
{
    [Fact]
    public void Sync_apis_require_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                webView.Stop();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        thread.Join();

        var invalidOp = Assert.IsType<InvalidOperationException>(thrown);
        Assert.Contains("UI thread", invalidOp.Message, StringComparison.OrdinalIgnoreCase);
    }
}

