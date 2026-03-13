using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class ShellExperienceBranchCoverageTests
{
    [Fact]
    public void Constructor_throws_on_null_webView()
    {
        Assert.Throws<ArgumentNullException>(() => new WebViewShellExperience(null!));
    }

    [Fact]
    public void Constructor_uses_default_options_when_null()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, options: null);
        Assert.NotNull(shell);
        Assert.Null(shell.SessionDecision);
    }

    [Fact]
    public void ReadClipboardText_with_bridge_returns_provider_value()
    {
        var provider = new TrackingHostCapabilityProvider { ClipboardText = "hello" };
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.ReadClipboardText();

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void WriteClipboardText_with_bridge_executes()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.WriteClipboardText("world");

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.Equal("world", provider.LastWrittenClipboardText);
    }

    [Fact]
    public void ShowOpenFileDialog_with_bridge_executes()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.ShowOpenFileDialog(new WebViewOpenFileDialogRequest { Title = "Open" });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void ShowSaveFileDialog_with_bridge_executes()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.ShowSaveFileDialog(new WebViewSaveFileDialogRequest
        {
            Title = "Save",
            SuggestedFileName = "a.txt"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void ShowNotification_with_bridge_executes()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.ShowNotification(new WebViewNotificationRequest { Title = "T", Message = "M" });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void ApplyMenuModel_with_bridge_applies_and_stores_effective_model()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "file", Label = "File", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.NotNull(shell.EffectiveMenuModel);
        Assert.Single(shell.EffectiveMenuModel!.Items);
    }

    [Fact]
    public void UpdateTrayState_with_bridge_delegates()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true, Tooltip = "test" });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void ExecuteSystemAction_with_bridge_and_whitelisted_action()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var result = shell.ExecuteSystemAction(new WebViewSystemActionRequest { Action = WebViewSystemAction.Quit });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void ExecuteSystemAction_not_whitelisted_returns_deny()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            SystemActionWhitelist = new HashSet<WebViewSystemAction>(),
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = shell.ExecuteSystemAction(new WebViewSystemActionRequest { Action = WebViewSystemAction.Quit });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
        Assert.NotNull(error);
    }

    [Fact]
    public void PublishSystemIntegrationEvent_with_bridge_dispatches()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var request = new WebViewSystemIntegrationEventRequest
        {
            Source = "test",
            Kind = WebViewSystemIntegrationEventKind.MenuItemInvoked,
            ItemId = "item1",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        var result = shell.PublishSystemIntegrationEvent(request);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void SystemIntegrationEvent_forwarding_raises_event()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        WebViewSystemIntegrationEventRequest? received = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });
        shell.SystemIntegrationEventReceived += (_, e) => received = e;

        var request = new WebViewSystemIntegrationEventRequest
        {
            Source = "test-source",
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "tray-icon",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        bridge.DispatchSystemIntegrationEvent(request, shell.RootWindowId, null, shell.RootWindowId);

        Assert.NotNull(received);
        Assert.Equal("test-source", received!.Source);
    }

    [Fact]
    public void RootProfileIdentity_returns_null_when_no_resolver()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        Assert.Null(shell.RootProfileIdentity);
    }

    [Fact]
    public void ReadClipboardText_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.ReadClipboardText();
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void WriteClipboardText_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.WriteClipboardText("text");
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void ShowOpenFileDialog_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.ShowOpenFileDialog(new WebViewOpenFileDialogRequest { Title = "t" });
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void ShowSaveFileDialog_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.ShowSaveFileDialog(new WebViewSaveFileDialogRequest { Title = "t" });
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void ShowNotification_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.ShowNotification(new WebViewNotificationRequest { Title = "T", Message = "M" });
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void UpdateTrayState_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true });
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void ExecuteSystemAction_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());
        var result = shell.ExecuteSystemAction(new WebViewSystemActionRequest { Action = WebViewSystemAction.Quit });
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void PublishSystemIntegrationEvent_without_bridge_returns_denied()
    {
        using var webView = new FullWebView();
        var errors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            PolicyErrorHandler = (_, e) => errors.Add(e)
        });

        var result = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Source = "s",
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "i"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void Dispose_with_managed_windows_transitions_and_disposes()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var childWebView = new FullWebView();
        var lifecycleStates = new List<WebViewManagedWindowLifecycleState>();
        var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => childWebView
        });
        shell.ManagedWindowLifecycleChanged += (_, e) => lifecycleStates.Add(e.State);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        shell.Dispose();

        Assert.Contains(WebViewManagedWindowLifecycleState.Closing, lifecycleStates);
        Assert.Contains(WebViewManagedWindowLifecycleState.Closed, lifecycleStates);
        Assert.True(childWebView.IsDisposed);
    }

    [Fact]
    public void MenuPruning_policy_exception_reports_failure()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                throw new InvalidOperationException("pruning failed")),
            PolicyErrorHandler = (_, e) => error = e
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "f", Label = "F", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, result.Outcome);
        Assert.NotNull(error);
    }

    [Fact]
    public void MenuPruning_policy_deny_reports_denied()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                WebViewMenuPruningDecision.Deny("not-allowed"))
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "f", Label = "F", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void MenuPruning_with_profile_deny_blocks()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, ctx) =>
                WebViewMenuPruningDecision.Allow(ctx.RequestedMenuModel)),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((_, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "deny-menu",
                    SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "scope"),
                    PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                    {
                        [WebViewPermissionKind.Other] = WebViewPermissionProfileDecision.Deny()
                    }
                })
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "f", Label = "F", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void MenuPruning_profile_resolver_exception_reports_failure()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, ctx) =>
                WebViewMenuPruningDecision.Allow(ctx.RequestedMenuModel)),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.PermissionKind == WebViewPermissionKind.Other)
                    throw new InvalidOperationException("resolver failed");
                return new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "p",
                    SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "s")
                };
            })
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "f", Label = "F", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, result.Outcome);
    }

    [Fact]
    public void MenuPruning_null_profile_reports_failure()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, ctx) =>
                WebViewMenuPruningDecision.Allow(ctx.RequestedMenuModel)),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.PermissionKind == WebViewPermissionKind.Other)
                    return null!;
                return new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "p",
                    SessionDecisionOverride = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "s")
                };
            })
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "f", Label = "F", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, result.Outcome);
    }

    [Fact]
    public void MenuNormalization_skips_null_whitespace_and_duplicate_items()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var request = new WebViewMenuModelRequest
        {
            Items = new WebViewMenuItemModel[]
            {
                null!,
                new() { Id = "", Label = "Empty", IsEnabled = true },
                new() { Id = "  ", Label = "Whitespace", IsEnabled = true },
                new() { Id = "file", Label = "File", IsEnabled = true },
                new() { Id = "file", Label = "File Duplicate", IsEnabled = true }
            }
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.Single(shell.EffectiveMenuModel!.Items);
        Assert.Equal("file", shell.EffectiveMenuModel.Items[0].Id);
    }

    [Fact]
    public void MenuNormalization_null_item_id_skipped()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });

        var request = new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = null!, Label = "NullId", IsEnabled = true },
                new WebViewMenuItemModel { Id = "valid", Label = "Valid", IsEnabled = true }
            ]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.Single(shell.EffectiveMenuModel!.Items);
    }

    [Fact]
    public void MenuPruning_effective_model_passthrough()
    {
        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, ctx) =>
                WebViewMenuPruningDecision.Allow(null))
        });

        var request = new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "file", Label = "File", IsEnabled = true }]
        };
        var result = shell.ApplyMenuModel(request);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    [Fact]
    public void SystemIntegration_deny_outcome_reports_failure()
    {
        var provider = new TrackingHostCapabilityProvider();
        var denyPolicy = new DenyAllCapabilityPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, denyPolicy);
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, error!.Domain);
    }

    [Fact]
    public void SystemIntegration_failure_outcome_reports_error()
    {
        var provider = new TrackingHostCapabilityProvider { ThrowOnUpdateTrayState = true };
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, result.Outcome);
        Assert.NotNull(error);
    }

    [Fact]
    public void PolicyErrorEventArgs_null_exception_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WebViewShellPolicyErrorEventArgs(WebViewShellPolicyDomain.DevTools, null!));
    }

    [Fact]
    public void PolicyError_without_subscriber_does_not_crash()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadPolicy = new DelegateDownloadPolicy((_, _) => throw new InvalidOperationException("fail"))
        });

        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin")));
    }

    [Fact]
    public void PolicyErrorHandler_exception_swallowed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadPolicy = new DelegateDownloadPolicy((_, _) => throw new InvalidOperationException("fail")),
            PolicyErrorHandler = (_, _) => throw new InvalidOperationException("handler also fails")
        });

        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin")));
    }

    [Fact]
    public void DelegateDownloadPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateDownloadPolicy(null!));
    }

    [Fact]
    public void DelegatePermissionPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegatePermissionPolicy(null!));
    }

    [Fact]
    public void DelegateSessionPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateSessionPolicy(null!));
    }

    [Fact]
    public void DelegateMenuPruningPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateMenuPruningPolicy(null!));
    }

    [Fact]
    public void DelegateSessionPermissionProfileResolver_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateSessionPermissionProfileResolver(null!));
    }

    [Fact]
    public void SharedSessionPolicy_null_scope_uses_fallback()
    {
        var policy = new SharedSessionPolicy();
        var context = new WebViewShellSessionContext(null!);
        var decision = policy.Resolve(context);

        Assert.Equal(WebViewShellSessionScope.Shared, decision.Scope);
        Assert.Equal("shared", decision.ScopeIdentity);
    }

    [Fact]
    public void SharedSessionPolicy_whitespace_scope_uses_fallback()
    {
        var policy = new SharedSessionPolicy();
        var context = new WebViewShellSessionContext("   ");
        var decision = policy.Resolve(context);

        Assert.Equal("shared", decision.ScopeIdentity);
    }

    [Fact]
    public void IsolatedSessionPolicy_null_scope_uses_default()
    {
        var policy = new IsolatedSessionPolicy();
        var context = new WebViewShellSessionContext(null!);
        var decision = policy.Resolve(context);

        Assert.Equal(WebViewShellSessionScope.Isolated, decision.Scope);
        Assert.Equal("isolated:default", decision.ScopeIdentity);
    }

    [Fact]
    public void IsolatedSessionPolicy_whitespace_scope_uses_default()
    {
        var policy = new IsolatedSessionPolicy();
        var context = new WebViewShellSessionContext("  ");
        var decision = policy.Resolve(context);

        Assert.Equal("isolated:default", decision.ScopeIdentity);
    }

    [Fact]
    public void ResolveSessionDecision_inherits_parent_when_no_override()
    {
        var profile = new WebViewSessionPermissionProfile
        {
            ProfileIdentity = "test",
            InheritParentSessionDecision = true,
            SessionDecisionOverride = null
        };

        var parentDecision = new WebViewShellSessionDecision(WebViewShellSessionScope.Shared, "parent-scope");
        var result = profile.ResolveSessionDecision(parentDecision, null, "scope");

        Assert.Same(parentDecision, result);
    }

    [Fact]
    public void ResolveSessionDecision_uses_fallback_when_no_override_and_no_parent()
    {
        var profile = new WebViewSessionPermissionProfile
        {
            ProfileIdentity = "test",
            InheritParentSessionDecision = false,
            SessionDecisionOverride = null
        };

        var fallback = new WebViewShellSessionDecision(WebViewShellSessionScope.Isolated, "fallback");
        var result = profile.ResolveSessionDecision(null, fallback, "scope");

        Assert.Same(fallback, result);
    }

    [Fact]
    public void ResolveSessionDecision_creates_default_when_no_override_parent_or_fallback()
    {
        var profile = new WebViewSessionPermissionProfile
        {
            ProfileIdentity = "test",
            InheritParentSessionDecision = false,
            SessionDecisionOverride = null
        };

        var result = profile.ResolveSessionDecision(null, null, "scope");

        Assert.Equal(WebViewShellSessionScope.Shared, result.Scope);
        Assert.Equal("scope", result.ScopeIdentity);
    }

    [Fact]
    public void ResolveSessionDecision_default_for_whitespace_scope()
    {
        var profile = new WebViewSessionPermissionProfile
        {
            ProfileIdentity = "test",
            InheritParentSessionDecision = false,
            SessionDecisionOverride = null
        };

        var result = profile.ResolveSessionDecision(null, null, "  ");

        Assert.Equal("default", result.ScopeIdentity);
    }

    [Fact]
    public void NormalizeProfileHash_valid_sha256()
    {
        var hash = "sha256:" + new string('a', 64);
        var result = WebViewSessionPermissionProfile.NormalizeProfileHash(hash);
        Assert.Equal(hash, result);
    }

    [Fact]
    public void NormalizeProfileHash_null_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileHash(null));
    }

    [Fact]
    public void NormalizeProfileHash_whitespace_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileHash("  "));
    }

    [Fact]
    public void NormalizeProfileHash_wrong_prefix_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileHash("md5:" + new string('a', 64)));
    }

    [Fact]
    public void NormalizeProfileHash_wrong_length_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileHash("sha256:" + new string('a', 32)));
    }

    [Fact]
    public void NormalizeProfileHash_invalid_char_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileHash("sha256:" + new string('g', 64)));
    }

    [Fact]
    public void NormalizeProfileHash_uppercase_hex_normalized()
    {
        var hash = "sha256:" + new string('A', 64);
        var result = WebViewSessionPermissionProfile.NormalizeProfileHash(hash);
        Assert.Equal("sha256:" + new string('a', 64), result);
    }

    [Fact]
    public void NormalizeProfileVersion_null_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileVersion(null));
    }

    [Fact]
    public void NormalizeProfileVersion_whitespace_returns_null()
    {
        Assert.Null(WebViewSessionPermissionProfile.NormalizeProfileVersion("  "));
    }

    [Fact]
    public void NormalizeProfileVersion_trims_value()
    {
        Assert.Equal("1.0", WebViewSessionPermissionProfile.NormalizeProfileVersion(" 1.0 "));
    }

    [Fact]
    public void LoggingBridgeTracer_null_logger_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingBridgeTracer(null!));
    }

    [Fact]
    public void DiagnosticEventArgs_export_with_nulls()
    {
        var args = new WebViewHostCapabilityDiagnosticEventArgs(
            Guid.NewGuid(), Guid.NewGuid(),
            parentWindowId: null, targetWindowId: null,
            WebViewHostCapabilityOperation.ClipboardReadText,
            requestUri: null,
            WebViewHostCapabilityCallOutcome.Allow,
            wasAuthorized: true, denyReason: null,
            failureCategory: null,
            durationMilliseconds: 10);

        var export = args.ToExportRecord();

        Assert.Null(export.ParentWindowId);
        Assert.Null(export.TargetWindowId);
        Assert.Null(export.RequestUri);
        Assert.Null(export.FailureCategory);
    }

    [Fact]
    public void DiagnosticEventArgs_export_with_values()
    {
        var parentId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var args = new WebViewHostCapabilityDiagnosticEventArgs(
            Guid.NewGuid(), Guid.NewGuid(),
            parentWindowId: parentId, targetWindowId: targetId,
            WebViewHostCapabilityOperation.ExternalOpen,
            requestUri: new Uri("https://example.com"),
            WebViewHostCapabilityCallOutcome.Failure,
            wasAuthorized: true, denyReason: null,
            failureCategory: WebViewOperationFailureCategory.AdapterFailed,
            durationMilliseconds: 50);

        var export = args.ToExportRecord();

        Assert.Equal(parentId.ToString("D"), export.ParentWindowId);
        Assert.Equal(targetId.ToString("D"), export.TargetWindowId);
        Assert.Equal("https://example.com/", export.RequestUri);
        Assert.NotNull(export.FailureCategory);
    }
}
