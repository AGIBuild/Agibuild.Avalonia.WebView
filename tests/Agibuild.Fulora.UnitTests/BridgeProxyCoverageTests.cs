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

    [Fact]
    public void IsHashedFilename_null_path_returns_false()
    {
        // GetFileNameWithoutExtension(null) returns null.
        Assert.False(SpaHostingService.IsHashedFilename(null!));
    }

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

    [Fact]
    public void WebDialog_Bridge_returns_core_bridge()
    {
        var host = new MockDialogHost();
        var adapter = MockWebViewAdapter.Create();
        using var dialog = new WebDialog(host, adapter, _dispatcher);

        // Bridge is non-null even without explicit enablement (Core always has it).
        Assert.NotNull(dialog.Bridge);
    }

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
}
