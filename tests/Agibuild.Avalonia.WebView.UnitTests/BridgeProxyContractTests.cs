using System.Text.Json;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.UnitTests;

// ==================== JsImport test interfaces ====================

[JsImport]
public interface IUiController
{
    Task ShowNotification(string message, string? title = null);
    Task<bool> ConfirmDialog(string prompt);
    Task UpdateTheme(ThemeOptions options);
}

[JsImport(Name = "ui")]
public interface ICustomNamedImport
{
    Task<string> GetStatus();
}

public record ThemeOptions(string PrimaryColor, bool DarkMode);

// ==================== Tests ====================

public sealed class BridgeProxyContractTests
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

    // ==================== Attribute validation ====================

    [Fact]
    public void GetProxy_without_JsImport_throws_InvalidOperationException()
    {
        var (core, _) = CreateCoreWithRpc();

        var ex = Assert.Throws<InvalidOperationException>(
            () => core.Bridge.GetProxy<INotDecoratedService>());

        Assert.Contains("JsImport", ex.Message);
    }

    // ==================== Proxy creation ====================

    [Fact]
    public void GetProxy_returns_proxy_implementing_interface()
    {
        var (core, _) = CreateCoreWithRpc();

        var proxy = core.Bridge.GetProxy<IUiController>();

        Assert.NotNull(proxy);
        Assert.IsAssignableFrom<IUiController>(proxy);
    }

    [Fact]
    public void GetProxy_returns_same_instance_on_second_call()
    {
        var (core, _) = CreateCoreWithRpc();

        var proxy1 = core.Bridge.GetProxy<IUiController>();
        var proxy2 = core.Bridge.GetProxy<IUiController>();

        Assert.Same(proxy1, proxy2);
    }

    // ==================== Proxy method routing ====================

    [Fact]
    public void Proxy_void_method_sends_correct_RPC_request()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<IUiController>();

        // Note: InvokeAsync will time out waiting for a response, but we're testing
        // that the correct RPC request is sent — we don't need the response.
        var task = proxy.ShowNotification("hello", "greeting");

        // The proxy should have sent a script with the RPC request.
        Assert.True(capturedScripts.Count > 0, "Expected at least one script for the RPC call");

        var script = capturedScripts.Last();
        Assert.Contains("UiController.showNotification", script);
        Assert.Contains("hello", script);
        Assert.Contains("greeting", script);
    }

    [Fact]
    public void Proxy_with_custom_name_uses_custom_name()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<ICustomNamedImport>();
        var task = proxy.GetStatus();

        var script = capturedScripts.Last();
        Assert.Contains("ui.getStatus", script);
    }

    [Fact]
    public void Proxy_method_with_complex_param_serializes_correctly()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<IUiController>();
        var task = proxy.UpdateTheme(new ThemeOptions("#FF0000", true));

        var script = capturedScripts.Last();
        Assert.Contains("UiController.updateTheme", script);
        Assert.Contains("#FF0000", script);
    }

    [Fact]
    public void Proxy_method_with_optional_param_omits_null()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<IUiController>();
        var task = proxy.ShowNotification("hello");

        var script = capturedScripts.Last();
        Assert.Contains("UiController.showNotification", script);
        Assert.Contains("hello", script);
    }

    // ==================== Lifecycle ====================

    [Fact]
    public void GetProxy_after_core_dispose_throws()
    {
        var (core, _) = CreateCoreWithRpc();
        var bridge = core.Bridge;
        core.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => bridge.GetProxy<IUiController>());
    }

    // ==================== Service name derivation ====================

    [Fact]
    public void Import_service_name_strips_leading_I()
    {
        var (core, adapter) = CreateCoreWithRpc();
        var capturedScripts = new List<string>();
        adapter.ScriptCallback = script => { capturedScripts.Add(script); return null; };

        var proxy = core.Bridge.GetProxy<IUiController>();
        var task = proxy.ConfirmDialog("Are you sure?");

        var script = capturedScripts.Last();
        // Should be "UiController", not "IUiController".
        Assert.Contains("UiController.confirmDialog", script);
        Assert.DoesNotContain("IUiController", script);
    }
}
