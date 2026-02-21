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
            Action = WebViewSystemAction.Restart
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal("system-action-not-whitelisted", denied.DenyReason);
        Assert.Equal(0, provider.SystemActionCalls);
        Assert.Single(policyErrors);
        Assert.Equal(WebViewShellPolicyDomain.SystemIntegration, policyErrors[0].Domain);
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
            ItemId = "tray-main"
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
        Assert.Single(policyErrors);
        Assert.Contains("inbound-menu-event-denied", policyErrors[0].Exception.Message, StringComparison.Ordinal);
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
