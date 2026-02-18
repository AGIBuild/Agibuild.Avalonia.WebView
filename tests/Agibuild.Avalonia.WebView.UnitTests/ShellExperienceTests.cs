using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class ShellExperienceTests
{
    [Fact]
    public void NavigateInPlace_policy_preserves_v1_fallback_navigation()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new NavigateInPlaceNewWindowPolicy()
        });

        NewWindowRequestedEventArgs? observedArgs = null;
        core.NewWindowRequested += (_, e) => observedArgs = e;

        var uri = new Uri("https://example.com/");
        adapter.RaiseNewWindowRequested(uri);

        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 1);

        Assert.NotNull(observedArgs);
        Assert.False(observedArgs!.Handled);
        Assert.Equal(uri, adapter.LastNavigationUri);
    }

    [Fact]
    public void Delegate_policy_can_handle_new_window_and_suppress_fallback()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var called = false;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, e) =>
            {
                called = true;
                e.Handled = true;
            })
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/"));

        DispatcherTestPump.WaitUntil(dispatcher, () => called);

        dispatcher.RunAll();
        Assert.Equal(0, adapter.NavigateCallCount);
    }

    [Fact]
    public void Download_handler_can_set_path_and_cancel()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadHandler = (_, e) =>
            {
                e.DownloadPath = "C:\\temp\\file.bin";
                e.Cancel = true;
            }
        });

        DownloadRequestedEventArgs? observedArgs = null;
        core.DownloadRequested += (_, e) => observedArgs = e;

        adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin")));

        Assert.NotNull(observedArgs);
        Assert.Equal("C:\\temp\\file.bin", observedArgs!.DownloadPath);
        Assert.True(observedArgs.Cancel);
    }

    [Fact]
    public void Download_policy_runs_before_delegate_handler_in_deterministic_order()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        var order = new List<string>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadPolicy = new DelegateDownloadPolicy((_, e) =>
            {
                order.Add("policy");
                e.DownloadPath = "C:\\policy\\from-policy.bin";
            }),
            DownloadHandler = (_, e) =>
            {
                order.Add("handler");
                e.Cancel = true;
            }
        });

        var args = new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin"));
        adapter.RaiseDownloadRequested(args);

        Assert.Equal(new[] { "policy", "handler" }, order);
        Assert.Equal("C:\\policy\\from-policy.bin", args.DownloadPath);
        Assert.True(args.Cancel);
    }

    [Fact]
    public void Permission_handler_can_allow_or_deny()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PermissionHandler = (_, e) => e.State = PermissionState.Deny
        });

        PermissionRequestedEventArgs? observedArgs = null;
        core.PermissionRequested += (_, e) => observedArgs = e;

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com")));

        Assert.NotNull(observedArgs);
        Assert.Equal(PermissionState.Deny, observedArgs!.State);
    }

    [Fact]
    public void Permission_policy_runs_before_delegate_handler_in_deterministic_order()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        var order = new List<string>();
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PermissionPolicy = new DelegatePermissionPolicy((_, e) =>
            {
                order.Add("policy");
                e.State = PermissionState.Allow;
            }),
            PermissionHandler = (_, e) =>
            {
                order.Add("handler");
                e.State = PermissionState.Deny;
            }
        });

        var args = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera);
        adapter.RaisePermissionRequested(args);

        Assert.Equal(new[] { "policy", "handler" }, order);
        Assert.Equal(PermissionState.Deny, args.State);
    }

    [Fact]
    public void Shell_experience_with_empty_options_is_non_breaking_for_all_domains()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions());

        var downloadArgs = new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin"));
        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
        var uri = new Uri("https://example.com/");

        adapter.RaiseDownloadRequested(downloadArgs);
        adapter.RaisePermissionRequested(permissionArgs);
        adapter.RaiseNewWindowRequested(uri);
        DispatcherTestPump.WaitUntil(dispatcher, () => adapter.NavigateCallCount == 1);

        Assert.Equal(uri, adapter.LastNavigationUri);
        Assert.False(downloadArgs.Cancel);
        Assert.Null(downloadArgs.DownloadPath);
        Assert.Equal(PermissionState.Default, permissionArgs.State);
    }

    [Fact]
    public void Policy_failure_isolated_and_reported_without_breaking_other_domains()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        WebViewShellPolicyErrorEventArgs? observedError = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadPolicy = new DelegateDownloadPolicy((_, _) => throw new InvalidOperationException("download policy failed")),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
            PolicyErrorHandler = (_, error) => observedError = error
        });

        var downloadArgs = new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin"));
        var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Microphone, new Uri("https://example.com"));

        adapter.RaiseDownloadRequested(downloadArgs);
        adapter.RaisePermissionRequested(permissionArgs);

        Assert.NotNull(observedError);
        Assert.Equal(WebViewShellPolicyDomain.Download, observedError!.Domain);
        Assert.True(WebViewOperationFailure.TryGetCategory(observedError.Exception, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
        Assert.Equal(PermissionState.Deny, permissionArgs.State);
    }

    [Fact]
    public void Session_policy_resolution_is_deterministic_and_propagates_scope_identity()
    {
        var dispatcher = new TestDispatcher();
        var adapter1 = MockWebViewAdapter.Create();
        var adapter2 = MockWebViewAdapter.Create();
        using var core1 = new WebViewCore(adapter1, dispatcher);
        using var core2 = new WebViewCore(adapter2, dispatcher);

        var options = new WebViewShellExperienceOptions
        {
            SessionPolicy = new IsolatedSessionPolicy(),
            SessionContext = new WebViewShellSessionContext("tenant-a")
        };

        using var shell1 = new WebViewShellExperience(core1, options);
        using var shell2 = new WebViewShellExperience(core2, options);

        Assert.NotNull(shell1.SessionDecision);
        Assert.NotNull(shell2.SessionDecision);
        Assert.Equal(shell1.SessionDecision, shell2.SessionDecision);
        Assert.Equal(WebViewShellSessionScope.Isolated, shell1.SessionDecision!.Scope);
        Assert.Equal("isolated:tenant-a", shell1.SessionDecision.ScopeIdentity);
    }

    [Fact]
    public async Task New_window_policy_executes_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        int? observedThreadId = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _) => observedThreadId = Environment.CurrentManagedThreadId)
        });

        await ThreadingTestHelper.RunOffThread(() =>
        {
            adapter.RaiseNewWindowRequested(new Uri("https://example.com/"));
            return Task.CompletedTask;
        });

        DispatcherTestPump.WaitUntil(dispatcher, () => observedThreadId.HasValue);
        Assert.Equal(dispatcher.UiThreadId, observedThreadId);
    }

    [Fact]
    public async Task Download_policy_executes_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        int? observedThreadId = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            DownloadPolicy = new DelegateDownloadPolicy((_, _) => observedThreadId = Environment.CurrentManagedThreadId)
        });

        await ThreadingTestHelper.RunOffThread(() =>
        {
            adapter.RaiseDownloadRequested(new DownloadRequestedEventArgs(new Uri("https://example.com/file.bin")));
            return Task.CompletedTask;
        });

        DispatcherTestPump.WaitUntil(dispatcher, () => observedThreadId.HasValue);
        Assert.Equal(dispatcher.UiThreadId, observedThreadId);
    }

    [Fact]
    public async Task Permission_policy_executes_on_ui_thread()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        int? observedThreadId = null;
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PermissionPolicy = new DelegatePermissionPolicy((_, _) => observedThreadId = Environment.CurrentManagedThreadId)
        });

        await ThreadingTestHelper.RunOffThread(() =>
        {
            adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera));
            return Task.CompletedTask;
        });

        DispatcherTestPump.WaitUntil(dispatcher, () => observedThreadId.HasValue);
        Assert.Equal(dispatcher.UiThreadId, observedThreadId);
    }

    [Fact]
    public void Disposing_shell_experience_unsubscribes_handlers()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);

        var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            PermissionHandler = (_, e) => e.State = PermissionState.Allow
        });

        shell.Dispose();

        PermissionRequestedEventArgs? observedArgs = null;
        core.PermissionRequested += (_, e) => observedArgs = e;

        adapter.RaisePermissionRequested(new PermissionRequestedEventArgs(WebViewPermissionKind.Camera));

        Assert.NotNull(observedArgs);
        Assert.Equal(PermissionState.Default, observedArgs!.State);
    }
}

