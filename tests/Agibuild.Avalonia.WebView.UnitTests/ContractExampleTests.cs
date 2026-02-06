using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractExampleTests
{
    [Fact]
    public async Task NavigationStarted_can_be_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        webView.NavigationStarted += (_, args) => args.Cancel = true;

        await webView.NavigateAsync(new Uri("https://example.test"));

        Assert.Null(adapter.LastNavigateThreadId);
    }

    [Fact]
    public async Task Script_invocation_returns_configured_value()
    {
        var adapter = new MockWebViewAdapter
        {
            ScriptResult = "ok"
        };

        var result = await adapter.InvokeScriptAsync("return 'ok';");

        Assert.Equal("ok", result);
    }
}
