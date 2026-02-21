using System;
using System.Collections.Generic;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ShellSystemIntegrationCapabilityTests
{
    [Fact]
    public void System_integration_operations_without_bridge_return_deny_and_report_policy_errors()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var menu = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel
                {
                    Id = "file",
                    Label = "File"
                }
            ]
        });
        var tray = shell.UpdateTrayState(new WebViewTrayStateRequest
        {
            IsVisible = true,
            Tooltip = "tray"
        });
        var action = shell.ExecuteSystemAction(new WebViewSystemActionRequest
        {
            Action = WebViewSystemAction.FocusMainWindow
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, menu.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, tray.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, action.Outcome);
        Assert.False(menu.IsAllowed);
        Assert.False(tray.IsAllowed);
        Assert.False(action.IsAllowed);

        Assert.Equal(3, policyErrors.Count);
        Assert.All(policyErrors, error =>
        {
            Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, error.Domain);
            Assert.Contains("Host capability bridge is not configured.", error.Exception.Message, StringComparison.Ordinal);
            Assert.True(WebViewOperationFailure.TryGetCategory(error.Exception, out var category));
            Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
        });
    }

    [Fact]
    public void Denied_system_integration_policy_skips_provider_and_keeps_other_capabilities_available()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        var bridge = new WebViewHostCapabilityBridge(provider, new DenyTrayPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var deniedTray = shell.UpdateTrayState(new WebViewTrayStateRequest
        {
            IsVisible = true,
            Tooltip = "tray-denied"
        });
        var clipboard = shell.ReadClipboardText();

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, deniedTray.Outcome);
        Assert.Equal("tray-denied", deniedTray.DenyReason);
        Assert.Equal(0, provider.TrayUpdateCalls);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, clipboard.Outcome);
        Assert.Equal("clipboard-ok", clipboard.Value);
        Assert.Equal(1, provider.ClipboardReadCalls);

        Assert.Single(policyErrors);
        Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, policyErrors[0].Domain);
        Assert.Contains("tray-denied", policyErrors[0].Exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void System_action_provider_failure_is_reported_and_isolated_from_other_system_integration_operations()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider { ThrowOnSystemAction = true };
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var failedAction = shell.ExecuteSystemAction(new WebViewSystemActionRequest
        {
            Action = WebViewSystemAction.Restart
        });
        var menu = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel
                {
                    Id = "help",
                    Label = "Help"
                }
            ]
        });
        var tray = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = false });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, failedAction.Outcome);
        Assert.NotNull(failedAction.Error);
        Assert.Equal(1, provider.SystemActionCalls);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, menu.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, tray.Outcome);
        Assert.Equal(1, provider.MenuApplyCalls);
        Assert.Equal(1, provider.TrayUpdateCalls);

        Assert.Single(policyErrors);
        Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, policyErrors[0].Domain);
        Assert.True(WebViewOperationFailure.TryGetCategory(policyErrors[0].Exception, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
    }

    [Fact]
    public void Allowed_system_integration_operations_complete_without_policy_errors()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var menu = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel
                {
                    Id = "view",
                    Label = "View"
                }
            ]
        });
        var tray = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true, Tooltip = "ok" });
        var action = shell.ExecuteSystemAction(new WebViewSystemActionRequest
        {
            Action = WebViewSystemAction.FocusMainWindow
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, menu.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, tray.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, action.Outcome);
        Assert.Equal(1, provider.MenuApplyCalls);
        Assert.Equal(1, provider.TrayUpdateCalls);
        Assert.Equal(1, provider.SystemActionCalls);
        Assert.Empty(policyErrors);
    }

    [Fact]
    public void Menu_pruning_policy_is_deterministic_and_denied_pruning_does_not_mutate_effective_menu_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var pruningPolicy = new ToggleMenuPruningPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = pruningPolicy
        });

        var first = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "file", Label = "File" },
                new WebViewMenuItemModel { Id = "file", Label = "File Duplicate" },
                new WebViewMenuItemModel { Id = "edit", Label = "Edit" }
            ]
        });
        var snapshot = shell.EffectiveMenuModel;

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, first.Outcome);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot!.Items.Count);
        Assert.Equal("file", snapshot.Items[0].Id);
        Assert.Equal("edit", snapshot.Items[1].Id);

        pruningPolicy.DenyNext = true;
        var denied = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "tools", Label = "Tools" }
            ]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("menu-pruning-denied", denied.DenyReason);
        Assert.NotNull(shell.EffectiveMenuModel);
        Assert.Equal(2, shell.EffectiveMenuModel!.Items.Count);
        Assert.Equal(1, provider.MenuApplyCalls);
    }

    [Fact]
    public void Profile_denied_menu_pruning_short_circuits_policy_stage_and_preserves_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var pruningPolicy = new CountingMenuPruningPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var profileDiagnostics = new List<WebViewSessionPermissionProfileDiagnosticEventArgs>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = pruningPolicy,
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((_, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "deny-profile",
                    ProfileVersion = "2026.02.21",
                    ProfileHash = $"SHA256:{new string('B', 64)}",
                    PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                    {
                        [WebViewPermissionKind.Other] = WebViewPermissionProfileDecision.Deny()
                    }
                }),
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });
        shell.SessionPermissionProfileEvaluated += (_, e) => profileDiagnostics.Add(e);

        var denied = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "file", Label = "File" }
            ]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("menu-pruning-profile-denied:deny-profile", denied.DenyReason);
        Assert.Equal(0, pruningPolicy.InvocationCount);
        Assert.Equal(0, provider.MenuApplyCalls);
        Assert.Null(shell.EffectiveMenuModel);
        Assert.Single(policyErrors);
        Assert.Contains("menu-pruning-profile-denied:deny-profile", policyErrors[0].Exception.Message, StringComparison.Ordinal);

        var profileDiagnostic = Assert.Single(profileDiagnostics);
        Assert.Equal("deny-profile", profileDiagnostic.ProfileIdentity);
        Assert.Equal("2026.02.21", profileDiagnostic.ProfileVersion);
        Assert.Equal($"sha256:{new string('b', 64)}", profileDiagnostic.ProfileHash);
        Assert.Equal(WebViewPermissionKind.Other, profileDiagnostic.PermissionKind);
        Assert.Equal(PermissionState.Deny, profileDiagnostic.PermissionDecision.State);
        Assert.True(profileDiagnostic.PermissionDecision.IsExplicit);
    }

    [Fact]
    public void Profile_allow_then_policy_deny_keeps_previous_effective_menu_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var pruningPolicy = new CountingMenuPruningPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var profileDiagnostics = new List<WebViewSessionPermissionProfileDiagnosticEventArgs>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = pruningPolicy,
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((_, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "allow-profile",
                    ProfileVersion = "2026.02.21",
                    ProfileHash = $"SHA256:{new string('C', 64)}",
                    PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                    {
                        [WebViewPermissionKind.Other] = WebViewPermissionProfileDecision.Allow()
                    }
                }),
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });
        shell.SessionPermissionProfileEvaluated += (_, e) => profileDiagnostics.Add(e);

        var first = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "file", Label = "File" }
            ]
        });
        pruningPolicy.DenyNext = true;
        var denied = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "tools", Label = "Tools" }
            ]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, first.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("menu-pruning-policy-denied", denied.DenyReason);
        Assert.Equal(2, pruningPolicy.InvocationCount);
        Assert.Equal(1, provider.MenuApplyCalls);
        Assert.NotNull(shell.EffectiveMenuModel);
        Assert.Single(shell.EffectiveMenuModel!.Items);
        Assert.Equal("file", shell.EffectiveMenuModel.Items[0].Id);
        Assert.Single(policyErrors);
        Assert.Contains("menu-pruning-policy-denied", policyErrors[0].Exception.Message, StringComparison.Ordinal);

        Assert.Equal(2, profileDiagnostics.Count);
        Assert.All(profileDiagnostics, diag =>
        {
            Assert.Equal("allow-profile", diag.ProfileIdentity);
            Assert.Equal("2026.02.21", diag.ProfileVersion);
            Assert.Equal($"sha256:{new string('c', 64)}", diag.ProfileHash);
            Assert.Equal(WebViewPermissionKind.Other, diag.PermissionKind);
            Assert.Equal(PermissionState.Allow, diag.PermissionDecision.State);
            Assert.True(diag.PermissionDecision.IsExplicit);
        });
    }

    [Fact]
    public void Profile_revision_metadata_is_optional_and_emits_stable_null_fields()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var profileDiagnostics = new List<WebViewSessionPermissionProfileDiagnosticEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new CountingMenuPruningPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((_, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "null-revision-profile",
                    ProfileVersion = " 2026.02.21 ",
                    ProfileHash = "sha256:invalid-hash",
                    PermissionDecisions = new Dictionary<WebViewPermissionKind, WebViewPermissionProfileDecision>
                    {
                        [WebViewPermissionKind.Other] = WebViewPermissionProfileDecision.Allow()
                    }
                })
        });
        shell.SessionPermissionProfileEvaluated += (_, e) => profileDiagnostics.Add(e);

        var menu = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "file", Label = "File" }
            ]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, menu.Outcome);
        var diagnostic = Assert.Single(profileDiagnostics);
        Assert.Equal("null-revision-profile", diagnostic.ProfileIdentity);
        Assert.Equal("2026.02.21", diagnostic.ProfileVersion);
        Assert.Null(diagnostic.ProfileHash);
        Assert.Equal(WebViewPermissionKind.Other, diagnostic.PermissionKind);
        Assert.Equal(PermissionState.Allow, diagnostic.PermissionDecision.State);
    }

    [Fact]
    public void Menu_pruning_profile_failure_is_isolated_from_permission_download_and_new_window_domains()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new CountingMenuPruningPolicy(),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
            {
                if (ctx.PermissionKind == WebViewPermissionKind.Other)
                    throw new InvalidOperationException("menu-pruning-profile-failure");

                return new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "isolation-profile",
                    DefaultPermissionDecision = WebViewPermissionProfileDecision.DefaultFallback()
                };
            }),
            DownloadPolicy = new DelegateDownloadPolicy((_, e) =>
            {
                e.DownloadPath = "D:\\tmp\\isolated.bin";
            }),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) =>
            {
                e.State = PermissionState.Allow;
            }),
            NewWindowPolicy = new NavigateInPlaceNewWindowPolicy(),
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var pruningFailure = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel { Id = "file", Label = "File" }
            ]
        });
        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        var downloadArgs = new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin"));
        var popupUri = new Uri("https://example.com/popup");
        adapter.RaisePermissionRequested(permissionArgs);
        adapter.RaiseDownloadRequested(downloadArgs);
        adapter.RaiseNewWindowRequested(popupUri);
        dispatcher.RunAll();

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, pruningFailure.Outcome);
        Assert.NotNull(pruningFailure.Error);
        Assert.Equal(0, provider.MenuApplyCalls);
        Assert.True(WebViewOperationFailure.TryGetCategory(pruningFailure.Error!, out var failureCategory));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, failureCategory);

        Assert.Equal(PermissionState.Allow, permissionArgs.State);
        Assert.Equal("D:\\tmp\\isolated.bin", downloadArgs.DownloadPath);
        Assert.Equal(1, adapter.NavigateCallCount);
        Assert.Equal(popupUri, adapter.LastNavigationUri);

        Assert.Contains(policyErrors, e =>
            e.Domain == WebViewShellPolicyDomain.SystemIntegration &&
            e.Exception.Message.Contains("menu-pruning-profile-failure", StringComparison.Ordinal));
    }

    [Fact]
    public void Non_whitelisted_system_action_is_denied_before_provider_execution()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            SystemActionWhitelist = new HashSet<WebViewSystemAction> { WebViewSystemAction.FocusMainWindow },
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var denied = shell.ExecuteSystemAction(new WebViewSystemActionRequest
        {
            Action = WebViewSystemAction.ShowAbout
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("system-action-not-whitelisted", denied.DenyReason);
        Assert.Equal(0, provider.SystemActionCalls);
        Assert.Single(policyErrors);
        Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, policyErrors[0].Domain);
    }

    [Fact]
    public void ShowAbout_system_action_executes_only_when_explicitly_allowlisted()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            SystemActionWhitelist = new HashSet<WebViewSystemAction>
            {
                WebViewSystemAction.ShowAbout
            },
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        var allowed = shell.ExecuteSystemAction(new WebViewSystemActionRequest
        {
            Action = WebViewSystemAction.ShowAbout
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, allowed.Outcome);
        Assert.True(allowed.IsAllowed);
        Assert.True(allowed.IsSuccess);
        Assert.Equal(1, provider.SystemActionCalls);
        Assert.Empty(policyErrors);
    }

    [Fact]
    public void Inbound_system_integration_events_are_delivered_only_when_policy_allows()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new DenyMenuInboundPolicy());
        var received = new List<WebViewSystemIntegrationEventRequest>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });
        shell.SystemIntegrationEventReceived += (_, evt) => received.Add(evt);

        var tray = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "tray-main",
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "unit-test"
            }
        });
        var menu = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Kind = WebViewSystemIntegrationEventKind.MenuItemInvoked,
            ItemId = "menu-file-open"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, tray.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, menu.Outcome);
        Assert.Single(received);
        Assert.Equal(WebViewSystemIntegrationEventKind.TrayInteracted, received[0].Kind);
        Assert.Equal("unit-test", received[0].Metadata["source"]);
        Assert.Single(policyErrors);
        Assert.Contains("inbound-menu-event-denied", policyErrors[0].Exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_inbound_tray_metadata_is_denied_before_web_delivery()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var received = new List<WebViewSystemIntegrationEventRequest>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });
        shell.SystemIntegrationEventReceived += (_, evt) => received.Add(evt);

        var invalid = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "tray-main",
            Metadata = new Dictionary<string, string>
            {
                [""] = "invalid-key"
            }
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, invalid.Outcome);
        Assert.Equal("system-integration-event-metadata-envelope-invalid", invalid.DenyReason);
        Assert.Empty(received);
        Assert.Single(policyErrors);
        Assert.Contains("system-integration-event-metadata-envelope-invalid", policyErrors[0].Exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Over_budget_inbound_tray_metadata_is_denied_before_web_delivery()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new TrackingProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var received = new List<WebViewSystemIntegrationEventRequest>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });
        shell.SystemIntegrationEventReceived += (_, evt) => received.Add(evt);

        var denied = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "tray-budget-over",
            Metadata = new Dictionary<string, string>
            {
                ["a"] = new string('x', 256),
                ["b"] = new string('x', 256),
                ["c"] = new string('x', 256),
                ["d"] = new string('x', 256)
            }
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("system-integration-event-metadata-budget-exceeded", denied.DenyReason);
        Assert.Empty(received);
        Assert.Single(policyErrors);
        Assert.Contains("system-integration-event-metadata-budget-exceeded", policyErrors[0].Exception.Message, StringComparison.Ordinal);
    }

    private sealed class AllowAllPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    private sealed class DenyTrayPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation == WebViewHostCapabilityOperation.TrayUpdateState
                ? WebViewHostCapabilityDecision.Deny("tray-denied")
                : WebViewHostCapabilityDecision.Allow();
    }

    private sealed class DenyMenuInboundPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation == WebViewHostCapabilityOperation.MenuInteractionEventDispatch
                ? WebViewHostCapabilityDecision.Deny("inbound-menu-event-denied")
                : WebViewHostCapabilityDecision.Allow();
    }

    private sealed class ToggleMenuPruningPolicy : IWebViewShellMenuPruningPolicy
    {
        public bool DenyNext { get; set; }

        public WebViewMenuPruningDecision Decide(IWebView webView, WebViewMenuPruningPolicyContext context)
        {
            if (DenyNext)
            {
                DenyNext = false;
                return WebViewMenuPruningDecision.Deny("menu-pruning-denied");
            }

            return WebViewMenuPruningDecision.Allow(context.RequestedMenuModel);
        }
    }

    private sealed class CountingMenuPruningPolicy : IWebViewShellMenuPruningPolicy
    {
        public int InvocationCount { get; private set; }
        public bool DenyNext { get; set; }

        public WebViewMenuPruningDecision Decide(IWebView webView, WebViewMenuPruningPolicyContext context)
        {
            InvocationCount++;
            if (DenyNext)
            {
                DenyNext = false;
                return WebViewMenuPruningDecision.Deny("menu-pruning-policy-denied");
            }

            return WebViewMenuPruningDecision.Allow(context.RequestedMenuModel);
        }
    }

    private sealed class TrackingProvider : IWebViewHostCapabilityProvider
    {
        public int ClipboardReadCalls { get; private set; }
        public int MenuApplyCalls { get; private set; }
        public int TrayUpdateCalls { get; private set; }
        public int SystemActionCalls { get; private set; }
        public bool ThrowOnSystemAction { get; init; }

        public string? ReadClipboardText()
        {
            ClipboardReadCalls++;
            return "clipboard-ok";
        }

        public void WriteClipboardText(string text)
        {
        }

        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
            => throw new NotSupportedException();

        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
            => throw new NotSupportedException();

        public void OpenExternal(Uri uri)
            => throw new NotSupportedException();

        public void ShowNotification(WebViewNotificationRequest request)
            => throw new NotSupportedException();

        public void ApplyMenuModel(WebViewMenuModelRequest request)
            => MenuApplyCalls++;

        public void UpdateTrayState(WebViewTrayStateRequest request)
            => TrayUpdateCalls++;

        public void ExecuteSystemAction(WebViewSystemActionRequest request)
        {
            SystemActionCalls++;
            if (ThrowOnSystemAction)
                throw new InvalidOperationException("system-action-provider-failure");
        }
    }
}
