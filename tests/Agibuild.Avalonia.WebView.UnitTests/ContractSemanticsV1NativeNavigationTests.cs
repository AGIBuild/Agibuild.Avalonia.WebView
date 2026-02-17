using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ContractSemanticsV1NativeNavigationTests
{
    [Fact]
    public void Native_initiated_navigation_can_be_canceled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        webView.NavigationCompleted += (_, e) => completed = e;

        webView.NavigationStarted += (_, e) => e.Cancel = true;

        var decisionTask = ThreadingTestHelper.RunOffThread(async () =>
        {
            var decision = await adapter.SimulateNativeNavigationStartingAsync(new Uri("https://example.test/native"));
            return decision;
        });

        dispatcher.RunAll();
        var decisionResult = decisionTask.GetAwaiter().GetResult();

        Assert.False(decisionResult.IsAllowed);
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Canceled, completed!.Status);
    }

    [Fact]
    public void Native_initiated_navigation_updates_Source_and_completes_on_adapter_completion()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        NavigationCompletedEventArgs? completed = null;
        using var completedRaised = new ManualResetEventSlim(false);
        webView.NavigationCompleted += (_, e) =>
        {
            completed = e;
            completedRaised.Set();
        };

        var target = new Uri("https://example.test/native-ok");
        var decisionTask = ThreadingTestHelper.RunOffThread(async () =>
        {
            var decision = await adapter.SimulateNativeNavigationStartingAsync(target);
            return decision;
        });

        dispatcher.RunAll();
        var decision = decisionTask.GetAwaiter().GetResult();

        Assert.True(decision.IsAllowed);
        Assert.Equal(target, webView.Source);

        adapter.RaiseNavigationCompleted(NavigationCompletedStatus.Success);

        Assert.True(completedRaised.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.NotNull(completed);
        Assert.Equal(NavigationCompletedStatus.Success, completed!.Status);
        Assert.Equal(decision.NavigationId, completed.NavigationId);
    }

    [Fact]
    public void Redirect_steps_reuse_NavigationId_for_same_CorrelationId()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapter();
        var webView = new WebViewCore(adapter, dispatcher);

        var started = new List<(Guid NavigationId, Uri RequestUri)>();
        webView.NavigationStarted += (_, e) => started.Add((e.NavigationId, e.RequestUri));

        var correlationId = Guid.NewGuid();

        var step1 = ThreadingTestHelper.RunOffThread(async () =>
            await adapter.SimulateNativeNavigationStartingAsync(new Uri("https://example.test/start"), correlationId: correlationId));
        dispatcher.RunAll();
        var d1 = step1.GetAwaiter().GetResult();

        var step2 = ThreadingTestHelper.RunOffThread(async () =>
            await adapter.SimulateNativeNavigationStartingAsync(new Uri("https://example.test/redirected"), correlationId: correlationId));
        dispatcher.RunAll();
        var d2 = step2.GetAwaiter().GetResult();

        Assert.True(d1.IsAllowed);
        Assert.True(d2.IsAllowed);
        Assert.NotEqual(Guid.Empty, d1.NavigationId);
        Assert.Equal(d1.NavigationId, d2.NavigationId);

        Assert.True(started.Count >= 2);
        Assert.All(started, s => Assert.Equal(d1.NavigationId, s.NavigationId));
        Assert.Contains(started, s => s.RequestUri == new Uri("https://example.test/start"));
        Assert.Contains(started, s => s.RequestUri == new Uri("https://example.test/redirected"));
    }

}

