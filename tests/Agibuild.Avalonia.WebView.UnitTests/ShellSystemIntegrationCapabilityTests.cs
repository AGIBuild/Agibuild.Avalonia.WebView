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
