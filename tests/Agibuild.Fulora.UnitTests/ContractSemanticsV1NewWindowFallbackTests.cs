using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1NewWindowFallbackTests
{
    [Fact]
    public void Unhandled_NewWindowRequested_with_non_null_URI_triggers_NavigateAsync_fallback()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? startedArgs = null;
        core.NavigationStarted += (_, e) => startedArgs = e;

        // Raise new window request without handling it
        var targetUri = new Uri("https://example.test/popup");
        adapter.RaiseNewWindowRequested(targetUri);
        DispatcherTestPump.WaitUntil(dispatcher, () => startedArgs is not null);

        // The fallback should have triggered NavigateAsync which raises NavigationStarted
        Assert.NotNull(startedArgs);
        Assert.Equal(targetUri, startedArgs!.RequestUri);

        // Complete the fallback navigation
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
    }

    [Fact]
    public void Handled_NewWindowRequested_does_not_trigger_fallback_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.NewWindowRequested += (_, e) => e.Handled = true;

        NavigationStartingEventArgs? startedArgs = null;
        core.NavigationStarted += (_, e) => startedArgs = e;

        adapter.RaiseNewWindowRequested(new Uri("https://example.test/popup"));
        dispatcher.RunAll();

        // No fallback navigation should have been triggered
        Assert.Null(startedArgs);
    }

    [Fact]
    public void Unhandled_NewWindowRequested_with_null_URI_takes_no_action()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NavigationStartingEventArgs? startedArgs = null;
        core.NavigationStarted += (_, e) => startedArgs = e;

        adapter.RaiseNewWindowRequested(null);
        dispatcher.RunAll();

        Assert.Null(startedArgs);
    }
}
