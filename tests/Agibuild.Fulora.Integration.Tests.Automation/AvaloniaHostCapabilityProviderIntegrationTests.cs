using System;
using System.Collections.Generic;
using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

public sealed class AvaloniaHostCapabilityProviderIntegrationTests
{
    [AvaloniaFact]
    public void Tray_update_flows_through_bridge_and_provider()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new AllowAllPolicy());
        var diagnostics = new List<WebViewHostCapabilityDiagnosticEventArgs>();
        bridge.CapabilityCallCompleted += (_, e) => diagnostics.Add(e);

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest
        {
            IsVisible = true,
            Tooltip = "Integration Test Tray",
            IconPath = null
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.True(result.IsAllowed);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(WebViewHostCapabilityOperation.TrayUpdateState, diagnostic.Operation);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, diagnostic.Outcome);
    }

    [AvaloniaFact]
    public void Menu_model_flows_through_bridge_and_creates_native_menu()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new AllowAllPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items =
            [
                new WebViewMenuItemModel
                {
                    Id = "file", Label = "File",
                    Children =
                    [
                        new WebViewMenuItemModel { Id = "new", Label = "New" },
                        new WebViewMenuItemModel { Id = "open", Label = "Open" }
                    ]
                },
                new WebViewMenuItemModel { Id = "help", Label = "Help" }
            ]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
        Assert.True(result.IsAllowed);
    }

    [AvaloniaFact]
    public void Tray_interaction_event_dispatches_through_bridge()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new AllowAllPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var receivedEvents = new List<WebViewSystemIntegrationEventRequest>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });
        shell.SystemIntegrationEventReceived += (_, e) => receivedEvents.Add(e);

        var eventResult = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Source = "test-tray",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "tray-click",
            Context = "user-clicked-tray"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, eventResult.Outcome);
        Assert.Single(receivedEvents);
        Assert.Equal("tray-click", receivedEvents[0].ItemId);
    }

    [AvaloniaFact]
    public void Menu_interaction_event_dispatches_through_bridge()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new AllowAllPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var receivedEvents = new List<WebViewSystemIntegrationEventRequest>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });
        shell.SystemIntegrationEventReceived += (_, e) => receivedEvents.Add(e);

        // Apply a menu first
        shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "settings", Label = "Settings" }]
        });

        // Simulate menu item interaction via system integration event
        var eventResult = shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
        {
            Source = "test-menu",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Kind = WebViewSystemIntegrationEventKind.MenuItemInvoked,
            ItemId = "settings",
            Context = "user-clicked-settings"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, eventResult.Outcome);
        Assert.Single(receivedEvents);
        Assert.Equal("settings", receivedEvents[0].ItemId);
        Assert.Equal(WebViewSystemIntegrationEventKind.MenuItemInvoked, receivedEvents[0].Kind);
    }

    [AvaloniaFact]
    public void Deny_policy_blocks_tray_update()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new DenyTrayMenuPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest
        {
            IsVisible = true,
            Tooltip = "Should be denied"
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [AvaloniaFact]
    public void Deny_policy_blocks_menu_apply()
    {
        var inner = new StubInnerProvider();
        using var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new DenyTrayMenuPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "a", Label = "A" }]
        });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [AvaloniaFact]
    public void Provider_dispose_is_safe_after_operations()
    {
        var inner = new StubInnerProvider();
        var avaloniaProvider = new AvaloniaHostCapabilityProvider(inner);
        var bridge = new WebViewHostCapabilityBridge(avaloniaProvider, new AllowAllPolicy());

        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true, Tooltip = "Test" });
        shell.ApplyMenuModel(new WebViewMenuModelRequest
        {
            Items = [new WebViewMenuItemModel { Id = "x", Label = "X" }]
        });

        avaloniaProvider.Dispose();
        avaloniaProvider.Dispose(); // double dispose safe
    }

    // ─── Test helpers ───────────────────────────────────────────────────────

    private sealed class StubInnerProvider : IWebViewHostCapabilityProvider
    {
        public string? ReadClipboardText() => null;
        public void WriteClipboardText(string text) { }
        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
            => new() { IsCanceled = true, Paths = [] };
        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
            => new() { IsCanceled = true, Paths = [] };
        public void OpenExternal(Uri uri) { }
        public void ShowNotification(WebViewNotificationRequest request) { }
        public void ApplyMenuModel(WebViewMenuModelRequest request) { }
        public void UpdateTrayState(WebViewTrayStateRequest request) { }
        public void ExecuteSystemAction(WebViewSystemActionRequest request) { }
    }

    private sealed class AllowAllPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    private sealed class DenyTrayMenuPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation switch
            {
                WebViewHostCapabilityOperation.TrayUpdateState => WebViewHostCapabilityDecision.Deny("tray-denied"),
                WebViewHostCapabilityOperation.MenuApplyModel => WebViewHostCapabilityDecision.Deny("menu-denied"),
                _ => WebViewHostCapabilityDecision.Allow()
            };
    }
}
