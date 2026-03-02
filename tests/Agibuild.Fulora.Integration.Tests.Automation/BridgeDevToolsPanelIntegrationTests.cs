using Agibuild.Fulora;
using Agibuild.Fulora.Testing;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

public sealed class BridgeDevToolsPanelIntegrationTests
{
    private readonly TestDispatcher _dispatcher = new();

    private (WebViewCore Core, MockWebViewAdapter Adapter) CreateCoreWithTracer(out BridgeDevToolsService devTools)
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);

        devTools = new BridgeDevToolsService();
        core.BridgeTracer = devTools.Tracer;

        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });

        return (core, adapter);
    }

    [JsExport(Name = "DevToolsTestService")]
    public interface IDevToolsTestService
    {
        Task<string> Echo(string input);
    }

    private sealed class DevToolsTestServiceImpl : IDevToolsTestService
    {
        public Task<string> Echo(string input) => Task.FromResult($"echo:{input}");
    }

    [AvaloniaFact]
    public void Expose_service_with_tracer_produces_ServiceExposed_event()
    {
        var (core, _) = CreateCoreWithTracer(out var devTools);
        using (devTools)
        {
            core.Bridge.Expose<IDevToolsTestService>(new DevToolsTestServiceImpl());

            var events = devTools.Collector.GetEvents();
            Assert.Contains(events, e =>
                e.Phase == BridgeCallPhase.ServiceExposed &&
                e.ServiceName == "DevToolsTestService");

            core.Dispose();
        }
    }

    [AvaloniaFact]
    public void Remove_service_with_tracer_produces_ServiceRemoved_event()
    {
        var (core, _) = CreateCoreWithTracer(out var devTools);
        using (devTools)
        {
            core.Bridge.Expose<IDevToolsTestService>(new DevToolsTestServiceImpl());
            core.Bridge.Remove<IDevToolsTestService>();

            var events = devTools.Collector.GetEvents();
            Assert.Contains(events, e =>
                e.Phase == BridgeCallPhase.ServiceRemoved &&
                e.ServiceName == "DevToolsTestService");

            core.Dispose();
        }
    }

    [AvaloniaFact]
    public void Tracer_set_after_bridge_access_is_ignored()
    {
        var adapter = MockWebViewAdapter.Create();
        var core = new WebViewCore(adapter, _dispatcher);
        core.EnableWebMessageBridge(new WebMessageBridgeOptions
        {
            AllowedOrigins = new HashSet<string> { "*" }
        });

        _ = core.Bridge;
        using var devTools = new BridgeDevToolsService();
        core.BridgeTracer = devTools.Tracer;

        core.Bridge.Expose<IDevToolsTestService>(new DevToolsTestServiceImpl());
        Assert.Empty(devTools.Collector.GetEvents());

        core.Dispose();
    }

    [AvaloniaFact]
    public void StartPushing_sends_existing_events_via_invokeScript()
    {
        var scripts = new List<string>();
        using var devTools = new BridgeDevToolsService();

        devTools.Tracer.OnExportCallStart("Svc", "M", "{}");
        devTools.Tracer.OnExportCallEnd("Svc", "M", 10, "string");

        devTools.StartPushing(script =>
        {
            scripts.Add(script);
            return Task.FromResult<string?>(null);
        });

        Assert.Single(scripts);
        Assert.Contains("__bridgeDevToolsLoadEvents", scripts[0]);
    }

    [AvaloniaFact]
    public void StopPushing_stops_sending_events()
    {
        var scripts = new List<string>();
        using var devTools = new BridgeDevToolsService();

        devTools.StartPushing(script =>
        {
            scripts.Add(script);
            return Task.FromResult<string?>(null);
        });

        scripts.Clear();
        devTools.StopPushing();
        devTools.Tracer.OnExportCallStart("Svc", "M", "{}");

        Assert.Empty(scripts);
    }

    [AvaloniaFact]
    public void GetOverlayHtml_contains_required_hooks()
    {
        var html = BridgeDevToolsService.GetOverlayHtml();
        Assert.Contains("__bridgeDevToolsAddEvent", html);
        Assert.Contains("__bridgeDevToolsLoadEvents", html);
    }
}
