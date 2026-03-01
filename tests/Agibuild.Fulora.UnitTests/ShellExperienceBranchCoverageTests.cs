using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ShellExperienceBranchCoverageTests
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

    #region Host capability bridge configured paths

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

    #endregion

    #region DevTools operations

    [Fact]
    public async Task DevTools_operations_return_false_when_disposed()
    {
        using var webView = new FullWebView();
        var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        shell.Dispose();

        Assert.False(await shell.OpenDevToolsAsync());
        Assert.False(await shell.CloseDevToolsAsync());
        Assert.False(await shell.IsDevToolsOpenAsync());
    }

    [Fact]
    public async Task DevTools_without_policy_allows_by_default()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());

        var opened = await shell.OpenDevToolsAsync();
        var state = await shell.IsDevToolsOpenAsync();
        var closed = await shell.CloseDevToolsAsync();

        Assert.True(opened);
        Assert.True(state);
        Assert.True(closed);
    }

    [Fact]
    public async Task CloseDevTools_deny_reports_error()
    {
        using var webView = new FullWebView();
        var errors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, ctx) =>
                ctx.Action == WebViewShellDevToolsAction.Close
                    ? WebViewShellDevToolsDecision.Deny("close-blocked")
                    : WebViewShellDevToolsDecision.Allow()),
            PolicyErrorHandler = (_, e) => errors.Add(e)
        });

        await shell.OpenDevToolsAsync();
        var closed = await shell.CloseDevToolsAsync();

        Assert.False(closed);
        Assert.Contains(errors, e => e.Domain == WebViewShellPolicyDomain.DevTools);
    }

    [Fact]
    public async Task DevTools_policy_returns_null_blocks_operation()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var opened = await shell.OpenDevToolsAsync();

        Assert.False(opened);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task DevTools_operation_exception_reports_failure()
    {
        using var webView = new ThrowingDevToolsWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        WebViewShellPolicyErrorEventArgs? error = null;
        shell.PolicyError += (_, e) => error = e;

        var opened = await shell.OpenDevToolsAsync();

        Assert.False(opened);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.DevTools, error!.Domain);
    }

    [Fact]
    public async Task DevTools_query_exception_reports_failure()
    {
        using var webView = new ThrowingDevToolsWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        WebViewShellPolicyErrorEventArgs? error = null;
        shell.PolicyError += (_, e) => error = e;

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CloseDevTools_null_decision_reports_error()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var closed = await shell.CloseDevToolsAsync();

        Assert.False(closed);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task IsDevToolsOpen_null_decision_reports_error()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task IsDevToolsOpen_deny_reports_error()
    {
        using var webView = new FullWebView();
        var errors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Deny("query-blocked")),
            PolicyErrorHandler = (_, e) => errors.Add(e)
        });

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.Contains(errors, e => e.Domain == WebViewShellPolicyDomain.DevTools);
    }

    #endregion

    #region Command operations

    [Fact]
    public async Task Command_returns_false_when_disposed()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow())
        });
        shell.Dispose();

        Assert.False(await shell.ExecuteCommandAsync(WebViewCommand.Copy));
        Assert.Empty(commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_without_policy_allows_by_default()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.True(executed);
        Assert.Equal([WebViewCommand.Copy], commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_all_types_execute_via_manager()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow())
        });

        foreach (var cmd in new[] { WebViewCommand.Copy, WebViewCommand.Cut, WebViewCommand.Paste,
            WebViewCommand.SelectAll, WebViewCommand.Undo, WebViewCommand.Redo })
        {
            Assert.True(await shell.ExecuteCommandAsync(cmd));
        }

        Assert.Equal(6, commandManager.ExecutedCommands.Count);
    }

    [Fact]
    public async Task Command_null_decision_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Empty(commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_denied_with_null_reason_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) =>
                WebViewShellCommandDecision.Deny(null)),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Paste);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.Command, error!.Domain);
    }

    [Fact]
    public async Task Command_unsupported_type_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow()),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync((WebViewCommand)999);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.Command, error!.Domain);
    }

    [Fact]
    public async Task Command_operation_exception_reports_failure()
    {
        using var webView = new FullWebView { CommandManager = new ThrowingCommandManager() };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow()),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.False(executed);
        Assert.NotNull(error);
    }

    #endregion

    #region New window strategies

    [Fact]
    public void NewWindow_ExternalBrowser_opens_uri_via_bridge()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/external"));
        DispatcherTestPump.WaitUntil(dispatcher, () => provider.OpenExternalCalledUris.Count == 1);

        Assert.Equal(new Uri("https://example.com/external"), provider.OpenExternalCalledUris[0]);
    }

    [Fact]
    public void NewWindow_ExternalBrowser_null_uri_reports_failure()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new TrackingHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(uri: null);
        DispatcherTestPump.WaitUntil(dispatcher, () => error is not null);

        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, error!.Domain);
    }

    [Fact]
    public void NewWindow_ExternalBrowser_without_bridge_reports_failure()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/ext"));
        DispatcherTestPump.WaitUntil(dispatcher, () => error is not null);

        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, error!.Domain);
    }

    [Fact]
    public void NewWindow_ExternalBrowser_deny_reports_error()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new TrackingHostCapabilityProvider();
        var denyPolicy = new DenyAllCapabilityPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, denyPolicy);
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/denied"));
        DispatcherTestPump.WaitUntil(dispatcher, () => error is not null);

        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, error!.Domain);
    }

    [Fact]
    public void NewWindow_ExternalBrowser_provider_throws_reports_failure()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new TrackingHostCapabilityProvider { ThrowOnOpenExternal = true };
        var bridge = new WebViewHostCapabilityBridge(provider);
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/fail"));
        DispatcherTestPump.WaitUntil(dispatcher, () => error is not null);

        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, error!.Domain);
    }

    [Fact]
    public void NewWindow_Delegate_strategy_sets_handled()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        NewWindowRequestedEventArgs? observed = null;
        core.NewWindowRequested += (_, e) => observed = e;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.Delegate(handled: true))
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/delegate"));
        DispatcherTestPump.WaitUntil(dispatcher, () => observed is not null);

        Assert.True(observed!.Handled);
    }

    #endregion

    #region Managed window lifecycle

    [Fact]
    public async Task CloseManagedWindowAsync_returns_false_when_disposed()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new FullWebView()
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var windowId = shell.GetManagedWindowIds()[0];
        shell.Dispose();

        Assert.False(await shell.CloseManagedWindowAsync(windowId));
    }

    [Fact]
    public async Task CloseManagedWindowAsync_unknown_id_returns_false()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());

        Assert.False(await shell.CloseManagedWindowAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseManagedWindowAsync_success_lifecycle()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var lifecycleStates = new List<WebViewManagedWindowLifecycleState>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new FullWebView()
        });
        shell.ManagedWindowLifecycleChanged += (_, e) => lifecycleStates.Add(e.State);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var windowId = shell.GetManagedWindowIds()[0];
        var closed = await shell.CloseManagedWindowAsync(windowId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(closed);
        Assert.Equal(0, shell.ManagedWindowCount);
        Assert.Contains(WebViewManagedWindowLifecycleState.Closing, lifecycleStates);
        Assert.Contains(WebViewManagedWindowLifecycleState.Closed, lifecycleStates);
    }

    [Fact]
    public async Task CloseManagedWindowAsync_timeout_reports_cancellation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new FullWebView(),
            ManagedWindowCloseAsync = async (_, ct) => await Task.Delay(TimeSpan.FromSeconds(30), ct),
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var windowId = shell.GetManagedWindowIds()[0];
        var closed = await shell.CloseManagedWindowAsync(windowId, timeout: TimeSpan.FromMilliseconds(10));

        Assert.False(closed);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.ManagedWindowLifecycle, error!.Domain);
    }

    [Fact]
    public async Task CloseManagedWindowAsync_handler_exception_reports_failure()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => new FullWebView(),
            ManagedWindowCloseAsync = (_, _) => throw new InvalidOperationException("close failed"),
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        var windowId = shell.GetManagedWindowIds()[0];
        var closed = await shell.CloseManagedWindowAsync(windowId);

        Assert.False(closed);
        Assert.NotNull(error);
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
    public void ManagedWindow_factory_returns_null_falls_back()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        NewWindowRequestedEventArgs? observed = null;
        core.NewWindowRequested += (_, e) => observed = e;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => null!
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => observed is not null);

        Assert.Equal(0, shell.ManagedWindowCount);
        Assert.False(observed!.Handled);
    }

    [Fact]
    public void ManagedWindow_without_factory_falls_back()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        NewWindowRequestedEventArgs? observed = null;
        core.NewWindowRequested += (_, e) => observed = e;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow())
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => observed is not null);

        Assert.Equal(0, shell.ManagedWindowCount);
    }

    #endregion

    #region Menu pruning

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
                new WebViewMenuItemModel { Id = null, Label = "NullId", IsEnabled = true },
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

    #endregion

    #region ReportSystemIntegrationOutcome branches

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

    #endregion

    #region PolicyError with no subscriber + PolicyErrorHandler exception

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

    #endregion

    #region Managed window with session policy and profile resolver

    [Fact]
    public void ManagedWindow_with_session_policy_resolves_scope()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var lifecycleStates = new List<WebViewManagedWindowLifecycleState>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow("custom-scope")),
            ManagedWindowFactory = _ => new FullWebView(),
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("tenant"),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = $"profile:{ctx.ScopeIdentity}",
                    SessionDecisionOverride = new WebViewShellSessionDecision(
                        WebViewShellSessionScope.Isolated, $"profile:{ctx.ScopeIdentity}")
                })
        });
        shell.ManagedWindowLifecycleChanged += (_, e) => lifecycleStates.Add(e.State);

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/managed"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        Assert.Contains(WebViewManagedWindowLifecycleState.Created, lifecycleStates);
        Assert.Contains(WebViewManagedWindowLifecycleState.Attached, lifecycleStates);
        Assert.Contains(WebViewManagedWindowLifecycleState.Ready, lifecycleStates);
    }

    #endregion

    #region Delegate policy null constructor tests

    [Fact]
    public void DelegateNewWindowPolicy_null_decider_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DelegateNewWindowPolicy(
                (Func<IWebView, NewWindowRequestedEventArgs, WebViewNewWindowPolicyContext,
                    WebViewNewWindowStrategyDecision>)null!));
    }

    [Fact]
    public void DelegateNewWindowPolicy_null_handler_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DelegateNewWindowPolicy((Action<IWebView, NewWindowRequestedEventArgs>)null!));
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
    public void DelegateDevToolsPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateDevToolsPolicy(null!));
    }

    [Fact]
    public void DelegateCommandPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateCommandPolicy(null!));
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

    #endregion

    #region Session policy edge cases

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

    #endregion

    #region WebViewSessionPermissionProfile edge cases

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

    #endregion

    #region LoggingBridgeTracer

    [Fact]
    public void LoggingBridgeTracer_null_logger_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingBridgeTracer(null!));
    }

    #endregion

    #region WebViewHostCapabilityDiagnosticEventArgs

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

    #endregion

    #region Test helpers

    private sealed class FullWebView : IWebView
    {
        public Uri Source { get; set; } = new("about:blank");
        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public bool IsLoading => false;
        public Guid ChannelId { get; } = Guid.NewGuid();
        public ICommandManager? CommandManager { get; init; }
        public bool IsDisposed { get; private set; }
        private bool _isDevToolsOpen;

        public event EventHandler<NavigationStartingEventArgs>? NavigationStarted { add { } remove { } }
        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted { add { } remove { } }
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested { add { } remove { } }
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived { add { } remove { } }
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested { add { } remove { } }
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested { add { } remove { } }
        public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested { add { } remove { } }
        public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested { add { } remove { } }
        public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated { add { } remove { } }
        public event EventHandler? AdapterDestroyed { add { } remove { } }
        public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested { add { } remove { } }

        public Task NavigateAsync(Uri uri) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html, Uri? baseUrl) => Task.CompletedTask;
        public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);
        public Task<bool> GoBackAsync() => Task.FromResult(false);
        public Task<bool> GoForwardAsync() => Task.FromResult(false);
        public Task<bool> RefreshAsync() => Task.FromResult(false);
        public Task<bool> StopAsync() => Task.FromResult(false);
        public ICookieManager? TryGetCookieManager() => null;
        public ICommandManager? TryGetCommandManager() => CommandManager;
        public Task<INativeHandle?> TryGetWebViewHandleAsync() => Task.FromResult<INativeHandle?>(null);
        public IWebViewRpcService? Rpc => null;
        public IBridgeService Bridge => throw new NotSupportedException();
        public Task<byte[]> CaptureScreenshotAsync() => Task.FromException<byte[]>(new NotSupportedException());
        public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null) => Task.FromException<byte[]>(new NotSupportedException());
        public Task<double> GetZoomFactorAsync() => Task.FromResult(1.0);
        public Task SetZoomFactorAsync(double zoomFactor) => Task.CompletedTask;
        public Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null) => Task.FromException<FindInPageResult>(new NotSupportedException());
        public Task StopFindInPageAsync(bool clearHighlights = true) => Task.CompletedTask;
        public Task<string> AddPreloadScriptAsync(string javaScript) => Task.FromException<string>(new NotSupportedException());
        public Task RemovePreloadScriptAsync(string scriptId) => Task.FromException(new NotSupportedException());

        public Task OpenDevToolsAsync()
        {
            _isDevToolsOpen = true;
            return Task.CompletedTask;
        }

        public Task CloseDevToolsAsync()
        {
            _isDevToolsOpen = false;
            return Task.CompletedTask;
        }

        public Task<bool> IsDevToolsOpenAsync() => Task.FromResult(_isDevToolsOpen);

        public void Dispose()
        {
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

    }

    private sealed class ThrowingDevToolsWebView : IWebView
    {
        public Uri Source { get; set; } = new("about:blank");
        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public bool IsLoading => false;
        public Guid ChannelId { get; } = Guid.NewGuid();

        public event EventHandler<NavigationStartingEventArgs>? NavigationStarted { add { } remove { } }
        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted { add { } remove { } }
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested { add { } remove { } }
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived { add { } remove { } }
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested { add { } remove { } }
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested { add { } remove { } }
        public event EventHandler<DownloadRequestedEventArgs>? DownloadRequested { add { } remove { } }
        public event EventHandler<PermissionRequestedEventArgs>? PermissionRequested { add { } remove { } }
        public event EventHandler<AdapterCreatedEventArgs>? AdapterCreated { add { } remove { } }
        public event EventHandler? AdapterDestroyed { add { } remove { } }
        public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested { add { } remove { } }

        public Task NavigateAsync(Uri uri) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html) => Task.CompletedTask;
        public Task NavigateToStringAsync(string html, Uri? baseUrl) => Task.CompletedTask;
        public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);
        public Task<bool> GoBackAsync() => Task.FromResult(false);
        public Task<bool> GoForwardAsync() => Task.FromResult(false);
        public Task<bool> RefreshAsync() => Task.FromResult(false);
        public Task<bool> StopAsync() => Task.FromResult(false);
        public ICookieManager? TryGetCookieManager() => null;
        public ICommandManager? TryGetCommandManager() => null;
        public Task<INativeHandle?> TryGetWebViewHandleAsync() => Task.FromResult<INativeHandle?>(null);
        public IWebViewRpcService? Rpc => null;
        public IBridgeService Bridge => throw new NotSupportedException();
        public Task<byte[]> CaptureScreenshotAsync() => Task.FromException<byte[]>(new NotSupportedException());
        public Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null) => Task.FromException<byte[]>(new NotSupportedException());
        public Task<double> GetZoomFactorAsync() => Task.FromResult(1.0);
        public Task SetZoomFactorAsync(double zoomFactor) => Task.CompletedTask;
        public Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null) => Task.FromException<FindInPageResult>(new NotSupportedException());
        public Task StopFindInPageAsync(bool clearHighlights = true) => Task.CompletedTask;
        public Task<string> AddPreloadScriptAsync(string javaScript) => Task.FromException<string>(new NotSupportedException());
        public Task RemovePreloadScriptAsync(string scriptId) => Task.FromException(new NotSupportedException());

        public Task OpenDevToolsAsync() => Task.FromException(new InvalidOperationException("devtools-broken"));
        public Task CloseDevToolsAsync() => Task.FromException(new InvalidOperationException("devtools-broken"));
        public Task<bool> IsDevToolsOpenAsync() => Task.FromException<bool>(new InvalidOperationException("devtools-broken"));

        public void Dispose() { }
    }

    private sealed class TrackingCommandManager : ICommandManager
    {
        public List<WebViewCommand> ExecutedCommands { get; } = [];

        public Task CopyAsync() => Track(WebViewCommand.Copy);
        public Task CutAsync() => Track(WebViewCommand.Cut);
        public Task PasteAsync() => Track(WebViewCommand.Paste);
        public Task SelectAllAsync() => Track(WebViewCommand.SelectAll);
        public Task UndoAsync() => Track(WebViewCommand.Undo);
        public Task RedoAsync() => Track(WebViewCommand.Redo);

        private Task Track(WebViewCommand command)
        {
            ExecutedCommands.Add(command);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingCommandManager : ICommandManager
    {
        public Task CopyAsync() => throw new InvalidOperationException("command-broken");
        public Task CutAsync() => throw new InvalidOperationException("command-broken");
        public Task PasteAsync() => throw new InvalidOperationException("command-broken");
        public Task SelectAllAsync() => throw new InvalidOperationException("command-broken");
        public Task UndoAsync() => throw new InvalidOperationException("command-broken");
        public Task RedoAsync() => throw new InvalidOperationException("command-broken");
    }

    private sealed class TrackingHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        public string? ClipboardText { get; set; }
        public string? LastWrittenClipboardText { get; private set; }
        public List<Uri> OpenExternalCalledUris { get; } = [];
        public bool ThrowOnOpenExternal { get; set; }
        public bool ThrowOnUpdateTrayState { get; set; }

        public string? ReadClipboardText() => ClipboardText;
        public void WriteClipboardText(string text) => LastWrittenClipboardText = text;
        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request) => new() { IsCanceled = false, Paths = ["test.txt"] };
        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request) => new() { IsCanceled = false, Paths = ["out.txt"] };

        public void OpenExternal(Uri uri)
        {
            if (ThrowOnOpenExternal) throw new InvalidOperationException("open-external-failed");
            OpenExternalCalledUris.Add(uri);
        }

        public void ShowNotification(WebViewNotificationRequest request) { }
        public void ApplyMenuModel(WebViewMenuModelRequest request) { }

        public void UpdateTrayState(WebViewTrayStateRequest request)
        {
            if (ThrowOnUpdateTrayState) throw new InvalidOperationException("tray-failed");
        }

        public void ExecuteSystemAction(WebViewSystemActionRequest request) { }
    }

    private sealed class DenyAllCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Deny("denied-by-test-policy");
    }

    #endregion
}
