using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

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

    [AvaloniaFact]
    public async Task Profile_governed_multi_window_flow_applies_session_and_permission_outcomes()
    {
        var dispatcher = new TestDispatcher();
        var rootAdapter = new MockWebViewAdapterFull();
        using var rootCore = new WebViewCore(rootAdapter, dispatcher);

        var created = new List<WebViewManagedWindowLifecycleEventArgs>();
        using var shell = new WebViewShellExperience(rootCore, new WebViewShellExperienceOptions
        {
            SessionContext = new WebViewShellSessionContext("tenant-a"),
            SessionPolicy = new SharedSessionPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.ParentWindowId is null)
                {
                    return new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "root-profile",
                        SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "tenant-root"),
                        PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                        {
                            [WebViewPermissionKind.Camera] = WebViewPermissionProfileDecision.Deny()
                        }
                    };
                }

                var isolate = ctx.RequestUri?.AbsolutePath.Contains("isolate", StringComparison.Ordinal) == true;
                return isolate
                    ? new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "child-isolated",
                        SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Isolated, $"isolated:{ctx.WindowId}")
                    }
                    : new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "child-inherited",
                        InheritParentSessionDecision = true
                    };
            }),
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (e.State == WebViewManagedWindowLifecycleState.Created)
                created.Add(e);
        };

        rootAdapter.RaiseNewWindowRequested(new Uri("https://example.com/inherit"));
        rootAdapter.RaiseNewWindowRequested(new Uri("https://example.com/isolate"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 2);

        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        rootAdapter.RaisePermissionRequested(permissionArgs);
        Assert.Equal(PermissionState.Deny, permissionArgs.State);

        Assert.Equal(2, created.Count);
        Assert.Equal("child-inherited", created[0].ProfileIdentity);
        Assert.Equal("tenant-root", created[0].SessionDecision!.ScopeIdentity);
        Assert.Equal("child-isolated", created[1].ProfileIdentity);
        Assert.Equal(WebViewShellSessionScope.Isolated, created[1].SessionDecision!.Scope);

        foreach (var id in shell.GetManagedWindowIds())
        {
            var closed = await shell.CloseManagedWindowAsync(id, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(closed);
        }

        Assert.Equal(0, shell.ManagedWindowCount);
    }

    [AvaloniaFact]
    public async Task Profile_governed_stress_cycles_keep_window_profile_correlation_clean()
    {
        var dispatcher = new TestDispatcher();
        var rootAdapter = new MockWebViewAdapterFull();
        using var rootCore = new WebViewCore(rootAdapter, dispatcher);

        var createdWindowIds = new HashSet<Guid>();
        var closedWindowIds = new HashSet<Guid>();
        var profileDecisionDiagnostics = new List<WebViewSessionPermissionProfileDiagnosticEventArgs>();

        using var shell = new WebViewShellExperience(rootCore, new WebViewShellExperienceOptions
        {
            SessionContext = new WebViewShellSessionContext("stress-root"),
            SessionPolicy = new SharedSessionPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.PermissionKind is not null)
                {
                    return new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "permission-profile",
                        PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                        {
                            [WebViewPermissionKind.Camera] = ctx.RequestUri?.AbsolutePath.Contains("/deny/", StringComparison.Ordinal) == true
                                ? WebViewPermissionProfileDecision.Deny()
                                : WebViewPermissionProfileDecision.Allow()
                        }
                    };
                }

                return new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = $"window-profile:{ctx.WindowId}",
                    SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Isolated, $"isolated:{ctx.WindowId}")
                };
            }),
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (e.State == WebViewManagedWindowLifecycleState.Created)
            {
                createdWindowIds.Add(e.WindowId);
                Assert.False(string.IsNullOrWhiteSpace(e.ProfileIdentity));
            }
            else if (e.State == WebViewManagedWindowLifecycleState.Closed)
            {
                closedWindowIds.Add(e.WindowId);
            }
        };

        shell.SessionPermissionProfileEvaluated += (_, e) =>
        {
            if (e.PermissionKind is not null)
                profileDecisionDiagnostics.Add(e);
        };

        const int iterations = 20;
        for (var i = 0; i < iterations; i++)
        {
            var uri = new Uri($"https://example.com/stress/{i}");
            rootAdapter.RaiseNewWindowRequested(uri);
            DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

            var permissionOrigin = new Uri(i % 2 == 0 ? "https://example.com/allow/camera" : "https://example.com/deny/camera");
            var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, permissionOrigin);
            rootAdapter.RaisePermissionRequested(permissionArgs);
            Assert.Equal(i % 2 == 0 ? PermissionState.Allow : PermissionState.Deny, permissionArgs.State);

            var ids = shell.GetManagedWindowIds();
            Assert.Single(ids);
            var closed = await shell.CloseManagedWindowAsync(ids[0], cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(closed);
            Assert.Equal(0, shell.ManagedWindowCount);
        }

        Assert.Equal(iterations, createdWindowIds.Count);
        Assert.Equal(createdWindowIds.Count, closedWindowIds.Count);
        Assert.Equal(iterations, profileDecisionDiagnostics.Count);
        Assert.All(profileDecisionDiagnostics, DiagnosticSchemaAssertionHelper.AssertSessionProfileDiagnostic);
    }
}
