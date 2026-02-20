using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class ShellPolicyIntegrationTests
{
    [AvaloniaFact]
    public void Shell_policy_representative_flow_applies_all_domains_end_to_end()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new NavigateInPlaceNewWindowPolicy(),
            DownloadPolicy = new DelegateDownloadPolicy((_, e) =>
            {
                e.DownloadPath = "C:\\downloads\\policy.bin";
                e.Cancel = true;
            }),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("team-a")
        });

        var newWindowUri = new Uri("https://example.com/new-window");
        var downloadArgs = new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin"));
        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));

        adapter.RaiseNewWindowRequested(newWindowUri);
        adapter.RaiseDownloadRequested(downloadArgs);
        adapter.RaisePermissionRequested(permissionArgs);
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 1);

        Assert.Equal(newWindowUri, adapter.LastNavigationUri);
        Assert.Equal("C:\\downloads\\policy.bin", downloadArgs.DownloadPath);
        Assert.True(downloadArgs.Cancel);
        Assert.Equal(PermissionState.Deny, permissionArgs.State);
        Assert.NotNull(shell.SessionDecision);
        Assert.Equal(WebViewShellSessionScope.Isolated, shell.SessionDecision!.Scope);
        Assert.Equal("isolated:team-a", shell.SessionDecision.ScopeIdentity);
    }

    [AvaloniaFact]
    public async Task Shell_policy_stress_cycle_preserves_fallback_and_handler_isolation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        const int iterations = 40;
        var policyErrorLog = new List<WebViewShellPolicyErrorEventArgs>();

        for (var i = 0; i < iterations; i++)
        {
            var current = i;
            using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
            {
                NewWindowPolicy = new NavigateInPlaceNewWindowPolicy(),
                DownloadPolicy = new DelegateDownloadPolicy((_, e) =>
                {
                    if (current % 5 == 0)
                        throw new InvalidOperationException("simulated download policy fault");
                    e.DownloadPath = $"C:\\downloads\\run-{current}.bin";
                }),
                PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
                PolicyErrorHandler = (_, error) => policyErrorLog.Add(error)
            });

            var newWindowUri = new Uri($"https://example.com/stress/{current}");
            var downloadArgs = new DownloadRequestedEventArgs(new Uri($"https://example.com/file-{current}.bin"));
            var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Geolocation, new Uri("https://example.com"));

            await ThreadingTestHelper.RunOffThread(() =>
            {
                adapter.RaiseNewWindowRequested(newWindowUri);
                adapter.RaiseDownloadRequested(downloadArgs);
                adapter.RaisePermissionRequested(permissionArgs);
                return Task.CompletedTask;
            });

            DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == current + 1);
            Assert.Equal(newWindowUri, adapter.LastNavigationUri);
            Assert.Equal(PermissionState.Deny, permissionArgs.State);

            if (current % 5 == 0)
            {
                Assert.Null(downloadArgs.DownloadPath);
            }
            else
            {
                Assert.Equal($"C:\\downloads\\run-{current}.bin", downloadArgs.DownloadPath);
            }
        }

        // After all shell scopes are disposed, policy handlers must no longer mutate events.
        var postDisposePermission = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera);
        adapter.RaisePermissionRequested(postDisposePermission);
        Assert.Equal(PermissionState.Default, postDisposePermission.State);

        Assert.Equal(8, policyErrorLog.Count);
        foreach (var policyError in policyErrorLog)
        {
            Assert.Equal(WebViewShellPolicyDomain.Download, policyError.Domain);
            Assert.True(WebViewOperationFailure.TryGetCategory(policyError.Exception, out var category));
            Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
        }
    }

    [AvaloniaFact]
    public async Task DevTools_policy_deny_is_isolated_and_permission_domain_remains_deterministic()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? observedError = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Deny("devtools-blocked")),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
            PolicyErrorHandler = (_, error) => observedError = error
        });

        var opened = await shell.OpenDevToolsAsync();
        var openState = await shell.IsDevToolsOpenAsync();

        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(permissionArgs);

        Assert.False(opened);
        Assert.False(openState);
        Assert.Equal(PermissionState.Deny, permissionArgs.State);
        Assert.NotNull(observedError);
        Assert.Equal(WebViewShellPolicyDomain.DevTools, observedError!.Domain);
    }

    [AvaloniaFact]
    public async Task Command_policy_deny_is_isolated_and_permission_domain_remains_deterministic()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? observedError = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Deny("shortcut-denied")),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
            PolicyErrorHandler = (_, error) => observedError = error
        });

        var commandExecuted = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        adapter.RaisePermissionRequested(permissionArgs);

        Assert.False(commandExecuted);
        Assert.Equal(PermissionState.Deny, permissionArgs.State);
        Assert.NotNull(observedError);
        Assert.Equal(WebViewShellPolicyDomain.Command, observedError!.Domain);
    }
}
