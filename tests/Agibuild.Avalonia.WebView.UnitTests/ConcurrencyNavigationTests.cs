using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Tests for concurrent / superseding navigation scenarios.
/// </summary>
public sealed class ConcurrencyNavigationTests
{
    [Fact]
    public async Task Second_navigate_supersedes_first()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var completedStatuses = new List<NavigationCompletedStatus>();
        core.NavigationCompleted += (_, e) => completedStatuses.Add(e.Status);

        var task1 = core.NavigateAsync(new Uri("https://example.test/1"));
        dispatcher.RunAll();

        var task2 = core.NavigateAsync(new Uri("https://example.test/2"));
        dispatcher.RunAll();

        // First navigation should have been superseded.
        Assert.Contains(NavigationCompletedStatus.Superseded, completedStatuses);

        // Complete the second navigation.
        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        await task1;
        await task2;

        Assert.Equal(2, completedStatuses.Count);
        Assert.Equal(NavigationCompletedStatus.Superseded, completedStatuses[0]);
        Assert.Equal(NavigationCompletedStatus.Success, completedStatuses[1]);
    }

    [Fact]
    public async Task Source_set_supersedes_active_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var core = new WebViewCore(adapter, dispatcher);

        var completedStatuses = new List<NavigationCompletedStatus>();
        core.NavigationCompleted += (_, e) => completedStatuses.Add(e.Status);

        var task1 = core.NavigateAsync(new Uri("https://example.test/first"));
        dispatcher.RunAll();

        core.Source = new Uri("https://example.test/second");

        Assert.Contains(NavigationCompletedStatus.Superseded, completedStatuses);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);
        dispatcher.RunAll();

        await task1;

        Assert.False(core.IsLoading);
    }

    [Fact]
    public async Task Stop_during_navigation_completes_as_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { StopAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        core.NavigationCompleted += (_, e) => completed = e;

        var task = core.NavigateAsync(new Uri("https://example.test/stop"));
        dispatcher.RunAll();

        var stopAccepted = DispatcherTestPump.Run(dispatcher, () => core.StopAsync());

        Assert.True(stopAccepted);
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
        Assert.False(core.IsLoading);
        Assert.Equal(1, adapter.StopCallCount);

        await task;
    }

    [Fact]
    public void Stop_when_no_active_navigation_is_noop()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter { StopAccepted = true };
        var core = new WebViewCore(adapter, dispatcher);

        Assert.False(DispatcherTestPump.Run(dispatcher, () => core.StopAsync()));
        Assert.False(core.IsLoading);
    }
}
