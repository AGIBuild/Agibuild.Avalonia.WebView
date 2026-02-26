using System.Text.Json;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Tests that the Source Generator correctly produces BridgeRegistration and BridgeProxy types,
/// and that RuntimeBridgeService discovers and uses them instead of reflection.
/// </summary>
public sealed class BridgeGeneratorTests
{
    private readonly TestDispatcher _dispatcher = new();

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

    // ==================== Assembly attribute discovery ====================

    [Fact]
    public void Assembly_has_BridgeRegistration_attribute_for_JsExport_interfaces()
    {
        var assembly = typeof(IAppService).Assembly;
        var attrs = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false);

        // IAppService and ICustomNameService should have generated registrations
        Assert.True(attrs.Length >= 1, $"Expected at least 1 BridgeRegistrationAttribute, found {attrs.Length}");

        var appServiceAttr = attrs.Cast<BridgeRegistrationAttribute>()
            .FirstOrDefault(a => a.InterfaceType == typeof(IAppService));
        Assert.NotNull(appServiceAttr);
        Assert.NotNull(appServiceAttr!.RegistrationType);
    }

    [Fact]
    public void Assembly_has_BridgeProxy_attribute_for_JsImport_interfaces()
    {
        var assembly = typeof(IUiController).Assembly;
        var attrs = assembly.GetCustomAttributes(typeof(BridgeProxyAttribute), false);

        Assert.True(attrs.Length >= 1, $"Expected at least 1 BridgeProxyAttribute, found {attrs.Length}");

        var uiCtrlAttr = attrs.Cast<BridgeProxyAttribute>()
            .FirstOrDefault(a => a.InterfaceType == typeof(IUiController));
        Assert.NotNull(uiCtrlAttr);
        Assert.NotNull(uiCtrlAttr!.ProxyType);
    }

    // ==================== Generated Registration ====================

    [Fact]
    public void Generated_registration_has_correct_service_name()
    {
        var assembly = typeof(IAppService).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>()
            .First(a => a.InterfaceType == typeof(IAppService));

        var registration = (IBridgeServiceRegistration<IAppService>)Activator.CreateInstance(attr.RegistrationType)!;
        Assert.Equal("AppService", registration.ServiceName);
    }

    [Fact]
    public void Generated_registration_has_correct_method_names()
    {
        var assembly = typeof(IAppService).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>()
            .First(a => a.InterfaceType == typeof(IAppService));

        var registration = (IBridgeServiceRegistration<IAppService>)Activator.CreateInstance(attr.RegistrationType)!;
        var methods = registration.MethodNames;

        Assert.Contains("AppService.getCurrentUser", methods);
        Assert.Contains("AppService.saveSettings", methods);
        Assert.Contains("AppService.searchItems", methods);
    }

    [Fact]
    public void Generated_registration_produces_JS_stub()
    {
        var assembly = typeof(IAppService).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>()
            .First(a => a.InterfaceType == typeof(IAppService));

        var registration = (IBridgeServiceRegistration<IAppService>)Activator.CreateInstance(attr.RegistrationType)!;
        var stub = registration.GetJsStub();

        Assert.Contains("agWebView.bridge.AppService", stub);
        Assert.Contains("getCurrentUser", stub);
        Assert.Contains("saveSettings", stub);
    }

    [Fact]
    public void Generated_custom_name_registration_uses_custom_name()
    {
        var assembly = typeof(ICustomNameService).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeRegistrationAttribute), false)
            .Cast<BridgeRegistrationAttribute>()
            .FirstOrDefault(a => a.InterfaceType == typeof(ICustomNameService));

        Assert.NotNull(attr);
        var registration = (IBridgeServiceRegistration<ICustomNameService>)Activator.CreateInstance(attr!.RegistrationType)!;
        Assert.Equal("api", registration.ServiceName);
        Assert.Contains("api.ping", registration.MethodNames);
    }

    // ==================== Generated Proxy ====================

    [Fact]
    public void Generated_proxy_implements_interface()
    {
        var assembly = typeof(IUiController).Assembly;
        var attr = assembly.GetCustomAttributes(typeof(BridgeProxyAttribute), false)
            .Cast<BridgeProxyAttribute>()
            .First(a => a.InterfaceType == typeof(IUiController));

        Assert.True(typeof(IUiController).IsAssignableFrom(attr.ProxyType));
    }

    // ==================== End-to-end: Expose via generated code ====================

    [Fact]
    public void Expose_uses_generated_registration_when_available()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        // IAppService has [JsExport] and the SG should have generated a registration.
        core.Bridge.Expose<IAppService>(new FakeAppService());

        // Verify JS stub was injected.
        var jsStub = capturedScripts.Last();
        Assert.Contains("agWebView.bridge.AppService", jsStub);

        // Verify the service is callable via RPC.
        capturedScripts.Clear();
        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"gen-1","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.True(capturedScripts.Count > 0);
        var response = capturedScripts.Last();
        Assert.Contains("Alice", response);
    }

    [Fact]
    public void Expose_generated_handles_named_params()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var svc = new FakeAppService();
        core.Bridge.Expose<IAppService>(svc);
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"gen-2","method":"AppService.saveSettings","params":{"settings":{"theme":"blue","darkMode":false}}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.NotNull(svc.LastSavedSettings);
        Assert.Equal("blue", svc.LastSavedSettings!.Theme);
        Assert.False(svc.LastSavedSettings.DarkMode);
    }

    [Fact]
    public void Remove_generated_unregisters_handlers()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"gen-rm","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        var response = capturedScripts.Last();
        Assert.Contains("-32601", response); // Method not found.
    }

    [Fact]
    public void Re_expose_after_remove_works_with_generated()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        core.Bridge.Expose<IAppService>(new FakeAppService());
        core.Bridge.Remove<IAppService>();
        core.Bridge.Expose<IAppService>(new FakeAppService());
        capturedScripts.Clear();

        adapter.RaiseWebMessage(
            """{"jsonrpc":"2.0","id":"gen-re","method":"AppService.getCurrentUser","params":{}}""",
            "*", core.ChannelId);
        _dispatcher.RunAll();

        Assert.Contains("Alice", capturedScripts.Last());
    }

    // ==================== GetProxy via generated code ====================

    [Fact]
    public void GetProxy_uses_generated_proxy_when_available()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<IUiController>();

        Assert.NotNull(proxy);
        // Should NOT be a DispatchProxy (BridgeImportProxy), but a generated type.
        Assert.IsNotType<BridgeImportProxy>(proxy);

        // Verify it sends correct RPC.
        var task = proxy.ShowNotification("test-msg");
        Assert.True(capturedScripts.Count > 0);
        var script = capturedScripts.Last();
        Assert.Contains("UiController.showNotification", script);
        Assert.Contains("test-msg", script);
    }
}
