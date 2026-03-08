using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class RuntimeCoverageTests
{
    [Fact]
    public void RuntimeBridge_Expose_handles_array_format_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send RPC with array params format (positional).
        var request = """{"jsonrpc":"2.0","id":"arr-1","method":"MultiParamExport.greet","params":["Bob",25,true]}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("arr-1")));

        // The handler should have executed successfully.
        Assert.Contains(scripts, s => s.Contains("arr-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_handles_single_param_shorthand()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send RPC for SyncMethod with a single string param shorthand (not object, not array).
        var request = """{"jsonrpc":"2.0","id":"sp-1","method":"MultiParamExport.syncMethod","params":"hello"}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("sp-1")));

        Assert.Contains(scripts, s => s.Contains("sp-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_handles_null_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send RPC for VoidMethod with null params.
        var request = """{"jsonrpc":"2.0","id":"np-1","method":"MultiParamExport.voidMethod","params":null}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("np-1")));

        Assert.Contains(scripts, s => s.Contains("np-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_handles_exact_name_fallback()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send params with PascalCase property names (exact match fallback).
        var request = """{"jsonrpc":"2.0","id":"ex-1","method":"MultiParamExport.greet","params":{"Name":"Charlie","Age":30}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ex-1")));

        Assert.Contains(scripts, s => s.Contains("ex-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_handles_missing_optional_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send only required params — the optional `formal` param should use default value.
        var request = """{"jsonrpc":"2.0","id":"opt-1","method":"MultiParamExport.greet","params":{"name":"Dana","age":28}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("opt-1")));

        Assert.Contains(scripts, s => s.Contains("opt-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_handles_array_params_less_than_method_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Send array with fewer elements than method parameters.
        var request = """{"jsonrpc":"2.0","id":"short-1","method":"MultiParamExport.greet","params":["Eve"]}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("short-1")));

        Assert.Contains(scripts, s => s.Contains("short-1"));
    }

    [Fact]
    public void RuntimeBridge_Expose_sync_method_returns_result()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // SyncMethod returns string (not Task) — CreateHandler should wrap it.
        var request = """{"jsonrpc":"2.0","id":"sync-m1","method":"MultiParamExport.syncMethod","params":{"input":"hello"}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("sync-m1")));

        // Should contain the result "HELLO".
        Assert.Contains(scripts, s => s.Contains("sync-m1") && s.Contains("HELLO"));
    }

    [Fact]
    public void RuntimeBridge_Dispose_removes_all_handlers()
    {
        var (core, _, _) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Get a proxy too
        core.Bridge.GetProxy<IAsyncImport>();

        core.Dispose();

        // After dispose, operations should throw.
        Assert.Throws<ObjectDisposedException>(() =>
            core.Bridge.Expose<IMultiParamExport>(impl));
        Assert.Throws<ObjectDisposedException>(() =>
            core.Bridge.GetProxy<IAsyncImport>());
    }

    [Fact]
    public void RuntimeBridge_Remove_cleans_reflection_handlers()
    {
        var (core, _, _) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();
        core.Bridge.Expose<IMultiParamExport>(impl);

        // Remove and re-expose should work without "already exposed" error.
        core.Bridge.Remove<IMultiParamExport>();
        core.Bridge.Expose<IMultiParamExport>(impl); // Should not throw.

        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_handler_unwraps_TargetInvocationException()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        core.Bridge.Expose<IReflectionThrowingExport>(new FakeReflectionThrowingExport());

        // Call the method that always throws synchronously (before returning a Task).
        var request = """{"jsonrpc":"2.0","id":"throw-1","method":"ReflectionThrowingExport.willThrow","params":null}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("throw-1")));

        // Error response should contain the unwrapped inner exception message, not TargetInvocationException.
        Assert.Contains(scripts, s => s.Contains("throw-1") && s.Contains("Deliberate test exception"));
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_DeserializeParameters_value_type_default()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionValueTypeExport();
        core.Bridge.Expose<IReflectionValueTypeExport>(impl);

        // Send only `a` param — `b` is a non-optional int, should get default(int) = 0 via Activator.CreateInstance.
        var request = """{"jsonrpc":"2.0","id":"vt-1","method":"ReflectionValueTypeExport.add","params":{"a":5}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("vt-1")));

        Assert.Equal((5, 0), impl.LastArgs);
        Assert.Contains(scripts, s => s.Contains("vt-1") && s.Contains("5"));
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_sourceGenerated_Expose_with_RateLimit()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeMultiParamExport();

        // IMultiParamExport is in UnitTests assembly which has Bridge.Generator → source-generated path.
        core.Bridge.Expose<IMultiParamExport>(impl, new BridgeOptions
        {
            RateLimit = new RateLimit(1, TimeSpan.FromSeconds(10))
        });

        // First call should succeed.
        var request1 = """{"jsonrpc":"2.0","id":"sgrl-1","method":"MultiParamExport.voidMethod","params":null}""";
        adapter.RaiseWebMessage(request1, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("sgrl-1")));

        Assert.Contains(scripts, s => s.Contains("sgrl-1"));

        // Second call should be rate limited.
        var request2 = """{"jsonrpc":"2.0","id":"sgrl-2","method":"MultiParamExport.voidMethod","params":null}""";
        adapter.RaiseWebMessage(request2, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("sgrl-2")));

        Assert.Contains(scripts, s => s.Contains("-32029"));
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_rate_limit_evicts_expired_entries()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();

        // Use a 2-second window to avoid flaky failures from Thread.Sleep imprecision on CI.
        core.Bridge.Expose<IReflectionExportService>(impl, new BridgeOptions
        {
            RateLimit = new RateLimit(1, TimeSpan.FromSeconds(2))
        });

        // First call succeeds.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ev-1","method":"ReflectionExportService.voidNoArgs","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        // Second call immediately after first — well within the 2-second window, must be rate limited.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ev-2","method":"ReflectionExportService.voidNoArgs","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ev-2")));
        Assert.Equal(1, impl.VoidCallCount);

        // Wait for the 2-second window to expire.
        var evictionDeadline = DateTime.UtcNow.AddSeconds(2.5);
        DispatcherTestPump.WaitUntil(_dispatcher, () => DateTime.UtcNow >= evictionDeadline, TimeSpan.FromSeconds(3));

        // Third call: should succeed after eviction of expired entry.
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"ev-3","method":"ReflectionExportService.voidNoArgs","params":null}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ev-3")));
        Assert.Equal(2, impl.VoidCallCount);

        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_registers_handlers()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();

        // This should go through ExposeViaReflection since the Testing assembly
        // does not have the Bridge.Generator running.
        core.Bridge.Expose<IReflectionExportService>(impl);

        // JS stub should have been injected.
        Assert.Contains(scripts, s => s.Contains("ReflectionExportService"));
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_custom_name_works()
    {
        var (core, _, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionCustomNameExport();

        core.Bridge.Expose<IReflectionCustomNameExport>(impl);

        // Custom service name should be used.
        Assert.Contains(scripts, s => s.Contains("reflectionCustomName"));
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_RPC_call_named_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Call Greet via RPC with named params.
        var request = """{"jsonrpc":"2.0","id":"ref-1","method":"ReflectionExportService.greet","params":{"name":"Alice"}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-1")));

        Assert.Equal("Alice", impl.LastGreetName);
        Assert.Contains(scripts, s => s.Contains("ref-1") && s.Contains("Hello, Alice!"));
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_RPC_call_array_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Call Greet via RPC with array params (positional).
        var request = """{"jsonrpc":"2.0","id":"ref-arr-1","method":"ReflectionExportService.greet","params":["Bob"]}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-arr-1")));

        Assert.Equal("Bob", impl.LastGreetName);
        Assert.Contains(scripts, s => s.Contains("ref-arr-1"));
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_void_no_args()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Call VoidNoArgs with null params.
        var request = """{"jsonrpc":"2.0","id":"ref-void-1","method":"ReflectionExportService.voidNoArgs","params":null}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-void-1")));

        Assert.Equal(1, impl.VoidCallCount);
        Assert.Contains(scripts, s => s.Contains("ref-void-1"));
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_multi_params()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Call SaveData with named params.
        var request = """{"jsonrpc":"2.0","id":"ref-sd-1","method":"ReflectionExportService.saveData","params":{"key":"myKey","value":"myValue"}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-sd-1")));

        Assert.Equal(("myKey", "myValue"), impl.LastSavedData);
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_missing_param_uses_default()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Send with only `key` — `value` param should get default (null for string).
        var request = """{"jsonrpc":"2.0","id":"ref-def-1","method":"ReflectionExportService.saveData","params":{"key":"onlyKey"}}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-def-1")));

        Assert.Equal("onlyKey", impl.LastSavedData?.Key);
        // value should be null (default for missing string param).
        Assert.Null(impl.LastSavedData?.Value);
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_handles_single_param_shorthand()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Single param shorthand (not object, not array).
        var request = """{"jsonrpc":"2.0","id":"ref-sp-1","method":"ReflectionExportService.greet","params":"Dave"}""";
        adapter.RaiseWebMessage(request, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("ref-sp-1")));

        Assert.Equal("Dave", impl.LastGreetName);
    }

    [Fact]
    public void RuntimeBridge_reflection_Remove_clears_handlers()
    {
        var (core, _, _) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        // Remove should clean up reflection-based handlers.
        core.Bridge.Remove<IReflectionExportService>();

        // Re-expose should work without "already exposed" error.
        core.Bridge.Expose<IReflectionExportService>(impl);
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_double_Expose_throws()
    {
        var (core, _, _) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();
        core.Bridge.Expose<IReflectionExportService>(impl);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            core.Bridge.Expose<IReflectionExportService>(impl));
        Assert.Contains("already been exposed", ex.Message);
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_GetProxy_creates_DispatchProxy()
    {
        var (core, _, _) = CreateCoreWithBridge();

        // IReflectionImportService is from Testing assembly — no generated proxy.
        var proxy = core.Bridge.GetProxy<IReflectionImportService>();

        Assert.NotNull(proxy);
        Assert.IsAssignableFrom<IReflectionImportService>(proxy);
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_GetProxy_cached()
    {
        var (core, _, _) = CreateCoreWithBridge();

        var proxy1 = core.Bridge.GetProxy<IReflectionImportService>();
        var proxy2 = core.Bridge.GetProxy<IReflectionImportService>();

        Assert.Same(proxy1, proxy2);
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_GetProxy_routes_calls()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();

        var proxy = core.Bridge.GetProxy<IReflectionImportService>();
        var task = proxy.NotifyAsync("test message");

        // Should have sent an RPC call via InvokeScriptAsync.
        Assert.Contains(scripts, s =>
            s.Contains("ReflectionImportService.notifyAsync"));
        core.Dispose();
    }

    [Fact]
    public void RuntimeBridge_reflection_Expose_with_RateLimit_wraps_handlers()
    {
        var (core, adapter, scripts) = CreateCoreWithBridge();
        var impl = new FakeReflectionExportService();

        // Expose with rate limiting (through reflection path).
        core.Bridge.Expose<IReflectionExportService>(impl, new BridgeOptions
        {
            RateLimit = new RateLimit(2, TimeSpan.FromSeconds(10))
        });

        // First two calls should succeed.
        for (int i = 0; i < 2; i++)
        {
            var request = $$"""{"jsonrpc":"2.0","id":"rl-{{i}}","method":"ReflectionExportService.voidNoArgs","params":null}""";
            adapter.RaiseWebMessage(request, "*", core.ChannelId);
        }
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Count(s => s.Contains("rl-0") || s.Contains("rl-1")) >= 2);

        Assert.Equal(2, impl.VoidCallCount);

        // Third call should be rate limited.
        var request3 = """{"jsonrpc":"2.0","id":"rl-2","method":"ReflectionExportService.voidNoArgs","params":null}""";
        adapter.RaiseWebMessage(request3, "*", core.ChannelId);
        _dispatcher.RunAll();
        DispatcherTestPump.WaitUntil(_dispatcher, () => scripts.Any(s => s.Contains("rl-2")));

        // VoidCallCount should still be 2 (third call was rejected).
        Assert.Equal(2, impl.VoidCallCount);
        // Error response with rate limit code should have been sent.
        Assert.Contains(scripts, s => s.Contains("-32029"));

        core.Dispose();
    }
}
