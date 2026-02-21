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
        var diagnostics = new List<WebViewHostCapabilityDiagnosticEventArgs>();
        bridge.CapabilityCallCompleted += (_, e) => diagnostics.Add(e);

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

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, read.Outcome);
        Assert.True(read.IsAllowed && read.IsSuccess);
        Assert.Equal("integration-clipboard", read.Value);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, write.Outcome);
        Assert.True(write.IsAllowed && write.IsSuccess);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, open.Outcome);
        Assert.True(open.IsAllowed && open.IsSuccess);
        Assert.Single(open.Value!.Paths);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, save.Outcome);
        Assert.True(save.IsAllowed && save.IsSuccess);
        Assert.False(save.Value!.IsCanceled);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, deniedNotification.Outcome);
        Assert.False(deniedNotification.IsAllowed);
        Assert.False(deniedNotification.IsSuccess);
        Assert.Equal("notification-disabled", deniedNotification.DenyReason);

        Assert.Single(provider.ExternalOpens);
        Assert.Equal(new Uri("https://example.com/external"), provider.ExternalOpens[0]);
        Assert.Equal(0, adapter.NavigateCallCount);

        Assert.Equal(6, diagnostics.Count);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, diagnostics[4].Outcome);
        Assert.Equal(WebViewHostCapabilityOperation.NotificationShow, diagnostics[4].Operation);
        Assert.Equal("notification-disabled", diagnostics[4].DenyReason);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, diagnostics[5].Outcome);
        Assert.Equal(WebViewHostCapabilityOperation.ExternalOpen, diagnostics[5].Operation);
        Assert.All(diagnostics, d => Assert.True(d.DurationMilliseconds >= 0));
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

    [AvaloniaFact]
    public void Host_capability_policy_failure_blocks_provider_and_reports_external_open_domain_error()
    {
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, dispatcher);
        var provider = new IntegrationHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(
            provider,
            new IntegrationCapabilityPolicy(_ => throw new InvalidOperationException("external-policy-fault")));
        var policyErrors = new List<WebViewShellPolicyErrorEventArgs>();

        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge,
            NewWindowPolicy = new DelegateNewWindowPolicy((_, _, _) => WebViewNewWindowStrategyDecision.ExternalBrowser()),
            PolicyErrorHandler = (_, error) => policyErrors.Add(error)
        });

        adapter.RaiseNewWindowRequested(new Uri("https://example.com/policy-fault"));
        dispatcher.RunAll();

        Assert.Empty(provider.ExternalOpens);
        Assert.Equal(0, adapter.NavigateCallCount);
        Assert.Single(policyErrors);
        Assert.Equal(WebViewShellPolicyDomain.ExternalOpen, policyErrors[0].Domain);
        Assert.True(WebViewOperationFailure.TryGetCategory(policyErrors[0].Exception, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
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
