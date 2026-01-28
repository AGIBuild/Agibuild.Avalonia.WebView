using Xunit;

namespace Agibuild.Avalonia.WebView.Tests;

public sealed class ContractExampleTests
{
    [Fact]
    public void Navigation_started_can_be_canceled()
    {
        var adapter = new MockWebViewAdapter();
        var handle = new TestPlatformHandle(IntPtr.Zero, "test");

        adapter.Initialize(new TestWebViewHost());
        adapter.Attach(handle);

        adapter.NavigationStarted += (_, args) => args.Cancel = true;

        var eventArgs = adapter.RaiseNavigationStarted(new Uri("https://example.test"));

        Assert.NotNull(eventArgs);
        Assert.True(eventArgs!.Cancel);
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
