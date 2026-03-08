using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class ShellExperienceBranchCoverageTests
{
    [Fact]
    public async Task DevTools_operations_return_false_when_disposed()
    {
        using var webView = new FullWebView();
        var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        shell.Dispose();

        Assert.False(await shell.OpenDevToolsAsync());
        Assert.False(await shell.CloseDevToolsAsync());
        Assert.False(await shell.IsDevToolsOpenAsync());
    }

    [Fact]
    public async Task DevTools_without_policy_allows_by_default()
    {
        using var webView = new FullWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());

        var opened = await shell.OpenDevToolsAsync();
        var state = await shell.IsDevToolsOpenAsync();
        var closed = await shell.CloseDevToolsAsync();

        Assert.True(opened);
        Assert.True(state);
        Assert.True(closed);
    }

    [Fact]
    public async Task CloseDevTools_deny_reports_error()
    {
        using var webView = new FullWebView();
        var errors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, ctx) =>
                ctx.Action == WebViewShellDevToolsAction.Close
                    ? WebViewShellDevToolsDecision.Deny("close-blocked")
                    : WebViewShellDevToolsDecision.Allow()),
            PolicyErrorHandler = (_, e) => errors.Add(e)
        });

        await shell.OpenDevToolsAsync();
        var closed = await shell.CloseDevToolsAsync();

        Assert.False(closed);
        Assert.Contains(errors, e => e.Domain == WebViewShellPolicyDomain.DevTools);
    }

    [Fact]
    public async Task DevTools_policy_returns_null_blocks_operation()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var opened = await shell.OpenDevToolsAsync();

        Assert.False(opened);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task DevTools_operation_exception_reports_failure()
    {
        using var webView = new ThrowingDevToolsWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        WebViewShellPolicyErrorEventArgs? error = null;
        shell.PolicyError += (_, e) => error = e;

        var opened = await shell.OpenDevToolsAsync();

        Assert.False(opened);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.DevTools, error!.Domain);
    }

    [Fact]
    public async Task DevTools_query_exception_reports_failure()
    {
        using var webView = new ThrowingDevToolsWebView();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Allow())
        });
        WebViewShellPolicyErrorEventArgs? error = null;
        shell.PolicyError += (_, e) => error = e;

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CloseDevTools_null_decision_reports_error()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var closed = await shell.CloseDevToolsAsync();

        Assert.False(closed);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task IsDevToolsOpen_null_decision_reports_error()
    {
        using var webView = new FullWebView();
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task IsDevToolsOpen_deny_reports_error()
    {
        using var webView = new FullWebView();
        var errors = new List<WebViewShellPolicyErrorEventArgs>();
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            DevToolsPolicy = new DelegateDevToolsPolicy((_, _) => WebViewShellDevToolsDecision.Deny("query-blocked")),
            PolicyErrorHandler = (_, e) => errors.Add(e)
        });

        var result = await shell.IsDevToolsOpenAsync();

        Assert.False(result);
        Assert.Contains(errors, e => e.Domain == WebViewShellPolicyDomain.DevTools);
    }

    [Fact]
    public void DelegateDevToolsPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateDevToolsPolicy(null!));
    }
}
