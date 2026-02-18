using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class MultiWindowLifecycleIntegrationTests
{
    [AvaloniaFact]
    public async Task Managed_window_representative_flow_create_route_close_passes()
    {
        var dispatcher = new TestDispatcher();
        var rootAdapter = MockWebViewAdapter.Create();
        using var rootCore = new WebViewCore(rootAdapter, dispatcher);

        var lifecycle = new List<WebViewManagedWindowLifecycleState>();
        using var shell = new WebViewShellExperience(rootCore, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher),
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("integration")
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            lifecycle.Add(e.State);
        };

        rootAdapter.RaiseNewWindowRequested(new Uri("https://example.com/child"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var childIds = shell.GetManagedWindowIds();
        Assert.Single(childIds);
        var closed = await shell.CloseManagedWindowAsync(
            childIds[0],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(closed);
        Assert.Equal(0, shell.ManagedWindowCount);
        Assert.Contains(WebViewManagedWindowLifecycleState.Ready, lifecycle);
        Assert.Contains(WebViewManagedWindowLifecycleState.Closed, lifecycle);
    }

    [AvaloniaFact]
    public async Task Managed_window_stress_open_close_cycles_leave_no_active_windows()
    {
        var dispatcher = new TestDispatcher();
        var rootAdapter = MockWebViewAdapter.Create();
        using var rootCore = new WebViewCore(rootAdapter, dispatcher);

        using var shell = new WebViewShellExperience(rootCore, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher),
            ManagedWindowCloseTimeout = TimeSpan.FromSeconds(2)
        });

        const int iterations = 30;
        for (var i = 0; i < iterations; i++)
        {
            var uri = new Uri($"https://example.com/stress/{i}");
            await ThreadingTestHelper.RunOffThread(() =>
            {
                rootAdapter.RaiseNewWindowRequested(uri);
                return Task.CompletedTask;
            });

            DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);
            var ids = shell.GetManagedWindowIds();
            Assert.Single(ids);

            var closed = await shell.CloseManagedWindowAsync(
                ids[0],
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken);
            Assert.True(closed);
            Assert.Equal(0, shell.ManagedWindowCount);
        }
    }
}
