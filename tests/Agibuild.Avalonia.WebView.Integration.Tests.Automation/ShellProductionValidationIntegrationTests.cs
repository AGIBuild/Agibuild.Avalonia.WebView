using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class ShellProductionValidationIntegrationTests
{
    [AvaloniaFact]
    public async Task Shell_scope_attach_detach_soak_keeps_event_wiring_and_cleanup_deterministic()
    {
        var dispatcher = new TestDispatcher();
        var adapter = new MockWebViewAdapterFull();
        using var core = new WebViewCore(adapter, dispatcher);
        var capabilityProvider = new SoakHostCapabilityProvider();

        var createdWindowIds = new HashSet<Guid>();
        var closedWindowIds = new HashSet<Guid>();
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();
        var expectedExternalOpens = 0;

        const int iterations = 50;
        for (var i = 0; i < iterations; i++)
        {
            var current = i;
            var bridge = new WebViewHostCapabilityBridge(capabilityProvider, new SoakCapabilityPolicy());

            using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
            {
                NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) =>
                    current % 2 == 0
                        ? WebViewNewWindowStrategyDecision.ManagedWindow()
                        : WebViewNewWindowStrategyDecision.ExternalBrowser()),
                ManagedWindowFactory = _ => new WebViewCore(MockWebViewAdapter.Create(), dispatcher),
                ManagedWindowCloseTimeout = TimeSpan.FromSeconds(2),
                DownloadPolicy = new DelegateDownloadPolicy((_, e) => e.DownloadPath = $"C:\\soak\\{current}.bin"),
                PermissionPolicy = new DelegatePermissionPolicy((_, e) => e.State = PermissionState.Deny),
                SessionPolicy = new SharedSessionPolicy(),
                SessionContext = new WebViewShellSessionContext("soak-root"),
                HostCapabilityBridge = bridge,
                PolicyErrorHandler = (_, error) => policyErrors.Add(error)
            });

            shell.ManagedWindowLifecycleChanged += (_, e) =>
            {
                if (e.State == WebViewManagedWindowLifecycleState.Created)
                    createdWindowIds.Add(e.WindowId);
                else if (e.State == WebViewManagedWindowLifecycleState.Closed)
                    closedWindowIds.Add(e.WindowId);
            };

            await ThreadingTestHelper.RunOffThread(() =>
            {
                adapter.RaiseNewWindowRequested(new Uri($"https://example.com/shell-soak/{current}"));
                return Task.CompletedTask;
            });

            dispatcher.RunAll();
            if (current % 2 == 0)
            {
                DispatcherTestPump.WaitUntil(dispatcher, () => shell.ManagedWindowCount == 1);
                var managedIds = shell.GetManagedWindowIds();
                Assert.Single(managedIds);

                var closed = await shell.CloseManagedWindowAsync(
                    managedIds[0],
                    cancellationToken: TestContext.Current.CancellationToken);
                Assert.True(closed);
                Assert.Equal(0, shell.ManagedWindowCount);
            }
            else
            {
                expectedExternalOpens++;
                Assert.Equal(expectedExternalOpens, capabilityProvider.ExternalOpens.Count);
            }

            var permissionArgs = new PermissionRequestedEventArgs(WebViewPermissionKind.Camera, new Uri("https://example.com"));
            var downloadArgs = new DownloadRequestedEventArgs(new Uri($"https://example.com/download-{current}.bin"));
            adapter.RaisePermissionRequested(permissionArgs);
            adapter.RaiseDownloadRequested(downloadArgs);
            Assert.Equal(PermissionState.Deny, permissionArgs.State);
            Assert.Equal($"C:\\soak\\{current}.bin", downloadArgs.DownloadPath);
        }

        // After all shell scopes are disposed, event handlers must be detached.
        var postDisposePermission = new PermissionRequestedEventArgs(WebViewPermissionKind.Geolocation, new Uri("https://example.com"));
        var postDisposeDownload = new DownloadRequestedEventArgs(new Uri("https://example.com/post-dispose.bin"));
        adapter.RaisePermissionRequested(postDisposePermission);
        adapter.RaiseDownloadRequested(postDisposeDownload);
        Assert.Equal(PermissionState.Default, postDisposePermission.State);
        Assert.Null(postDisposeDownload.DownloadPath);

        Assert.Empty(policyErrors);
        Assert.NotEmpty(createdWindowIds);
        Assert.Equal(createdWindowIds.Count, closedWindowIds.Count);
    }

    private sealed class SoakCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    private sealed class SoakHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        public List<Uri> ExternalOpens { get; } = [];

        public string? ReadClipboardText() => "soak";

        public void WriteClipboardText(string text)
        {
        }

        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
            => new() { IsCanceled = true };

        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
            => new() { IsCanceled = true };

        public void OpenExternal(Uri uri)
            => ExternalOpens.Add(uri);

        public void ShowNotification(WebViewNotificationRequest request)
        {
        }

        public void ApplyMenuModel(WebViewMenuModelRequest request)
        {
        }

        public void UpdateTrayState(WebViewTrayStateRequest request)
        {
        }

        public void ExecuteSystemAction(WebViewSystemActionRequest request)
        {
        }
    }
}
