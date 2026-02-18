using System;
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

