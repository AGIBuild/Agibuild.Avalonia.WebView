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

// ==================== JsImport interfaces for BridgeImportProxy tests ====================

/// <summary>
/// Interface with sync return methods to verify BridgeImportProxy rejects non-Task returns.
/// NOT decorated with [JsImport] to avoid source generator issues with sync signatures.
/// Used directly via DispatchProxy.Create.
/// </summary>
public interface ISyncImport
{
    void FireAndForget(string message);
    void Ping();
    string GetLabel();
}

/// <summary>Interface for Task and Task&lt;T&gt; import proxy coverage.</summary>
[JsImport]
public interface IAsyncImport
{
    Task SendAsync(string data, int retries);
    Task<string> FetchAsync(string key);
}

[JsImport]
public interface IAsyncNoArgsImport
{
    Task PingAsync();
}

/// <summary>Interface with multiple parameters to exercise parameter mapping.</summary>
[JsExport]
public interface IMultiParamExport
{
    Task<string> Greet(string name, int age, bool formal = false);
    Task VoidMethod();
    string SyncMethod(string input);
}

public class FakeMultiParamExport : IMultiParamExport
{
    public Task<string> Greet(string name, int age, bool formal = false)
        => Task.FromResult(formal ? $"Dear {name} ({age})" : $"Hi {name}");

    public Task VoidMethod() => Task.CompletedTask;

    public string SyncMethod(string input) => input.ToUpperInvariant();
}

// ==================== Tests ====================

/// <summary>
/// Supplementary tests to increase code coverage in the Runtime assembly:
/// - BridgeImportProxy (0% → 90%+)
/// - SpaHostingExtensions (0% → 90%+)
/// - RuntimeBridgeService DeserializeParameters edge cases (44.3% → 90%+)
/// - SpaHostingService dev proxy + edge cases (67.7% → 90%+)
/// - WebDialog remaining paths (87.5% → 95%+)
/// </summary>
public sealed class RuntimeCoverageTests
{
    private readonly TestDispatcher _dispatcher = new();

    // ========================= BridgeImportProxy — direct tests =========================

