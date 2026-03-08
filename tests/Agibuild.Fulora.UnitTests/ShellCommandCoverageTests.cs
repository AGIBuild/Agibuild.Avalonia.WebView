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
    public async Task Command_returns_false_when_disposed()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow())
        });
        shell.Dispose();

        Assert.False(await shell.ExecuteCommandAsync(WebViewCommand.Copy));
        Assert.Empty(commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_without_policy_allows_by_default()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions());

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.True(executed);
        Assert.Equal([WebViewCommand.Copy], commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_all_types_execute_via_manager()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow())
        });

        foreach (var cmd in new[] { WebViewCommand.Copy, WebViewCommand.Cut, WebViewCommand.Paste,
            WebViewCommand.SelectAll, WebViewCommand.Undo, WebViewCommand.Redo })
        {
            Assert.True(await shell.ExecuteCommandAsync(cmd));
        }

        Assert.Equal(6, commandManager.ExecutedCommands.Count);
    }

    [Fact]
    public async Task Command_null_decision_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => null!),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Empty(commandManager.ExecutedCommands);
    }

    [Fact]
    public async Task Command_denied_with_null_reason_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) =>
                WebViewShellCommandDecision.Deny(null)),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Paste);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.Command, error!.Domain);
    }

    [Fact]
    public async Task Command_unsupported_type_reports_error()
    {
        var commandManager = new TrackingCommandManager();
        using var webView = new FullWebView { CommandManager = commandManager };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow()),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync((WebViewCommand)999);

        Assert.False(executed);
        Assert.NotNull(error);
        Assert.Equal(WebViewShellPolicyDomain.Command, error!.Domain);
    }

    [Fact]
    public async Task Command_operation_exception_reports_failure()
    {
        using var webView = new FullWebView { CommandManager = new ThrowingCommandManager() };
        WebViewShellPolicyErrorEventArgs? error = null;
        using var shell = new WebViewShellExperience(webView, new WebViewShellExperienceOptions
        {
            CommandPolicy = new DelegateCommandPolicy((_, _) => WebViewShellCommandDecision.Allow()),
            PolicyErrorHandler = (_, e) => error = e
        });

        var executed = await shell.ExecuteCommandAsync(WebViewCommand.Copy);

        Assert.False(executed);
        Assert.NotNull(error);
    }

    [Fact]
    public void DelegateCommandPolicy_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateCommandPolicy(null!));
    }
}
