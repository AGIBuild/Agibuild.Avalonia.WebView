using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Agibuild.Avalonia.WebView.Benchmarks;

/// <summary>
/// Measures Bridge RPC round-trip latency for typed service calls.
/// Uses a mock adapter so no real browser is involved — we measure the C# dispatch overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BridgeBenchmarks
{
    private WebViewCore _core = null!;
    private Testing.MockWebViewAdapter _adapter = null!;
    private Testing.TestDispatcher _dispatcher = null!;

    [JsExport]
    public interface ICalcService
    {
        Task<int> Add(int a, int b);
    }

    private sealed class CalcServiceImpl : ICalcService
    {
        public Task<int> Add(int a, int b) => Task.FromResult(a + b);
    }

    [GlobalSetup]
    public void Setup()
    {
        _dispatcher = new Testing.TestDispatcher();
        _adapter = new Testing.MockWebViewAdapter();
        _core = new WebViewCore(_adapter, _dispatcher);

        // Enable bridge
        _core.EnableWebMessageBridge(new WebMessageBridgeOptions());
        _dispatcher.RunAll();

        // Expose service
        _core.Bridge.Expose<ICalcService>(new CalcServiceImpl());
        _dispatcher.RunAll();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _core.Dispose();
    }

    /// <summary>
    /// Simulates a JS→C# RPC call by sending a raw WebMessage and measuring dispatch time.
    /// </summary>
    [Benchmark(Description = "Bridge: JS→C# typed call (Add)")]
    public async Task BridgeTypedCall()
    {
        var request = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "CalcService.Add",
            @params = new { a = 3, b = 4 }
        });

        _adapter.RaiseWebMessage(request, "app://localhost", Guid.Empty);
        _dispatcher.RunAll();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Measures the overhead of Expose + Remove cycle.
    /// </summary>
    [Benchmark(Description = "Bridge: Expose + Remove cycle")]
    public void BridgeExposeRemoveCycle()
    {
        _core.Bridge.Expose<ICalcService>(new CalcServiceImpl());
        _dispatcher.RunAll();
        _core.Bridge.Remove<ICalcService>();
    }
}

/// <summary>
/// Measures raw RPC (non-typed) overhead for comparison.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class RpcBenchmarks
{
    private WebViewCore _core = null!;
    private Testing.MockWebViewAdapter _adapter = null!;
    private Testing.TestDispatcher _dispatcher = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dispatcher = new Testing.TestDispatcher();
        _adapter = new Testing.MockWebViewAdapter();
        _core = new WebViewCore(_adapter, _dispatcher);

        _core.EnableWebMessageBridge(new WebMessageBridgeOptions());
        _dispatcher.RunAll();

        // Register raw RPC handler
        _core.Rpc!.Handle("echo", (JsonElement? args) =>
        {
            return Task.FromResult<object?>(args);
        });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _core.Dispose();
    }

    [Benchmark(Description = "Raw RPC: JS→C# echo")]
    public void RawRpcEcho()
    {
        var request = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "echo",
            @params = new { message = "hello" }
        });

        _adapter.RaiseWebMessage(request, "app://localhost", Guid.Empty);
        _dispatcher.RunAll();
    }
}