    [Fact]
    public async Task BridgeImportProxy_uninitialized_throws()
    {
        // Create a proxy without calling Initialize — Invoke should throw.
        var proxy = DispatchProxy.Create<IAsyncImport, BridgeImportProxy>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.SendAsync("test", 1));
        Assert.Contains("not been initialized", ex.Message);
    }

    [Fact]
    public void BridgeImportProxy_Task_return_calls_InvokeAsync()
    {
        var rpc = new RecordingRpcService();
        var proxy = CreateProxy<IAsyncImport>(rpc, "AsyncImport");

        // Task return should call rpc.InvokeAsync(methodName, params)
        var task = proxy.SendAsync("hello", 3);

        Assert.Single(rpc.Invocations);
        Assert.Equal("AsyncImport.sendAsync", rpc.Invocations[0].Method);
        var p = (Dictionary<string, object?>)rpc.Invocations[0].Args!;
        Assert.Equal("hello", p["data"]);
        Assert.Equal(3, p["retries"]);
    }

    [Fact]
    public async Task BridgeImportProxy_TaskT_return_calls_generic_InvokeAsync()
    {
        var rpc = new RecordingRpcService();
        rpc.NextResult = "fetchedValue";
        var proxy = CreateProxy<IAsyncImport>(rpc, "AsyncImport");

        var result = await proxy.FetchAsync("myKey");

        Assert.Equal("fetchedValue", result);
        Assert.Single(rpc.GenericInvocations);
        Assert.Equal("AsyncImport.fetchAsync", rpc.GenericInvocations[0].Method);
    }

    [Fact]
    public void BridgeImportProxy_sync_void_method_throws_not_supported()
    {
        var rpc = new RecordingRpcService();
        var proxy = CreateProxy<ISyncImport>(rpc, "SyncImport");

        var ex = Assert.Throws<NotSupportedException>(() => proxy.FireAndForget("msg"));
        Assert.Contains("must return Task or Task<T>", ex.Message);
        Assert.Empty(rpc.Invocations);
    }

    [Fact]
    public void BridgeImportProxy_sync_reference_return_throws_not_supported()
    {
        var rpc = new RecordingRpcService();
        var proxy = CreateProxy<ISyncImport>(rpc, "SyncImport");

        var ex = Assert.Throws<NotSupportedException>(() => proxy.GetLabel());
        Assert.Contains("must return Task or Task<T>", ex.Message);
        Assert.Empty(rpc.Invocations);
    }

    [Fact]
    public async Task BridgeImportProxy_no_args_sends_null_params()
    {
        var rpc = new RecordingRpcService();
        var proxy = CreateProxy<IAsyncNoArgsImport>(rpc, "AsyncNoArgsImport");

        // No-arg async import method should pass null params.
        await proxy.PingAsync();

        Assert.Null(rpc.Invocations[0].Args);
    }

    // ========================= SpaHostingExtensions =========================

    [Fact]
    public void AddEmbeddedFileProvider_null_options_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SpaHostingExtensions.AddEmbeddedFileProvider(null!, "app",
                typeof(RuntimeCoverageTests).Assembly, "wwwroot"));
    }

    [Fact]
    public void AddEmbeddedFileProvider_empty_scheme_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentException>(() =>
            opts.AddEmbeddedFileProvider("", typeof(RuntimeCoverageTests).Assembly, "wwwroot"));
    }

    [Fact]
    public void AddEmbeddedFileProvider_null_scheme_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentNullException>(() =>
            opts.AddEmbeddedFileProvider(null!, typeof(RuntimeCoverageTests).Assembly, "wwwroot"));
    }

    [Fact]
    public void AddEmbeddedFileProvider_null_assembly_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentNullException>(() =>
            opts.AddEmbeddedFileProvider("app", null!, "wwwroot"));
    }

    [Fact]
    public void AddEmbeddedFileProvider_empty_prefix_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentException>(() =>
            opts.AddEmbeddedFileProvider("app", typeof(RuntimeCoverageTests).Assembly, ""));
    }

    [Fact]
    public void AddEmbeddedFileProvider_null_prefix_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentNullException>(() =>
            opts.AddEmbeddedFileProvider("app", typeof(RuntimeCoverageTests).Assembly, null!));
    }

    [Fact]
    public void AddEmbeddedFileProvider_registers_custom_scheme()
    {
        var opts = new WebViewEnvironmentOptions();

        var result = opts.AddEmbeddedFileProvider("myscheme",
            typeof(RuntimeCoverageTests).Assembly, "wwwroot");

        Assert.Same(opts, result);
        Assert.Single(opts.CustomSchemes);
        Assert.Equal("myscheme", opts.CustomSchemes[0].SchemeName);
        Assert.True(opts.CustomSchemes[0].HasAuthorityComponent);
        Assert.True(opts.CustomSchemes[0].TreatAsSecure);
    }

    [Fact]
    public void AddEmbeddedFileProvider_does_not_duplicate_scheme()
    {
        var opts = new WebViewEnvironmentOptions();
        opts.AddEmbeddedFileProvider("app", typeof(RuntimeCoverageTests).Assembly, "wwwroot");
        opts.AddEmbeddedFileProvider("app", typeof(RuntimeCoverageTests).Assembly, "other");

        Assert.Single(opts.CustomSchemes);
    }

    [Fact]
    public void AddDevServerProxy_null_options_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SpaHostingExtensions.AddDevServerProxy(null!, "app", "http://localhost:5173"));
    }

    [Fact]
    public void AddDevServerProxy_empty_scheme_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentException>(() => opts.AddDevServerProxy("", "http://localhost:5173"));
    }

    [Fact]
    public void AddDevServerProxy_null_scheme_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentNullException>(() => opts.AddDevServerProxy(null!, "http://localhost:5173"));
    }

    [Fact]
    public void AddDevServerProxy_empty_url_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentException>(() => opts.AddDevServerProxy("app", ""));
    }

    [Fact]
    public void AddDevServerProxy_null_url_throws()
    {
        var opts = new WebViewEnvironmentOptions();
        Assert.Throws<ArgumentNullException>(() => opts.AddDevServerProxy("app", null!));
    }

    [Fact]
    public void AddDevServerProxy_registers_custom_scheme()
    {
        var opts = new WebViewEnvironmentOptions();

        var result = opts.AddDevServerProxy("devscheme", "http://localhost:3000");

        Assert.Same(opts, result);
        Assert.Single(opts.CustomSchemes);
        Assert.Equal("devscheme", opts.CustomSchemes[0].SchemeName);
    }

    [Fact]
    public void AddDevServerProxy_does_not_duplicate_scheme()
    {
        var opts = new WebViewEnvironmentOptions();
        opts.AddDevServerProxy("app", "http://localhost:3000");
        opts.AddDevServerProxy("app", "http://localhost:5173");

        Assert.Single(opts.CustomSchemes);
    }

    // ========================= SpaHostingService — constructor =========================

    [Fact]
    public void SpaHostingService_null_options_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SpaHostingService(null!, NullTestLogger.Instance));
    }

    [Fact]
    public void SpaHostingService_null_logger_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SpaHostingService(new SpaHostingOptions { DevServerUrl = "http://localhost:3000" }, null!));
    }

    [Fact]
    public void SpaHostingService_DevServerUrl_creates_proxy_mode()
    {
        // Should not throw — DevServerUrl is set, so embedded fields are not required.
        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = "http://localhost:5173/"
        }, NullTestLogger.Instance);

        Assert.NotNull(svc);
    }

    [Fact]
    public void SpaHostingService_no_DevServerUrl_no_embedded_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SpaHostingService(new SpaHostingOptions
            {
                DevServerUrl = null,
                EmbeddedResourcePrefix = null,
                ResourceAssembly = null,
            }, NullTestLogger.Instance));
    }

    [Fact]
    public void SpaHostingService_no_DevServerUrl_no_assembly_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SpaHostingService(new SpaHostingOptions
            {
                DevServerUrl = null,
                EmbeddedResourcePrefix = "wwwroot",
                ResourceAssembly = null,
            }, NullTestLogger.Instance));
    }

    // ========================= SpaHostingService — GetSchemeRegistration =========================

    [Fact]
    public void GetSchemeRegistration_returns_correct_scheme()
    {
        using var svc = CreateEmbeddedSpaService();

        var reg = svc.GetSchemeRegistration();

        Assert.Equal("app", reg.SchemeName);
        Assert.True(reg.HasAuthorityComponent);
        Assert.True(reg.TreatAsSecure);
    }

    // ========================= SpaHostingService — TryHandle edge cases =========================

    [Fact]
    public void TryHandle_returns_false_when_disposed()
    {
        var svc = CreateEmbeddedSpaService();
        svc.Dispose();

        var e = MakeSpaArgs("app://localhost/index.html");
        Assert.False(svc.TryHandle(e));
    }

    [Fact]
    public void TryHandle_returns_false_when_already_handled()
    {
        using var svc = CreateEmbeddedSpaService();
        var e = MakeSpaArgs("app://localhost/index.html");
        e.Handled = true;

        Assert.False(svc.TryHandle(e));
    }

    [Fact]
    public void TryHandle_returns_false_when_uri_is_null()
    {
        using var svc = CreateEmbeddedSpaService();
        var e = new WebResourceRequestedEventArgs { RequestUri = null, Method = "GET" };

        Assert.False(svc.TryHandle(e));
    }

    [Fact]
    public void TryHandle_returns_false_for_non_matching_scheme()
    {
        using var svc = CreateEmbeddedSpaService();
        var e = MakeSpaArgs("https://example.com/page");

        Assert.False(svc.TryHandle(e));
    }

    // ========================= SpaHostingService — DefaultHeaders =========================

    [Fact]
    public void TryHandle_applies_default_headers()
    {
        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
            DefaultHeaders = new Dictionary<string, string>
            {
                ["X-Custom"] = "TestValue",
                ["X-Frame-Options"] = "DENY"
            }
        }, NullTestLogger.Instance);

        var e = MakeSpaArgs("app://localhost/test.txt");
        svc.TryHandle(e);

        Assert.NotNull(e.ResponseHeaders);
        Assert.Equal("TestValue", e.ResponseHeaders!["X-Custom"]);
        Assert.Equal("DENY", e.ResponseHeaders["X-Frame-Options"]);
    }

    // ========================= SpaHostingService — hashed filename immutable cache =========================

    [Fact]
    public void TryHandle_hashed_filename_gets_immutable_cache()
    {
        // We need an actual embedded resource with a hashed name to test this fully.
        // Instead test that non-hashed gets no-cache (already tested) and hashed
        // via the static helper.
        Assert.True(SpaHostingService.IsHashedFilename("app.a1b2c3d4.js"));
        Assert.True(SpaHostingService.IsHashedFilename("chunk-ABCDEF1234.css"));
        Assert.False(SpaHostingService.IsHashedFilename("app.js"));
        Assert.False(SpaHostingService.IsHashedFilename(""));
    }

    [Fact]
    public void IsHashedFilename_null_path_returns_false()
    {
        // GetFileNameWithoutExtension(null) returns null.
        Assert.False(SpaHostingService.IsHashedFilename(null!));
    }

    // ========================= SpaHostingService — dev proxy error path =========================

    [Fact]
    public void TryHandle_DevProxy_unreachable_returns_502()
    {
        // Point to a non-listening port — the HTTP call will fail with connection refused.
        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = "http://127.0.0.1:1"  // Port 1 is almost certainly not listening.
        }, NullTestLogger.Instance);

        var e = MakeSpaArgs("app://localhost/index.html");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.True(e.Handled);
        Assert.Equal(502, e.ResponseStatusCode);
        Assert.Equal("text/plain", e.ResponseContentType);
    }

    // ========================= SpaHostingService — Dispose =========================

    [Fact]
    public void Dispose_idempotent()
    {
        var svc = CreateEmbeddedSpaService();
        svc.Dispose();
        svc.Dispose(); // Second call should not throw.
    }

    [Fact]
    public void Dispose_with_dev_proxy_disposes_httpClient()
    {
        var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = "http://localhost:12345"
        }, NullTestLogger.Instance);

        svc.Dispose();
        // After dispose, TryHandle should return false.
        var e = MakeSpaArgs("app://localhost/index.html");
        Assert.False(svc.TryHandle(e));
    }

    // ========================= SpaHostingService — MIME type edge cases =========================

    [Theory]
    [InlineData(".htm", "text/html")]
    [InlineData(".mjs", "application/javascript")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".ico", "image/x-icon")]
    [InlineData(".woff", "font/woff")]
    [InlineData(".ttf", "font/ttf")]
    [InlineData(".eot", "application/vnd.ms-fontobject")]
    [InlineData(".otf", "font/otf")]
    [InlineData(".map", "application/json")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".avif", "image/avif")]
    [InlineData(".mp4", "video/mp4")]
    [InlineData(".webm", "video/webm")]
    [InlineData(".xml", "application/xml")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(null, "application/octet-stream")]
    public void GetMimeType_covers_all_entries(string? ext, string expected)
    {
        Assert.Equal(expected, SpaHostingService.GetMimeType(ext!));
    }

    // ========================= SpaHostingService — embedded fallback for deep link =========================

    [Fact]
    public void TryHandle_embedded_resource_not_found_tries_fallback()
    {
        using var svc = CreateEmbeddedSpaService();
        // Request a file that doesn't exist — should try fallback to index.html.
        var e = MakeSpaArgs("app://localhost/assets/missing.js");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        // index.html may or may not exist in test assembly — either 200 or 404 is acceptable.
        Assert.True(e.ResponseStatusCode == 200 || e.ResponseStatusCode == 404);
    }

    // ========================= RuntimeBridgeService — DeserializeParameters edge cases =========================

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

    // ========================= RuntimeBridgeService — sync method handler =========================

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

    // ========================= RuntimeBridgeService — ValidateJsExportAttribute non-interface =========================

    [Fact]
    public void Expose_with_non_interface_throws()
    {
        var (core, _, _) = CreateCoreWithBridge();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            core.Bridge.Expose<FakeMultiParamExport>(new FakeMultiParamExport()));
        Assert.Contains("must be an interface", ex.Message);
    }

    [Fact]
    public void GetProxy_with_non_interface_throws()
    {
        var (core, _, _) = CreateCoreWithBridge();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            core.Bridge.GetProxy<FakeMultiParamExport>());
        Assert.Contains("must be an interface", ex.Message);
    }

    // ========================= RuntimeBridgeService — Dispose clears all =========================

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

    // ========================= RuntimeBridgeService — Remove removes reflection handler =========================

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

    // ========================= WebDialog — OpenDevTools / CloseDevTools / IsDevToolsOpen =========================

    [Fact]
    public async Task WebDialog_OpenDevTools_delegates_to_core()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        // OpenDevTools on base adapter is a no-op but covers the delegation path.
        await dialog.OpenDevToolsAsync();
        Assert.False(await dialog.IsDevToolsOpenAsync());

        await dialog.CloseDevToolsAsync();
        Assert.False(await dialog.IsDevToolsOpenAsync());
    }

    // ========================= WebDialog — ZoomFactorChanged event delegation =========================

    [Fact]
    public async Task WebDialog_SetZoomFactorAsync_delegates_to_core()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithZoom();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        await dialog.SetZoomFactorAsync(2.0);
        Assert.Equal(2.0, await dialog.GetZoomFactorAsync());

        await dialog.SetZoomFactorAsync(3.0);
        Assert.Equal(3.0, await dialog.GetZoomFactorAsync());
    }

    // ========================= WebDialog — ContextMenuRequested event unsubscribe =========================

    [Fact]
    public void WebDialog_ContextMenuRequested_unsubscribe()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.CreateWithContextMenu();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        ContextMenuRequestedEventArgs? received = null;
        EventHandler<ContextMenuRequestedEventArgs> handler = (_, e) => received = e;

        dialog.ContextMenuRequested += handler;
        dialog.ContextMenuRequested -= handler;

        ((MockWebViewAdapterWithContextMenu)adapter).RaiseContextMenu(
            new ContextMenuRequestedEventArgs { X = 1, Y = 2 });

        Assert.Null(received);
    }

    // ========================= WebDialog — AdapterCreated event =========================

    [Fact]
    public void WebDialog_AdapterCreated_event_subscribe_unsubscribe()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        bool raised = false;
        EventHandler<AdapterCreatedEventArgs> handler = (_, _) => raised = true;

        dialog.AdapterCreated += handler;
        dialog.AdapterCreated -= handler;

        // No way to raise adapter created externally — just covers the accessor.
        Assert.False(raised);
    }

    // ========================= WebDialog — Bridge delegation =========================

    [Fact]
    public void WebDialog_Bridge_returns_core_bridge()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        // Bridge is non-null even without explicit enablement (Core always has it).
        Assert.NotNull(dialog.Bridge);
    }

    // ========================= WebDialog — double dispose =========================

    [Fact]
    public void WebDialog_double_dispose_safe()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        var dialog = new WebDialog(host, adapter, _dispatcher);

        dialog.Dispose();
        dialog.Dispose(); // No exception.

        Assert.Equal(1, host.CloseCallCount);
    }

    // ========================= SpaHostingService — dev proxy non-success fallback path =========================

    [Fact]
    public void TryHandle_DevProxy_non_success_attempts_fallback()
    {
        // Create a dev proxy pointing to a non-listening address
        // to exercise both the initial request failure and fallback paths.
        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = "http://127.0.0.1:1"
        }, NullTestLogger.Instance);

        // Request a file with extension — will try direct, then fallback.
        var e = MakeSpaArgs("app://localhost/missing.js");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(502, e.ResponseStatusCode);
    }

    // ========================= SpaHostingService — embedded 404 path =========================

    [Fact]
    public void TryHandle_embedded_totally_missing_returns_404()
    {
        using var svc = CreateEmbeddedSpaService();

        // Request a file that doesn't exist and whose fallback also doesn't exist.
        var e = MakeSpaArgs("app://localhost/totally_missing_file.xyz");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        // The resource and fallback are both missing → 404.
        Assert.Equal(404, e.ResponseStatusCode);
        Assert.Equal("text/plain", e.ResponseContentType);
    }

    // ========================= RuntimeBridgeService — TargetInvocationException unwrap =========================

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

    // ========================= RuntimeBridgeService — DeserializeParameters value-type default =========================

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

    // ========================= RuntimeBridgeService — Source-generated path + RateLimit =========================

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

    // ========================= RuntimeBridgeService — Rate limit window eviction =========================

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

    // ========================= SpaHostingService — hashed filename embedded resource cache =========================

    [Fact]
    public void TryHandle_embedded_hashed_filename_serves_with_immutable_cache()
    {
        using var svc = CreateEmbeddedSpaService();

        // app.a1b2c3d4.js is an actual embedded resource with a hash-style name.
        var e = MakeSpaArgs("app://localhost/app.a1b2c3d4.js");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(200, e.ResponseStatusCode);
        Assert.Equal("application/javascript", e.ResponseContentType);
        Assert.NotNull(e.ResponseHeaders);
        Assert.Contains("immutable", e.ResponseHeaders!["Cache-Control"]);
    }

    // ========================= SpaHostingService — Dev proxy success path =========================

    [Fact]
    public void TryHandle_DevProxy_success_copies_response_body()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var ct = TestContext.Current.CancellationToken;
        _ = Task.Run(() =>
        {
            try
            {
                while (listener.IsListening && !ct.IsCancellationRequested)
                {
                    var ctx = listener.GetContext();
                    var body = Encoding.UTF8.GetBytes("<html>Dev Server OK</html>");
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/html";
                    ctx.Response.ContentLength64 = body.Length;
                    ctx.Response.OutputStream.Write(body);
                    ctx.Response.Close();
                }
            }
            catch { /* listener stopped */ }
        }, ct);

        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = $"http://127.0.0.1:{port}"
        }, NullTestLogger.Instance);

        var e = MakeSpaArgs("app://localhost/index.html");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(200, e.ResponseStatusCode);
        Assert.Equal("text/html", e.ResponseContentType);
        Assert.NotNull(e.ResponseBody);

        using var reader = new StreamReader(e.ResponseBody!);
        Assert.Contains("Dev Server OK", reader.ReadToEnd());

        listener.Stop();
    }

    // ========================= SpaHostingService — Dev proxy fallback success =========================

    [Fact]
    public void TryHandle_DevProxy_fallback_success_copies_fallback_response()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var ct = TestContext.Current.CancellationToken;
        _ = Task.Run(() =>
        {
            try
            {
                while (listener.IsListening && !ct.IsCancellationRequested)
                {
                    var ctx = listener.GetContext();
                    var path = ctx.Request.Url!.AbsolutePath;

                    if (path == "/index.html")
                    {
                        var body = Encoding.UTF8.GetBytes("<html>Fallback Index</html>");
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = "text/html";
                        ctx.Response.ContentLength64 = body.Length;
                        ctx.Response.OutputStream.Write(body);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                    }
                    ctx.Response.Close();
                }
            }
            catch { /* listener stopped */ }
        }, ct);

        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = $"http://127.0.0.1:{port}"
        }, NullTestLogger.Instance);

        // Request a file that doesn't exist → 404 from dev server → fallback to index.html → 200.
        var e = MakeSpaArgs("app://localhost/assets/missing.js");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(200, e.ResponseStatusCode);
        Assert.Equal("text/html", e.ResponseContentType);

        using var reader = new StreamReader(e.ResponseBody!);
        Assert.Contains("Fallback Index", reader.ReadToEnd());

        listener.Stop();
    }

    // ========================= SpaHostingService — Dev proxy non-success + fallback also fails =========================

    [Fact]
    public void TryHandle_DevProxy_non_success_and_fallback_fails_returns_error()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var ct = TestContext.Current.CancellationToken;
        _ = Task.Run(() =>
        {
            try
            {
                while (listener.IsListening && !ct.IsCancellationRequested)
                {
                    var ctx = listener.GetContext();
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
            }
            catch { /* listener stopped */ }
        }, ct);

        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = $"http://127.0.0.1:{port}"
        }, NullTestLogger.Instance);

        var e = MakeSpaArgs("app://localhost/missing.js");
        var handled = svc.TryHandle(e);

        Assert.True(handled);
        Assert.Equal(500, e.ResponseStatusCode);
        Assert.Equal("text/plain", e.ResponseContentType);

        listener.Stop();
    }

    // ========================= SpaHostingService — Dev proxy + DefaultHeaders =========================

    [Fact]
    public void TryHandle_DevProxy_success_applies_default_headers()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var ct = TestContext.Current.CancellationToken;
        _ = Task.Run(() =>
        {
            try
            {
                while (listener.IsListening && !ct.IsCancellationRequested)
                {
                    var ctx = listener.GetContext();
                    var body = Encoding.UTF8.GetBytes("OK");
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/plain";
                    ctx.Response.ContentLength64 = body.Length;
                    ctx.Response.OutputStream.Write(body);
                    ctx.Response.Close();
                }
            }
            catch { /* listener stopped */ }
        }, ct);

        using var svc = new SpaHostingService(new SpaHostingOptions
        {
            DevServerUrl = $"http://127.0.0.1:{port}",
            DefaultHeaders = new Dictionary<string, string>
            {
                ["X-Test-Header"] = "ProxyTest"
            }
        }, NullTestLogger.Instance);

        var e = MakeSpaArgs("app://localhost/index.html");
        svc.TryHandle(e);

        Assert.NotNull(e.ResponseHeaders);
        Assert.Equal("ProxyTest", e.ResponseHeaders!["X-Test-Header"]);

        listener.Stop();
    }

    // ========================= Helpers =========================

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static T CreateProxy<T>(IWebViewRpcService rpc, string serviceName) where T : class
    {
        var proxy = DispatchProxy.Create<T, BridgeImportProxy>();
        var bridgeProxy = (BridgeImportProxy)(object)proxy;
        bridgeProxy.Initialize(rpc, serviceName);
        return proxy;
    }

    private (WebViewCore Core, MockWebViewAdapter Adapter, List<string> Scripts) CreateCoreWithBridge()
    {
        var adapter = MockWebViewAdapter.Create();
        var scripts = new List<string>();
        adapter.ScriptCallback = script => { scripts.Add(script); return null; };
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });
        return (core, adapter, scripts);
    }

    private static SpaHostingService CreateEmbeddedSpaService()
    {
        return new SpaHostingService(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        }, NullTestLogger.Instance);
    }

    private static WebResourceRequestedEventArgs MakeSpaArgs(string uri)
    {
        return new WebResourceRequestedEventArgs(new Uri(uri), "GET");
    }

    // ========================= RuntimeBridgeService — reflection-based Expose =========================
    // These tests use IReflectionExportService from the Testing assembly, which has NO
    // source-generated registration, forcing the reflection fallback in RuntimeBridgeService.

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

    // ========================= RuntimeBridgeService — reflection-based GetProxy =========================

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

    // ========================= RuntimeBridgeService — Expose with rate limit via reflection =========================

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

    // ========================= WebViewCore — DevTools with IDevToolsAdapter mock =========================

    [Fact]
    public async Task WebViewCore_OpenDevTools_delegates_to_IDevToolsAdapter()
    {
        var adapter = new MockDevToolsAdapter();
        using var core = new WebViewCore(adapter, _dispatcher);

        await core.OpenDevToolsAsync();
        Assert.True(adapter.DevToolsOpened);
        Assert.True(await core.IsDevToolsOpenAsync());

        await core.CloseDevToolsAsync();
        Assert.False(await core.IsDevToolsOpenAsync());
        Assert.True(adapter.DevToolsClosed);
    }

    [Fact]
    public async Task WebViewCore_DevTools_open_close_are_idempotent()
    {
        var adapter = new MockDevToolsAdapter();
        using var core = new WebViewCore(adapter, _dispatcher);

        await core.OpenDevToolsAsync();
        await core.OpenDevToolsAsync();
        Assert.True(await core.IsDevToolsOpenAsync());

        await core.CloseDevToolsAsync();
        await core.CloseDevToolsAsync();
        Assert.False(await core.IsDevToolsOpenAsync());
    }

    // ========================= WebViewCore — SPA WebResourceRequested integration =========================

    [Fact]
    public void WebViewCore_SPA_handles_WebResourceRequested()
    {
        var adapter = MockWebViewAdapter.Create();
        using var core = new WebViewCore(adapter, _dispatcher);

        core.EnableSpaHosting(new SpaHostingOptions
        {
            EmbeddedResourcePrefix = "TestResources",
            ResourceAssembly = typeof(SpaHostingTests).Assembly,
        });

        // Trigger a WebResourceRequested for the app:// scheme.
        var e = new WebResourceRequestedEventArgs(new Uri("app://localhost/test.txt"), "GET");
        adapter.RaiseWebResourceRequested(e);

        // SPA hosting service should have handled it.
        Assert.True(e.Handled);
        Assert.Equal(200, e.ResponseStatusCode);
    }

    // ==================== Mock RPC service for BridgeImportProxy tests ====================

    /// <summary>Records all RPC invocations for assertions.</summary>
    private sealed class RecordingRpcService : IWebViewRpcService
    {
        public List<(string Method, object? Args)> Invocations { get; } = [];
        public List<(string Method, object? Args)> GenericInvocations { get; } = [];
        public object? NextResult { get; set; }

        private readonly Dictionary<string, Func<JsonElement?, Task<object?>>> _handlers = new();

        public Task<JsonElement> InvokeAsync(string method, object? args = null)
        {
            Invocations.Add((method, args));
            return Task.FromResult(default(JsonElement));
        }

        public Task<T?> InvokeAsync<T>(string method, object? args = null)
        {
            GenericInvocations.Add((method, args));
            if (NextResult is T typed)
                return Task.FromResult<T?>(typed);
            return Task.FromResult<T?>(default);
        }

        public void Handle(string method, Func<JsonElement?, Task<object?>> handler)
        {
            _handlers[method] = handler;
        }

        public void Handle(string method, Func<JsonElement?, object?> handler)
        {
            _handlers[method] = args => Task.FromResult(handler(args));
        }

        public void RemoveHandler(string method)
        {
            _handlers.Remove(method);
        }
    }

    // ==================== Mock DevTools adapter ====================

    #pragma warning disable CS0067 // Events are required by interface but not used in test mock
    private sealed class MockDevToolsAdapter : IWebViewAdapter, IDevToolsAdapter
    {
        public bool DevToolsOpened { get; private set; }
        public bool DevToolsClosed { get; private set; }
        public bool IsDevToolsOpen { get; private set; }

        public void OpenDevTools() { DevToolsOpened = true; IsDevToolsOpen = true; }
        public void CloseDevTools() { DevToolsClosed = true; IsDevToolsOpen = false; }

        // IWebViewAdapter minimal implementation
        public event EventHandler<NavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<NewWindowRequestedEventArgs>? NewWindowRequested;
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
        public event EventHandler<EnvironmentRequestedEventArgs>? EnvironmentRequested;

        public void Initialize(IWebViewAdapterHost host) { }
        public void Attach(INativeHandle parentHandle) { }
        public void Detach() { }
        public Task NavigateAsync(Guid navigationId, Uri uri) => Task.CompletedTask;
        public Task NavigateToStringAsync(Guid navigationId, string html) => Task.CompletedTask;
        public Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl) => Task.CompletedTask;
        public Task<string?> InvokeScriptAsync(string script) => Task.FromResult<string?>(null);
        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public bool GoBack(Guid navigationId) => false;
        public bool GoForward(Guid navigationId) => false;
        public bool Refresh(Guid navigationId) => false;
        public bool Stop() => false;
    }
    #pragma warning restore CS0067
}
