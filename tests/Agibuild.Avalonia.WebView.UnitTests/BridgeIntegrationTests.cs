using System.Text.Json;
using Agibuild.Avalonia.WebView.Shell;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

[JsExport]
public interface IDesktopHostProbeService
{
    Task<DesktopHostProbeResult> ProbeClipboard();
}

public sealed class DesktopHostProbeResult
{
    public int Outcome { get; init; }
    public string? ClipboardText { get; init; }
    public string? DenyReason { get; init; }
}

/// <summary>
/// Comprehensive integration and edge-case tests for the Bridge system.
/// Deliverable 1.7: exercises multi-service scenarios, lifecycle, and error boundaries.
/// </summary>
public sealed class BridgeIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter, List<string> Scripts) CreateCore()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        var scripts = new List<string>();
        adapter.ScriptCallback = script => { scripts.Add(script); return null; };
        return (core, adapter, scripts);
    }

    // ==================== Multi-service coexistence ====================

    [Fact]
    public void Multiple_JsExport_services_can_be_exposed_simultaneously()
    {
        var (core, adapter, scripts) = CreateCore();

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Expose<ICustomNameService>(new FakeCustomNameService());

        // Drain any queued dispatcher work from Expose.
        _dispatcher.RunAll();
        scripts.Clear();

        // Call first service.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"m-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        Assert.True(scripts.Count > 0, $"Expected scripts after RPC call, got 0");
        Assert.True(scripts.Any(s => s.Contains("Alice")), $"Expected 'Alice' in one of {scripts.Count} scripts");

        // Call second service.
        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"m-2","method":"api.ping","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        Assert.True(scripts.Any(s => s.Contains("pong")), "Expected 'pong' in response");
    }

    [Fact]
    public void Export_and_Import_proxies_can_coexist()
    {
        var (core, adapter, scripts) = CreateCore();

        core.Bridge.Expose<IAppService>(new FakeAppService());
        var proxy = core.Bridge.GetProxy<IUiController>();

        // Export works.
        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ei-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        Assert.True(scripts.Any(s => s.Contains("Alice")), "Expected 'Alice' in response");

        // Import proxy fires RPC call.
        scripts.Clear();
        _ = proxy.ShowNotification("hello");
        Assert.True(scripts.Count > 0);
        Assert.Contains("UiController.showNotification", scripts.Last());
    }

    // ==================== Lifecycle edge cases ====================

    [Fact]
    public void Bridge_survives_multiple_expose_remove_cycles()
    {
        var (core, adapter, scripts) = CreateCore();

        for (int i = 0; i < 3; i++)
        {
            core.Bridge.Expose<IAppService>(new FakeAppService());

            scripts.Clear();
            adapter.RaiseWebMessage(
                $"{{\"jsonrpc\":\"2.0\",\"id\":\"cycle-{i}\",\"method\":\"AppService.getCurrentUser\",\"params\":{{}}}}",
                "*", core.ChannelId);
            _dispatcher.RunAll();
            Assert.True(scripts.Any(s => s.Contains("Alice")), $"Expected 'Alice' in response for cycle {i}");

            core.Bridge.Remove<IAppService>();
        }
    }

    [Fact]
    public void Calling_method_on_removed_service_returns_method_not_found()
    {
        var (core, adapter, scripts) = CreateCore();

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();

        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"gone-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("-32601")), "Expected '-32601' (method not found)");
    }

    [Fact]
    public void Dispose_prevents_all_operations()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => core.Bridge.Expose<IAppService>(new FakeAppService()));
        Assert.Throws<ObjectDisposedException>(() => core.Bridge.GetProxy<IUiController>());
    }

    // ==================== Error boundaries ====================

    [JsExport]
    public interface IFailingService
    {
        Task<string> WillFail();
        Task WillThrowInvalidOp();
    }

    private class FailingImpl : IFailingService
    {
        public Task<string> WillFail() => throw new InvalidOperationException("Boom!");
        public Task WillThrowInvalidOp() => throw new ArgumentException("Bad arg");
    }

    [Fact]
    public void Handler_exception_returns_JSON_RPC_error_with_message()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IFailingService>(new FailingImpl());

        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"err-1","method":"FailingService.willFail","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("Boom!")), "Expected 'Boom!' error in response");
    }

    [Fact]
    public void Different_exception_types_are_reported()
    {
        var (core, adapter, scripts) = CreateCore();
        core.Bridge.Expose<IFailingService>(new FailingImpl());

        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"err-2","method":"FailingService.willThrowInvalidOp","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(scripts.Any(s => s.Contains("Bad arg")), "Expected 'Bad arg' error in response");
    }

    // ==================== Concurrent access ====================

    [Fact]
    public void Bridge_is_thread_safe_for_expose_operations()
    {
        var (core, adapter, scripts) = CreateCore();
        adapter.ScriptCallback = _ => null;

        // Parallel expose shouldn't throw (one might fail with duplicate, that's ok).
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            try
            {
                core.Bridge.Expose<IAppService>(new FakeAppService());
            }
            catch (InvalidOperationException)
            {
                // Expected: "already exposed"
            }
        }));

        Task.WaitAll(tasks.ToArray(), TestContext.Current.CancellationToken);

        // At least one should have succeeded.
        adapter.ScriptCallback = script => { scripts.Add(script); return null; };
        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ts-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("Alice")), TimeSpan.FromSeconds(2));
        Assert.True(scripts.Any(s => s.Contains("Alice")), "Expected 'Alice' in response after concurrent expose");
    }

    private sealed class DesktopHostProbeService : IDesktopHostProbeService
    {
        private readonly WebViewShellExperience _shell;

        public DesktopHostProbeService(WebViewShellExperience shell)
        {
            _shell = shell;
        }

        public Task<DesktopHostProbeResult> ProbeClipboard()
        {
            var result = _shell.ReadClipboardText();
            return Task.FromResult(new DesktopHostProbeResult
            {
                Outcome = (int)result.Outcome,
                ClipboardText = result.Value,
                DenyReason = result.DenyReason
            });
        }
    }

    [Fact]
    public void Web_rpc_call_to_exported_service_routes_through_shell_capability_gateway()
    {
        var (core, adapter, scripts) = CreateCore();
        var bridge = new WebViewHostCapabilityBridge(
            new TestClipboardCapabilityProvider("bridge-text"),
            new AllowAllCapabilityPolicy());
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });
        core.Bridge.Expose<IDesktopHostProbeService>(new DesktopHostProbeService(shell));
        _dispatcher.RunAll();

        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"probe-1","method":"DesktopHostProbeService.probeClipboard","params":{}}""",
            "*",
            core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains(scripts, s => s.Contains("probe-1", StringComparison.Ordinal));
        Assert.Contains(scripts, s =>
            s.Contains("\"clipboardText\":\"bridge-text\"", StringComparison.Ordinal) ||
            s.Contains("\"ClipboardText\":\"bridge-text\"", StringComparison.Ordinal) ||
            s.Contains("bridge-text", StringComparison.Ordinal));
    }

    [Fact]
    public void Web_rpc_call_to_exported_service_returns_deny_outcome_when_policy_blocks_capability()
    {
        var (core, adapter, scripts) = CreateCore();
        var bridge = new WebViewHostCapabilityBridge(
            new TestClipboardCapabilityProvider("blocked"),
            new DenyAllCapabilityPolicy());
        using var shell = new WebViewShellExperience(core, new WebViewShellExperienceOptions
        {
            HostCapabilityBridge = bridge
        });
        core.Bridge.Expose<IDesktopHostProbeService>(new DesktopHostProbeService(shell));
        _dispatcher.RunAll();

        scripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"probe-2","method":"DesktopHostProbeService.probeClipboard","params":{}}""",
            "*",
            core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains(scripts, s => s.Contains("probe-2", StringComparison.Ordinal));
        Assert.Contains(scripts, s =>
            s.Contains("\"denyReason\":\"denied-by-policy\"", StringComparison.Ordinal) ||
            s.Contains("\"DenyReason\":\"denied-by-policy\"", StringComparison.Ordinal) ||
            s.Contains("denied-by-policy", StringComparison.Ordinal));
    }

    private sealed class AllowAllCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    private sealed class DenyAllCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Deny("denied-by-policy");
    }

    private sealed class TestClipboardCapabilityProvider : IWebViewHostCapabilityProvider
    {
        private readonly string _clipboardText;

        public TestClipboardCapabilityProvider(string clipboardText)
        {
            _clipboardText = clipboardText;
        }

        public string? ReadClipboardText() => _clipboardText;
        public void WriteClipboardText(string text) { }
        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request) => throw new NotSupportedException();
        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request) => throw new NotSupportedException();
        public void OpenExternal(Uri uri) => throw new NotSupportedException();
        public void ShowNotification(WebViewNotificationRequest request) => throw new NotSupportedException();
    }
}
