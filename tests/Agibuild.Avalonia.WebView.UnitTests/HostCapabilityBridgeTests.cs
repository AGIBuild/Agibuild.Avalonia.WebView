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

        Assert.True(read.IsAllowed && read.IsSuccess);
        Assert.Equal("from-clipboard", read.Value);
        Assert.True(write.IsAllowed && write.IsSuccess);
        Assert.True(open.IsAllowed && open.IsSuccess);
        Assert.Single(open.Value!.Paths);
        Assert.True(save.IsAllowed && save.IsSuccess);
        Assert.False(save.Value!.IsCanceled);
        Assert.True(notify.IsAllowed && notify.IsSuccess);
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

        Assert.True(failed.IsAllowed);
        Assert.False(failed.IsSuccess);
        Assert.NotNull(failed.Error);
        Assert.True(WebViewOperationFailure.TryGetCategory(failed.Error!, out var category));
        Assert.Equal(WebViewOperationFailureCategory.AdapterFailed, category);

        Assert.True(external.IsAllowed);
        Assert.True(external.IsSuccess);
    }

    [Fact]
    public void Policy_exception_converts_to_deny_with_reason()
    {
        var provider = new TestHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider, new ThrowingPolicy());

        var result = bridge.ShowNotification(
            new WebViewNotificationRequest { Title = "A", Message = "B" },
            Guid.NewGuid());

        Assert.False(result.IsAllowed);
        Assert.False(result.IsSuccess);
        Assert.Contains("policy exploded", result.DenyReason, StringComparison.Ordinal);
        Assert.Equal(0, provider.CallCount);
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
