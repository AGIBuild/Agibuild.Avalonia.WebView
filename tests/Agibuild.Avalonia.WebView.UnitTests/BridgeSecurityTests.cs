using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

/// <summary>
/// Tests for Bridge security features: rate limiting, policy integration.
/// Deliverable 1.4.
/// </summary>
public sealed class BridgeSecurityTests
{
    private readonly TestDispatcher _dispatcher = new();

    // Need a non-SG interface for reflection-based rate limiting test.
    [JsExport]
    public interface IRateLimitedService
    {
        Task<int> Increment();
    }

    public class CounterService : IRateLimitedService
    {
        public int Count;
        public Task<int> Increment() => Task.FromResult(++Count);
    }

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithRpc()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        return (core, adapter);
    }

    // ==================== RateLimit contract ====================

    [Fact]
    public void RateLimit_constructor_validates_positive_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimit(0, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimit(10, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimit(-1, TimeSpan.FromSeconds(1)));

        // Valid
        var rl = new RateLimit(100, TimeSpan.FromSeconds(1));
        Assert.Equal(100, rl.MaxCalls);
        Assert.Equal(TimeSpan.FromSeconds(1), rl.Window);
    }

    [Fact]
    public void BridgeOptions_RateLimit_is_settable()
    {
        var opts = new BridgeOptions
        {
            RateLimit = new RateLimit(50, TimeSpan.FromMinutes(1))
        };
        Assert.NotNull(opts.RateLimit);
        Assert.Equal(50, opts.RateLimit!.MaxCalls);
    }

    // ==================== Rate limiting enforcement ====================

    [Fact]
    public void Rate_limited_service_allows_calls_within_limit()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var svc = new CounterService();
        core.Bridge.Expose<IRateLimitedService>(svc, new BridgeOptions
        {
            RateLimit = new RateLimit(5, TimeSpan.FromSeconds(10))
        });

        // 5 calls should succeed.
        for (int i = 1; i <= 5; i++)
        {
            capturedScripts.Clear();
            adapter.RaiseWebMessage(
                $"{{\"jsonrpc\":\"2.0\",\"id\":\"rl-{i}\",\"method\":\"RateLimitedService.increment\",\"params\":{{}}}}",
                "*", core.ChannelId);
            _dispatcher.RunAll();

            var response = capturedScripts.Last();
            Assert.Contains("_onResponse", response);
            Assert.DoesNotContain("-32029", response);
        }

        Assert.Equal(5, svc.Count);
    }

    [Fact]
    public void Rate_limited_service_rejects_calls_exceeding_limit()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var svc = new CounterService();
        core.Bridge.Expose<IRateLimitedService>(svc, new BridgeOptions
        {
            RateLimit = new RateLimit(2, TimeSpan.FromSeconds(60))
        });

        // 2 calls succeed.
        for (int i = 1; i <= 2; i++)
        {
            adapter.RaiseWebMessage(
                $"{{\"jsonrpc\":\"2.0\",\"id\":\"rl-{i}\",\"method\":\"RateLimitedService.increment\",\"params\":{{}}}}",
                "*", core.ChannelId);
            _dispatcher.RunAll();
        }

        // 3rd call should be rate limited.
        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"rl-3","method":"RateLimitedService.increment","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("-32029", response);
        Assert.Contains("Rate limit exceeded", response);
        Assert.Equal(2, svc.Count); // Only 2 calls went through.
    }

    [Fact]
    public void Service_without_rate_limit_allows_unlimited_calls()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var svc = new CounterService();
        core.Bridge.Expose<IRateLimitedService>(svc); // No rate limit.

        for (int i = 1; i <= 20; i++)
        {
            adapter.RaiseWebMessage(
                $"{{\"jsonrpc\":\"2.0\",\"id\":\"ul-{i}\",\"method\":\"RateLimitedService.increment\",\"params\":{{}}}}",
                "*", core.ChannelId);
            _dispatcher.RunAll();
        }

        Assert.Equal(20, svc.Count);
    }
}
