using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class NewWindowRequestedTests
{
    [Fact]
    public void Adapter_NewWindowRequested_is_forwarded_to_core()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NewWindowRequestedEventArgs? received = null;
        core.NewWindowRequested += (_, e) => received = e;

        var uri = new Uri("https://example.test/popup");
        adapter.RaiseNewWindowRequested(uri);
        dispatcher.RunAll();

        Assert.NotNull(received);
        Assert.Equal(uri, received!.Uri);
    }

    [Fact]
    public void Adapter_NewWindowRequested_with_null_uri_is_forwarded()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        NewWindowRequestedEventArgs? received = null;
        core.NewWindowRequested += (_, e) => received = e;

        adapter.RaiseNewWindowRequested(null);
        dispatcher.RunAll();

        Assert.NotNull(received);
        Assert.Null(received!.Uri);
    }

    [Fact]
    public void NewWindowRequested_Handled_property_defaults_to_false()
    {
        var args = new NewWindowRequestedEventArgs(new Uri("https://example.test/popup"));

        Assert.False(args.Handled);
    }

    [Fact]
    public void NewWindowRequested_Handled_can_be_set_by_subscriber()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.NewWindowRequested += (_, e) => e.Handled = true;

        NewWindowRequestedEventArgs? received = null;
        core.NewWindowRequested += (_, e) => received = e;

        adapter.RaiseNewWindowRequested(new Uri("https://example.test/popup"));
        dispatcher.RunAll();

        Assert.NotNull(received);
        Assert.True(received!.Handled);
    }
}
