using System.Text.Json;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

// ==================== Test interfaces ====================

[JsExport]
public interface IAppService
{
    Task<UserProfile> GetCurrentUser();
    Task SaveSettings(AppSettings settings);
    Task<List<Item>> SearchItems(string query, int limit);
}

[JsExport(Name = "api")]
public interface ICustomNameService
{
    Task<string> Ping();
}

public interface INotDecoratedService
{
    Task DoSomething();
}

// ==================== Test DTOs ====================

public record UserProfile(string Name, int Age);
public record AppSettings(string Theme, bool DarkMode);
public record Item(int Id, string Title);

// ==================== Test implementations ====================

public class FakeAppService : IAppService
{
    public UserProfile? LastSavedUser { get; private set; }
    public AppSettings? LastSavedSettings { get; private set; }

    public Task<UserProfile> GetCurrentUser()
        => Task.FromResult(new UserProfile("Alice", 30));

    public Task SaveSettings(AppSettings settings)
    {
        LastSavedSettings = settings;
        return Task.CompletedTask;
    }

    public Task<List<Item>> SearchItems(string query, int limit)
        => Task.FromResult(Enumerable.Range(1, limit).Select(i => new Item(i, $"{query}-{i}")).ToList());
}

public class FakeCustomNameService : ICustomNameService
{
    public Task<string> Ping() => Task.FromResult("pong");
}

public class NotDecoratedStub : INotDecoratedService
{
    public Task DoSomething() => Task.CompletedTask;
}

public class ThrowingAppService : IAppService
{
    public Task<UserProfile> GetCurrentUser()
        => throw new ArgumentException("id must be positive");

    public Task SaveSettings(AppSettings settings)
        => throw new InvalidOperationException("read-only mode");

    public Task<List<Item>> SearchItems(string query, int limit)
        => Task.FromResult(new List<Item>());
}

// ==================== Tests ====================

