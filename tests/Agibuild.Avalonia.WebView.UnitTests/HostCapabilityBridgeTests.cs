using System;
using System.Collections.Generic;
using Agibuild.Avalonia.WebView.Shell;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

public sealed class HostCapabilityBridgeTests
{
    [Fact]
    public void Typed_capability_calls_succeed_when_policy_allows()
    {
        var provider = new TestHostCapabilityProvider();
        var policy = new AllowAllPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, policy);
        var root = Guid.NewGuid();

        var read = bridge.ReadClipboardText(root);
        var write = bridge.WriteClipboardText("hello", root);
        var open = bridge.ShowOpenFileDialog(new WebViewOpenFileDialogRequest { Title = "Open" }, root);
        var save = bridge.ShowSaveFileDialog(new WebViewSaveFileDialogRequest { Title = "Save", SuggestedFileName = "a.txt" }, root);
        var notify = bridge.ShowNotification(new WebViewNotificationRequest { Title = "T", Message = "M" }, root);
        var external = bridge.OpenExternal(new Uri("https://example.com"), root);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, read.Outcome);
        Assert.True(read.IsAllowed && read.IsSuccess);
        Assert.Equal("from-clipboard", read.Value);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, write.Outcome);
        Assert.True(write.IsAllowed && write.IsSuccess);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, open.Outcome);
        Assert.True(open.IsAllowed && open.IsSuccess);
        Assert.Single(open.Value!.Paths);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, save.Outcome);
        Assert.True(save.IsAllowed && save.IsSuccess);
        Assert.False(save.Value!.IsCanceled);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, notify.Outcome);
        Assert.True(notify.IsAllowed && notify.IsSuccess);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, external.Outcome);
        Assert.True(external.IsAllowed && external.IsSuccess);

        Assert.Equal(6, provider.CallCount);
    }

    [Fact]
    public void Denied_policy_skips_provider_and_returns_deny_reason()
    {
        var provider = new TestHostCapabilityProvider();
        var policy = new DenyAllPolicy();
        var bridge = new WebViewHostCapabilityBridge(provider, policy);

        var result = bridge.OpenExternal(new Uri("https://example.com"), Guid.NewGuid());

        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
        Assert.False(result.IsAllowed);
        Assert.False(result.IsSuccess);
        Assert.Equal("denied-by-policy", result.DenyReason);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void Provider_failure_isolated_and_classified()
    {
        var provider = new TestHostCapabilityProvider
        {
            ThrowOn = WebViewHostCapabilityOperation.ClipboardReadText
        };
        var bridge = new WebViewHostCapabilityBridge(provider, new AllowAllPolicy());
        var root = Guid.NewGuid();

        var failed = bridge.ReadClipboardText(root);
        var external = bridge.OpenExternal(new Uri("https://example.com"), root);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, failed.Outcome);
        Assert.True(failed.IsAllowed);
        Assert.False(failed.IsSuccess);
        Assert.NotNull(failed.Error);
        Assert.True(WebViewOperationFailure.TryGetCategory(failed.Error!, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, external.Outcome);
        Assert.True(external.IsAllowed);
        Assert.True(external.IsSuccess);
    }

    [Fact]
    public void Policy_exception_returns_failure_without_provider_execution()
    {
        var provider = new TestHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new ThrowingPolicy());

        var result = bridge.ShowNotification(
            new WebViewNotificationRequest { Title = "A", Message = "B" },
            Guid.NewGuid());

        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, result.Outcome);
        Assert.False(result.IsAllowed);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.True(WebViewOperationFailure.TryGetCategory(result.Error!, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void Capability_diagnostics_are_machine_checkable_with_allow_deny_and_failure_outcomes()
    {
        var provider = new TestHostCapabilityProvider
        {
            ThrowOn = WebViewHostCapabilityOperation.ExternalOpen
        };
        var bridge = new WebViewHostCapabilityBridge(provider, new SelectivePolicy());
        var diagnostics = new List<WebViewHostCapabilityDiagnosticEventArgs>();
        bridge.CapabilityCallCompleted += (_, e) => diagnostics.Add(e);
        var root = Guid.NewGuid();

        var read = bridge.ReadClipboardText(root);
        var denied = bridge.ShowNotification(new WebViewNotificationRequest { Title = "T", Message = "M" }, root);
        var failed = bridge.OpenExternal(new Uri("https://example.com"), root);

        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, read.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, denied.Outcome);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, failed.Outcome);

        Assert.Equal(3, diagnostics.Count);
        Assert.Equal(WebViewHostCapabilityOperation.ClipboardReadText, diagnostics[0].Operation);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Allow, diagnostics[0].Outcome);
        Assert.True(diagnostics[0].WasAuthorized);

        Assert.Equal(WebViewHostCapabilityOperation.NotificationShow, diagnostics[1].Operation);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, diagnostics[1].Outcome);
        Assert.False(diagnostics[1].WasAuthorized);
        Assert.Equal("notification-denied", diagnostics[1].DenyReason);

        Assert.Equal(WebViewHostCapabilityOperation.ExternalOpen, diagnostics[2].Operation);
        Assert.Equal(WebViewHostCapabilityCallOutcome.Failure, diagnostics[2].Outcome);
        Assert.True(diagnostics[2].WasAuthorized);
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, diagnostics[2].FailureCategory);

        Assert.All(diagnostics, d =>
        {
            Assert.NotEqual(Guid.Empty, d.CorrelationId);
            Assert.Equal(root, d.RootWindowId);
            Assert.True(d.DurationMilliseconds >= 0);
        });
    }

    private sealed class AllowAllPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    private sealed class DenyAllPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Deny("denied-by-policy");
    }

    private sealed class ThrowingPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => throw new InvalidOperationException("policy exploded");
    }

    private sealed class SelectivePolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation == WebViewHostCapabilityOperation.NotificationShow
                ? WebViewHostCapabilityDecision.Deny("notification-denied")
                : WebViewHostCapabilityDecision.Allow();
    }

    private sealed class TestHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        public int CallCount { get; private set; }
        public WebViewHostCapabilityOperation? ThrowOn { get; init; }
        public List<Uri> ExternalOpens { get; } = [];

        public string? ReadClipboardText()
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.ClipboardReadText);
            CallCount++;
            return "from-clipboard";
        }

        public void WriteClipboardText(string text)
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.ClipboardWriteText);
            CallCount++;
        }

        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.FileDialogOpen);
            CallCount++;
            return new WebViewFileDialogResult
            {
                IsCanceled = false,
                Paths = ["C:\\temp\\open.txt"]
            };
        }

        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.FileDialogSave);
            CallCount++;
            return new WebViewFileDialogResult
            {
                IsCanceled = false,
                Paths = ["C:\\temp\\save.txt"]
            };
        }

        public void OpenExternal(Uri uri)
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.ExternalOpen);
            CallCount++;
            ExternalOpens.Add(uri);
        }

        public void ShowNotification(WebViewNotificationRequest request)
        {
            ThrowIfNeeded(WebViewHostCapabilityOperation.NotificationShow);
            CallCount++;
        }

        private void ThrowIfNeeded(WebViewHostCapabilityOperation op)
        {
            if (ThrowOn == op)
                throw new InvalidOperationException($"Provider failure on {op}");
        }
    }
}
