using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

[Collection("NavigationSemantics")]
public sealed class ContractSemanticsV1CommandNavigationTests
{
    [Fact]
    public void Canceled_command_does_not_invoke_adapter_and_completes_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter
        {
            CanGoBack = true,
            GoBackAccepted = true
        };
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, e) => completed = e;

        webView.NavigationStarted += (_, e) => e.Cancel = true;

        var accepted = DispatcherTestPump.Run(dispatcher, () => webView.GoBackAsync());

        Assert.False(accepted);
        Assert.Equal(0, adapter.GoBackCallCount);
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
    }

    [Fact]
    public void Accepted_command_invokes_adapter_and_completes_with_matching_navigation_id()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter
        {
            CanGoBack = true,
            GoBackAccepted = true
        };
        var webView = new WebViewCore(adapter, dispatcher);

        var startedId = Guid.Empty;
        var completedId = Guid.Empty;

        webView.NavigationStarted += (_, e) => startedId = e.NavigationId;
        webView.NavigationCompleted += (_, e) => completedId = e.NavigationId;

        var accepted = DispatcherTestPump.Run(dispatcher, () => webView.GoBackAsync());

        Assert.True(accepted);
        Assert.Equal(1, adapter.GoBackCallCount);
        Assert.Equal(startedId, adapter.LastGoBackNavigationId);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        Assert.NotEqual(Guid.Empty, startedId);
        Assert.Equal(startedId, completedId);
    }
}

