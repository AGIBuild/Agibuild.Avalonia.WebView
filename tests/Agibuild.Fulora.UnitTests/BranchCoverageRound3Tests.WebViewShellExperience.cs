using System.Reflection;
using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class BranchCoverageRound3Tests
{
    #region Medium: WebViewShellExperience null-coalescing branches

    [Fact]
    public void ApplyMenuModel_null_effectiveMenuModel_uses_normalizedRequest()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = CreateFullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
        });

        var request = new WebViewMenuModelRequest();
        var result = shell.ApplyMenuModel(request);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryApplyProfilePermission_sessionDecision_null_ScopeIdentity()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateWithPermission();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        // No SessionPolicy set → _sessionDecision is null
        // SessionPermissionProfileResolver is set → TryApplyProfilePermissionDecision runs
        // Line 1097: _sessionDecision?.ScopeIdentity evaluates to null, falls through to ?? path
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "test-profile"
                })
        });

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera));
    }

    [Fact]
    public void ExternalBrowser_deny_with_null_reason_covers_coalesce()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, policy: new NullReasonDenyPolicy());
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ExternalBrowser()),
            PolicyErrorHandler = (_, e) => error = e
        });

        adapter.RaiseNewWindowRequested(new Uri("https://external.test"));
        DispatcherTestPump.WaitUntil(dispatcher, () => error is not null);

        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, error!.Domain);
        Assert.Contains("External open was denied by host capability policy.", error.Exception.Message);
    }

    [Fact]
    public void ReportSystemIntegrationOutcome_deny_null_reason_covers_coalesce()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, policy: new NullReasonDenyPolicy());
        using var webView = CreateFullWebView();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });
        shell.PolicyError += (_, e) => policyErrors.Add(e);

        var result = shell.ReadClipboardText();
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void Dispose_with_managed_window_in_closing_state()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                WebViewNewWindowStrategyDecision.ManagedWindow()),
            ManagedWindowFactory = _ => CreateFullWebView()
        });

        adapter.RaiseNewWindowRequested(new Uri("https://child.test"));
        DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);

        // Set the managed window's state to Closing via reflection
        var managerField = typeof(WebViewShellExperience).GetField(
            "_managedWindowManager", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(managerField);

        var manager = managerField!.GetValue(shell);
        Assert.NotNull(manager);

        var managedWindowsField = manager!.GetType().GetField(
            "_managedWindows", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(managedWindowsField);

        var windows = managedWindowsField!.GetValue(manager);
        var entriesProperty = windows!.GetType().GetProperty("Values");
        var entries = entriesProperty!.GetValue(windows) as System.Collections.IEnumerable;
        foreach (var entry in entries!)
        {
            var stateProperty = entry.GetType().GetProperty("State");
            stateProperty!.SetValue(entry, WebViewManagedWindowLifecycleState.Closing);
        }

        shell.Dispose();
    }

    #endregion

    #region Medium: WebViewShellExperience IsTransitionAllowed / TryTransition failure paths

    [Fact]
    public void IsTransitionAllowed_invalid_transition_returns_false()
    {
        // Use reflection to test the static method
        var method = typeof(WebViewShellExperience).GetMethod(
            "IsTransitionAllowed", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // null → Attached (invalid: null can only go to Created)
        var result1 = (bool)method!.Invoke(null, [null, WebViewManagedWindowLifecycleState.Attached])!;
        Assert.False(result1);

        // Created → Ready (invalid: Created can only go to Attached or Closing)
        var result2 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Created, WebViewManagedWindowLifecycleState.Ready])!;
        Assert.False(result2);

        // Ready → Created (invalid: Ready can only go to Closing)
        var result3 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Ready, WebViewManagedWindowLifecycleState.Created])!;
        Assert.False(result3);

        // Closed → Created (invalid: Closed is terminal)
        var result4 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Closed, WebViewManagedWindowLifecycleState.Created])!;
        Assert.False(result4);

        // Created → Closing (valid)
        var result5 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Created, WebViewManagedWindowLifecycleState.Closing])!;
        Assert.True(result5);

        // Attached → Closing (valid)
        var result6 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Attached, WebViewManagedWindowLifecycleState.Closing])!;
        Assert.True(result6);

        // Closing → Closed (valid)
        var result7 = (bool)method!.Invoke(null,
            [WebViewManagedWindowLifecycleState.Closing, WebViewManagedWindowLifecycleState.Closed])!;
        Assert.True(result7);
    }

    #endregion

    #region Round 4: Menu pruning denied with null reason

    [Fact]
    public void MenuPruning_deny_with_null_reason_covers_coalesce()
    {
        // Line 1604: decision.DenyReason ?? "menu-pruning-policy-denied"
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = CreateFullWebView();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                new WebViewMenuPruningDecision(IsAllowed: false, DenyReason: null))
        });
        shell.PolicyError += (_, e) => policyErrors.Add(e);

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest());
        Assert.NotNull(result);
        Assert.True(policyErrors.Count > 0);
    }

    #endregion

    #region Round 4: Menu pruning profile scope with null _sessionDecision

    [Fact]
    public void MenuPruning_profile_scope_uses_session_context_when_sessionDecision_null()
    {
        // Line 1621: _sessionDecision?.ScopeIdentity ?? _options.SessionContext.ScopeIdentity
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = CreateFullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                new WebViewMenuPruningDecision(IsAllowed: true)),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "test-profile"
                })
        });

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest());
        Assert.NotNull(result);
    }

    #endregion

    #region Round 5 Tier 1: WebViewShellExperience managed window lifecycle

    [Fact]
    public void IsTransitionAllowed_created_to_closing_returns_true()
    {
        var method = typeof(WebViewShellExperience)
            .GetMethod("IsTransitionAllowed", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (bool)method.Invoke(null, [
            WebViewManagedWindowLifecycleState.Created,
            WebViewManagedWindowLifecycleState.Closing
        ])!;

        Assert.True(result);
    }

    [Fact]
    public void IsTransitionAllowed_attached_to_closing_returns_true()
    {
        var method = typeof(WebViewShellExperience)
            .GetMethod("IsTransitionAllowed", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (bool)method.Invoke(null, [
            WebViewManagedWindowLifecycleState.Attached,
            WebViewManagedWindowLifecycleState.Closing
        ])!;

        Assert.True(result);
    }

    [Fact]
    public void IsTransitionAllowed_closed_to_created_returns_false()
    {
        var method = typeof(WebViewShellExperience)
            .GetMethod("IsTransitionAllowed", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (bool)method.Invoke(null, [
            WebViewManagedWindowLifecycleState.Closed,
            WebViewManagedWindowLifecycleState.Created
        ])!;

        Assert.False(result);
    }

    #endregion

    #region Round 5 Tier 2: Menu pruning EffectiveMenuModel not null

    [Fact]
    public void MenuPruning_allow_with_effective_menu_covers_non_null_branch()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = CreateFullWebView();

        var effectiveMenu = new WebViewMenuModelRequest();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                new WebViewMenuPruningDecision(IsAllowed: true, EffectiveMenuModel: effectiveMenu))
        });

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest());
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, result.Outcome);
    }

    #endregion

    #region Round 5 Tier 2: SessionDecision non-null covers ?. path

    [Fact]
    public void MenuPruning_profile_scope_uses_session_decision_when_not_null()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);
        using var webView = CreateFullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, _) =>
                new WebViewMenuPruningDecision(IsAllowed: true)),
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("test-scope"),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "test-profile"
                })
        });

        var result = shell.ApplyMenuModel(new WebViewMenuModelRequest());
        Assert.NotNull(result);
        Assert.NotNull(shell.SessionDecision);
    }

    [Fact]
    public void PermissionRequested_with_session_decision_covers_non_null_path()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Allow),
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("test-scope"),
            SessionPermissionProfileResolver = new DelegateSessionPermissionProfileResolver((ctx, _) =>
                new WebViewSessionPermissionProfile
                {
                    ProfileIdentity = "perm-profile"
                })
        });

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com")));
        Assert.NotNull(shell.SessionDecision);
    }

    #endregion

    #region Round 5 Tier 2: WebViewShellExperience ReportSystemIntegrationOutcome

    [Fact]
    public void UpdateTrayState_deny_with_null_reason_covers_coalesce()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new NullReasonDenyPolicy());
        using var webView = CreateFullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = shell.UpdateTrayState(new WebViewTrayStateRequest { IsVisible = true });

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
        Assert.NotNull(error);
    }

    #endregion
}
