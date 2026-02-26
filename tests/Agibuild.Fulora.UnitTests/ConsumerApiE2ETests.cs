using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Headless E2E tests verifying the consumer API path:
///   DI registration → resolve factory → create IWebView → navigate / script / events.
/// Uses <see cref="MockWebViewAdapter"/> + <see cref="TestDispatcher"/> so it runs
/// on all platforms without native WebView support.
/// </summary>
public sealed class ConsumerApiE2ETests
{
    [Fact]
    public void DI_resolves_IWebView_factory()
    {
        var services = new ServiceCollection();
        services.AddWebView();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetService<Func<IWebViewDispatcher, IWebView>>();

        Assert.NotNull(factory);
    }

    [Fact]
    public async Task Navigate_through_IWebView_fires_events_and_completes()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var startedArgs = default(NavigationStartingEventArgs);
        var completedArgs = default(NavigationCompletedEventArgs);

        core.NavigationStarted += (_, e) => startedArgs = e;
        core.NavigationCompleted += (_, e) => completedArgs = e;

        var uri = new Uri("https://example.test/page1");
        var navigateTask = core.NavigateAsync(uri);
        dispatcher.RunAll();

        // Simulate adapter raising NavigationCompleted.
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        await navigateTask;

        Assert.NotNull(startedArgs);
        Assert.Equal(uri, startedArgs!.RequestUri);

        Assert.NotNull(completedArgs);
        Assert.Equal(NavigationCompletedStatus.Success, completedArgs!.Status);
        Assert.Equal(startedArgs.NavigationId, completedArgs.NavigationId);
    }

    [Fact]
    public void InvokeScript_returns_result()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { ScriptResult = "42" };
        var core = new WebViewCore(adapter, dispatcher);

        var result = DispatcherTestPump.Run(dispatcher, () => core.InvokeScriptAsync("1+1"));

        Assert.Equal("42", result);
    }

    [Fact]
    public void Source_property_triggers_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var uri = new Uri("https://example.test/source");
        core.Source = uri;
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.LastNavigationUri == uri);

        Assert.Equal(uri, core.Source);
        Assert.Equal(uri, adapter.LastNavigationUri);
    }

    [Fact]
    public async Task NavigateToStringAsync_completes()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var html = "<html><body>hello</body></html>";
        var navigateTask = core.NavigateToStringAsync(html);
        dispatcher.RunAll();

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        await navigateTask;

        Assert.NotNull(adapter.LastNavigationId);
    }

    [Fact]
    public void GoBack_returns_false_when_not_available()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoBack = false };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.GoBackAsync()));
    }

    [Fact]
    public void GoBack_returns_true_when_available()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { CanGoBack = true, GoBackAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.True(DispatcherTestPump.Run(dispatcher, () => core.GoBackAsync()));
    }

    [Fact]
    public async Task Dispose_prevents_further_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        core.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => core.NavigateAsync(new Uri("https://example.test")));
    }
}
