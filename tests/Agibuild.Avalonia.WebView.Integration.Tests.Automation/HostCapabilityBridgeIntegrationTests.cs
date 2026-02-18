using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class HostCapabilityBridgeIntegrationTests
{
    [AvaloniaFact]
    public void Host_capability_bridge_representative_flow_enforces_policy_and_returns_typed_results()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);

        var provider = new IntegrationHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new IntegrationCapabilityPolicy());

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
        });

        var read = shell.ReadClipboardText();
        var write = shell.WriteClipboardText("integration-value");
        var open = shell.ShowOpenFileDialog(new WebViewOpenFileDialogRequest { Title = "Open integration file" });
        var save = shell.ShowSaveFileDialog(new WebViewSaveFileDialogRequest { SuggestedFileName = "integration.txt" });
        var deniedNotification = shell.ShowNotification(new WebViewNotificationRequest
        {
            Title = "Denied",
            Message = "Notification blocked in policy"
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/external"));
        dispatcher.RunAll();

        Assert.True(read.IsAllowed && read.IsSuccess);
        Assert.Equal("integration-clipboard", read.Value);
        Assert.True(write.IsAllowed && write.IsSuccess);

        Assert.True(open.IsAllowed && open.IsSuccess);
        Assert.Single(open.Value!.Paths);
        Assert.True(save.IsAllowed && save.IsSuccess);
        Assert.False(save.Value!.IsCanceled);

        Assert.False(deniedNotification.IsAllowed);
        Assert.False(deniedNotification.IsSuccess);
        Assert.Equal("notification-disabled", deniedNotification.DenyReason);

        Assert.Single(provider.ExternalOpens);
        Assert.Equal(new Uri("https://example.com/external"), provider.ExternalOpens[0]);
        Assert.Equal(0, adapter.NavigateCallCount);
    }

    [AvaloniaFact]
    public async Task Host_capability_bridge_stress_external_open_cycles_remain_deterministic()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new IntegrationHostCapabilityProvider();

        const int iterations = 30;
        for (var i = 0; i < iterations; i++)
        {
            var current = i;
            var bridge = new WebViewHostCapabilityBridge(
                provider,
                new IntegrationCapabilityPolicy(uri =>
                {
                    if (uri is null)
                        return WebViewHostCapabilityDecision.Allow();
                    return current % 4 == 0
                        ? WebViewHostCapabilityDecision.Deny("stress-deny")
                        : WebViewHostCapabilityDecision.Allow();
                }));

            using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
            {
                HostCapabilityBridge = bridge,
                NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser())
            });

            await ThreadingTestHelper.RunOffThread(() =>
            {
                adapter.RaiseNewWindowRequested(new Uri($"https://example.com/stress/{current}"));
                return Task.CompletedTask;
            });

            dispatcher.RunAll();
            Assert.Equal(0, adapter.NavigateCallCount);
            Assert.Equal(0, shell.ManagedWindowCount);
        }

        Assert.Equal(iterations - (iterations / 4 + 1), provider.ExternalOpens.Count);
    }

    private sealed class IntegrationCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        private readonly Func<Uri?, WebViewHostCapabilityDecision>? _externalDecision;

        public IntegrationCapabilityPolicy(Func<Uri?, WebViewHostCapabilityDecision>? externalDecision = null)
        {
            _externalDecision = externalDecision;
        }

        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
        {
            return context.Operation switch
            {
                WebViewHostCapabilityOperation.NotificationShow => WebViewHostCapabilityDecision.Deny("notification-disabled"),
                WebViewHostCapabilityOperation.ExternalOpen when _externalDecision is not null => _externalDecision(context.RequestUri),
                _ => WebViewHostCapabilityDecision.Allow()
            };
        }
    }

    private sealed class IntegrationHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        public List<Uri> ExternalOpens { get; } = [];

        public string? ReadClipboardText() => "integration-clipboard";

        public void WriteClipboardText(string text)
        {
        }

        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
            => new()
            {
                IsCanceled = false,
                Paths = ["C:\\integration\\open.txt"]
            };

        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
            => new()
            {
                IsCanceled = false,
                Paths = ["C:\\integration\\save.txt"]
            };

        public void OpenExternal(Uri uri)
            => ExternalOpens.Add(uri);

        public void ShowNotification(WebViewNotificationRequest request)
        {
        }
    }
}