public sealed class BridgeContractTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithBridge()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        return (core, adapter);
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

    // ==================== Auto-enable bridge ====================

    [Fact]
    public void Bridge_access_auto_enables_web_message_bridge()
    {
        var (core, adapter) = CreateCoreWithBridge();

        // Bridge access should NOT throw and should auto-enable.
        var bridge = core.Bridge;

        Assert.NotNull(bridge);
        Assert.NotNull(core.Rpc); // RPC should now be available.
    }

    [Fact]
    public void Bridge_uses_existing_bridge_when_pre_enabled()
    {
        var (core, adapter) = CreateCoreWithRpc();

        var bridge1 = core.Bridge;
        var bridge2 = core.Bridge;

        // Same instance.
        Assert.Same(bridge1, bridge2);
    }

    // ==================== Attribute validation ====================

    [Fact]
    public void Expose_without_JsExport_throws_InvalidOperationException()
    {
        var (core, _) = CreateCoreWithRpc();

        // Use a non-null stub that is *not* decorated with [JsExport].
        var stub = new NotDecoratedStub();
        var ex = Assert.Throws<InvalidOperationException>(
            () => core.Bridge.Expose<INotDecoratedService>(stub));

        Assert.Contains("JsExport", ex.Message);
    }

    [Fact]
    public void Expose_with_null_implementation_throws()
    {
        var (core, _) = CreateCoreWithRpc();

        Assert.Throws<ArgumentNullException>(
            () => core.Bridge.Expose<IAppService>(null!));
    }

    // ==================== Handler registration ====================

    [Fact]
    public void Expose_registers_RPC_handlers_and_injects_JS_stub()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());

        // JS stub should be injected.
        var jsStub = capturedScripts.Last();
        Assert.Contains("agWebView.bridge.AppService", jsStub);
        Assert.Contains("getCurrentUser", jsStub);
        Assert.Contains("saveSettings", jsStub);
        Assert.Contains("searchItems", jsStub);
    }

    [Fact]
    public void Expose_with_custom_name_uses_custom_name_in_RPC()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<ICustomNameService>(new FakeCustomNameService());

        var jsStub = capturedScripts.Last();
        Assert.Contains("agWebView.bridge.api", jsStub);
        Assert.Contains("api.ping", jsStub);
    }

    // ==================== Method naming (camelCase) ====================

    [Fact]
    public void CamelCase_conversion_works()
    {
        Assert.Equal("getCurrentUser", RuntimeBridgeService.ToCamelCase("GetCurrentUser"));
        Assert.Equal("saveSettings", RuntimeBridgeService.ToCamelCase("SaveSettings"));
        Assert.Equal("a", RuntimeBridgeService.ToCamelCase("A"));
        Assert.Equal("already", RuntimeBridgeService.ToCamelCase("already"));
        Assert.Equal("", RuntimeBridgeService.ToCamelCase(""));
    }

    // ==================== RPC dispatch for exposed methods ====================

    [Fact]
    public void Exposed_method_is_callable_via_RPC_message()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        capturedScripts.Clear(); // Clear the JS stub injection script.

        // Simulate JS → C# call.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"test-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        // Should have sent back a JSON-RPC response.
        Assert.True(capturedScripts.Count > 0, "Expected at least one script invocation for the response");
        var responseScript = capturedScripts.Last();
        Assert.Contains("_onResponse", responseScript);
        Assert.Contains("Alice", responseScript);
    }

    [Fact]
    public void Exposed_method_with_named_params_works()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"test-2","method":"AppService.searchItems","params":{"query":"hello","limit":3}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = capturedScripts.Last();
        Assert.Contains("_onResponse", responseScript);
        Assert.Contains("hello-1", responseScript);
        Assert.Contains("hello-3", responseScript);
    }

    [Fact]
    public void Exposed_void_method_returns_null_result()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var svc = new FakeAppService();
        core.Bridge.Expose<IAppService>(svc);
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"test-3","method":"AppService.saveSettings","params":{"settings":{"theme":"dark","darkMode":true}}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.NotNull(svc.LastSavedSettings);
        Assert.Equal("dark", svc.LastSavedSettings!.Theme);
        Assert.True(svc.LastSavedSettings.DarkMode);
    }

    // ==================== Error propagation ====================

    [Fact]
    public void Exception_in_exposed_method_returns_JSON_RPC_error()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new ThrowingAppService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"err-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = capturedScripts.Last();
        Assert.Contains("_onResponse", responseScript);
        Assert.Contains("id must be positive", responseScript);
        Assert.Contains("-32603", responseScript);
    }

    // ==================== Duplicate expose ====================

    [Fact]
    public void Double_expose_for_same_interface_throws()
    {
        var (core, _) = CreateCoreWithRpc();

        core.Bridge.Expose<IAppService>(new FakeAppService());

        Assert.Throws<InvalidOperationException>(
            () => core.Bridge.Expose<IAppService>(new FakeAppService()));
    }

    // ==================== Remove ====================

    [Fact]
    public void Remove_unregisters_handlers()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();
        capturedScripts.Clear();

        // Call should now return method-not-found.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"rm-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = capturedScripts.Last();
        Assert.Contains("-32601", responseScript); // Method not found.
    }

    [Fact]
    public void Remove_then_re_expose_works()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();

        // Should not throw — re-expose is allowed after Remove.
        core.Bridge.Expose<IAppService>(new FakeAppService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"re-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var responseScript = capturedScripts.Last();
        Assert.Contains("Alice", responseScript);
    }

    // ==================== Lifecycle / Disposal ====================

    [Fact]
    public void Bridge_operations_after_core_dispose_throw()
    {
        var (core, _) = CreateCoreWithRpc();
        var bridge = core.Bridge;

        core.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => bridge.Expose<IAppService>(new FakeAppService()));

        Assert.Throws<ObjectDisposedException>(
            () => bridge.Remove<IAppService>());
    }

    [Fact]
    public void Bridge_access_after_core_dispose_throws()
    {
        var (core, _) = CreateCoreWithBridge();
        core.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = core.Bridge);
    }

    // ==================== Service name derivation ====================

    [Fact]
    public void Service_name_strips_leading_I()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());

        var jsStub = capturedScripts.Last();
        // Should be "AppService", not "IAppService".
        Assert.Contains("bridge.AppService", jsStub);
        Assert.DoesNotContain("bridge.IAppService", jsStub);
    }
}
