using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class MultiWindowLifecycleTests
{
    [Fact]
    public void Strategy_mapping_supports_inplace_managed_external_and_delegate_paths()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var externalOpened = false;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, e, _) =>
            {
                if (e.Uri is null)
                    return WebViewNewWindowStrategyDecision.InPlace();
                if (e.Uri.AbsolutePath.Contains("managed", StringComparison.Ordinal))
                    return WebViewNewWindowStrategyDecision.ManagedWindow();
                if (e.Uri.AbsolutePath.Contains("external", StringComparison.Ordinal))
                    return WebViewNewWindowStrategyDecision.ExternalBrowser();
                if (e.Uri.AbsolutePath.Contains("delegate-fallback", StringComparison.Ordinal))
                    return WebViewNewWindowStrategyDecision.Delegate(handled: false);
                return WebViewNewWindowStrategyDecision.InPlace();
            }),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher),
            ExternalOpenHandler = (_, _) => externalOpened = true
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        dispatcher.RunAll();
        Assert.Equal(1, shell.ManagedWindowCount);
        Assert.Equal(0, adapter.NavigateCallCount);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/external"));
        dispatcher.RunAll();
        Assert.True(externalOpened);
        Assert.Equal(1, shell.ManagedWindowCount);
        Assert.Equal(0, adapter.NavigateCallCount);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/delegate-fallback"));
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 1);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/in-place"));
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 2);
    }

    [Fact]
    public async Task Managed_window_lifecycle_order_is_deterministic_and_closed_is_terminal()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var states = new List<WebViewManagedWindowLifecycleState>();
        Guid childWindowId = Guid.Empty;

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (childWindowId == Guid.Empty)
                childWindowId = e.WindowId;
            if (e.WindowId == childWindowId)
                states.Add(e.State);
        };

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var closeResult = await shell.CloseManagedWindowAsync(
            childWindowId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(closeResult);
        Assert.Equal(0, shell.ManagedWindowCount);

        var closeAgain = await shell.CloseManagedWindowAsync(
            childWindowId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(closeAgain);

        Assert.Equal(
            [
                WebViewManagedWindowLifecycleState.Created,
                WebViewManagedWindowLifecycleState.Attached,
                WebViewManagedWindowLifecycleState.Ready,
                WebViewManagedWindowLifecycleState.Closing,
                WebViewManagedWindowLifecycleState.Closed
            ],
            states);
    }

    [Fact]
    public void Session_policy_receives_parent_child_context_and_can_choose_inheritance_or_isolation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var observedChildContexts = new List<WebViewShellSessionContext>();
        var childSessionDecisions = new List<WebViewShellSessionDecision>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            SessionContext = new WebViewShellSessionContext("root"),
            SessionPolicy = new DelegateSessionPolicy(ctx =>
            {
                if (ctx.ParentWindowId is not null)
                    observedChildContexts.Add(ctx);

                if (string.Equals(ctx.ScopeIdentity, "inherit", StringComparison.Ordinal))
                    return new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "root");

                return new WebViewShellSessionDecision(WebViewShellSessionScope.Isolated, $"isolated:{ctx.WindowId}");
            }),
            NewWindowPolicy = new DelegateNewWindowPolicy((_, e, _) =>
            {
                var inherit = e.Uri?.AbsolutePath.Contains("inherit", StringComparison.Ordinal) == true;
                return WebViewNewWindowStrategyDecision.ManagedWindow(inherit ? "inherit" : "isolate");
            }),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (e.State == WebViewManagedWindowLifecycleState.Created && e.SessionDecision is not null)
                childSessionDecisions.Add(e.SessionDecision);
        };

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/inherit"));
        adapter.RaiseNewWindowRequested(new Uri("https://example.com/isolate"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 2);

        Assert.Equal(2, observedChildContexts.Count);
        Assert.All(observedChildContexts, ctx =>
        {
            Assert.Equal(shell.RootWindowId, ctx.ParentWindowId);
            Assert.NotNull(ctx.WindowId);
        });

        Assert.Equal(2, childSessionDecisions.Count);
        Assert.Equal(WebViewShellSessionScope.Shared, childSessionDecisions[0].Scope);
        Assert.Equal("root", childSessionDecisions[0].ScopeIdentity);
        Assert.Equal(WebViewShellSessionScope.Isolated, childSessionDecisions[1].Scope);
        Assert.StartsWith("isolated:", childSessionDecisions[1].ScopeIdentity, StringComparison.Ordinal);
    }

    [Fact]
    public void Strategy_failure_is_isolated_and_falls_back_without_breaking_other_domains()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? observedError = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => throw new InvalidOperationException("new-window policy failed")),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
            PolicyErrorHandler = (_, err) => observedError = err
        });

        var permission = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        adapter.RaiseNewWindowRequested(new Uri("https://example.com/fallback"));
        adapter.RaisePermissionRequested(permission);
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 1);

        Assert.NotNull(observedError);
        Assert.Equal(WebViewShellPolicyDomain.NewWindow, observedError!.Domain);
        Assert.Equal(PermissionState.Deny, permission.State);
    }

    [Fact]
    public async Task Teardown_failure_is_isolated_and_does_not_leave_stale_window_references()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        IWebView? firstWindow = null;
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ =>
            {
                var child = new WebViewCore(MockWebViewAdapter.Create(), dispatcher);
                firstWindow ??= child;
                return child;
            },
            ManagedWindowCloseAsync = (window, _) =>
            {
                if (ReferenceEquals(window, firstWindow))
                    throw new InvalidOperationException("close failed");
                window.Dispose();
                return Task.CompletedTask;
            },
            PolicyErrorHandler = (_, err) => policyErrors.Add(err)
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/1"));
        adapter.RaiseNewWindowRequested(new Uri("https://example.com/2"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 2);

        var ids = shell.GetManagedWindowIds();
        Assert.Equal(2, ids.Count);

        var firstClosed = await shell.CloseManagedWindowAsync(
            ids[0],
            cancellationToken: TestContext.Current.CancellationToken);
        var secondClosed = await shell.CloseManagedWindowAsync(
            ids[1],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(firstClosed);
        Assert.True(secondClosed);
        Assert.Equal(0, shell.ManagedWindowCount);
        Assert.Contains(policyErrors, e => e.Domain == WebViewShellPolicyDomain.ManagedWindowLifecycle);
    }

    [Fact]
    public void External_browser_strategy_routes_through_host_capability_bridge_when_configured()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new ExternalOpenOnlyProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowExternalOnlyPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge,
            ExternalOpenHandler = (_, _) => throw new InvalidOperationException("legacy handler should not be used")
        });

        var target = new Uri("https://example.com/external");
        adapter.RaiseNewWindowRequested(target);
        dispatcher.RunAll();

        Assert.Single(provider.ExternalOpens);
        Assert.Equal(target, provider.ExternalOpens[0]);
        Assert.Equal(0, adapter.NavigateCallCount);
    }

    [Fact]
    public void External_browser_deny_is_reported_without_fallback_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new ExternalOpenOnlyProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new DenyExternalPolicy());
        WebViewShellPolicyErrorEventArgs? observedError = null;

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => observedError = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/denied"));
        dispatcher.RunAll();

        Assert.Empty(provider.ExternalOpens);
        Assert.Equal(0, adapter.NavigateCallCount);
        Assert.NotNull(observedError);
        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, observedError!.Domain);
    }

    [Fact]
    public void Session_permission_profile_inheritance_and_override_matrix_is_deterministic()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var createdEvents = new List<WebViewManagedWindowLifecycleEventArgs>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            SessionContext = new WebViewShellSessionContext("root-scope"),
            SessionPolicy = new SharedSessionPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, parent) =>
            {
                if (ctx.ParentWindowId is null)
                {
                    return new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "root-profile",
                        SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "root-session")
                    };
                }

                var shouldOverride = ctx.RequestUri?.AbsolutePath.Contains("override", StringComparison.Ordinal) == true;
                return shouldOverride
                    ? new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "child-override-profile",
                        SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Isolated, $"isolated:{ctx.WindowId}")
                    }
                    : new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "child-inherit-profile",
                        InheritParentSessionDecision = true
                    };
            }),
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (e.State == WebViewManagedWindowLifecycleState.Created)
                createdEvents.Add(e);
        };

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/inherit"));
        adapter.RaiseNewWindowRequested(new Uri("https://example.com/override"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 2);

        Assert.Equal(2, createdEvents.Count);

        var inherited = createdEvents[0];
        Assert.Equal("child-inherit-profile", inherited.ProfileIdentity);
        Assert.NotNull(inherited.SessionDecision);
        Assert.Equal("root-session", inherited.SessionDecision!.ScopeIdentity);

        var overridden = createdEvents[1];
        Assert.Equal("child-override-profile", overridden.ProfileIdentity);
        Assert.NotNull(overridden.SessionDecision);
        Assert.Equal(WebViewShellSessionScope.Isolated, overridden.SessionDecision!.Scope);
        Assert.StartsWith("isolated:", overridden.SessionDecision.ScopeIdentity, StringComparison.Ordinal);
    }

    [Fact]
    public void Child_profile_resolution_failure_isolated_and_falls_back_to_root_profile_context()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        WebViewManagedWindowLifecycleEventArgs? createdEvent = null;

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            SessionContext = new WebViewShellSessionContext("fallback-root"),
            SessionPolicy = new SharedSessionPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.ParentWindowId is null)
                {
                    return new WebViewSessionPermissionProfile
                    {
                        ProfileIdentity = "root-profile",
                        SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "root-profile-session")
                    };
                }

                throw new InvalidOperationException("child profile resolution failed");
            }),
            PolicyErrorHandler = (_, error) => policyErrors.Add(error),
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher)
        });

        shell.ManagedWindowLifecycleChanged += (_, e) =>
        {
            if (e.State == WebViewManagedWindowLifecycleState.Created)
                createdEvent = e;
        };

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/failure"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        Assert.NotNull(createdEvent);
        Assert.Equal("root-profile", createdEvent!.ProfileIdentity);
        Assert.NotNull(createdEvent.SessionDecision);
        Assert.Equal("fallback-root", createdEvent.SessionDecision!.ScopeIdentity);
        Assert.Contains(policyErrors, e => e.Domain == WebViewShellPolicyDomain.Session);
    }

    private sealed class ExternalOpenOnlyProvider : IWebViewHostCapabilityProvider
    {
        public List<Uri> ExternalOpens { get; } = [];

        public string? ReadClipboardText() => throw new NotSupportedException();
        public void WriteClipboardText(string text) => throw new NotSupportedException();
        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request) => throw new NotSupportedException();
        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request) => throw new NotSupportedException();

        public void OpenExternal(Uri uri) => ExternalOpens.Add(uri);

        public void ShowNotification(WebViewNotificationRequest request) => throw new NotSupportedException();
    }

    private sealed class AllowExternalOnlyPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
        {
            return context.Operation == WebViewHostCapabilityOperation.ExternalOpen
                ? WebViewHostCapabilityDecision.Allow()
                : WebViewHostCapabilityDecision.Deny("unsupported");
        }
    }

    private sealed class DenyExternalPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Deny("external-blocked");
    }
}
