using System.Reflection;
using System.Text.Json;
using Agibuild.Fulora;
using Agibuild.Fulora.Adapters.Abstractions;
using Agibuild.Fulora.Shell;
using Agibuild.Fulora.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed partial class BranchCoverageRound3Tests
{
    #region Medium: RuntimeBridgeService ExtractMethodName no dot

    [Fact]
    public void ExtractMethodName_no_dot_returns_full_string()
    {
        // ExtractMethodName is now in RpcMethodHelpers (internal, accessible via InternalsVisibleTo)
        var result = RpcMethodHelpers.ExtractMethodName("NoDotMethodName");
        Assert.Equal("NoDotMethodName", result);

        var result2 = RpcMethodHelpers.ExtractMethodName("Service.Method");
        Assert.Equal("Method", result2);
    }

    #endregion

    #region Round 4: SPA autoInject when bridge already enabled

    [Fact]
    public void EnableSpaHosting_autoInject_skipped_when_bridge_already_enabled()
    {
        // Line 944: options.AutoInjectBridgeScript && !_webMessageBridgeEnabled
        // Cover the branch where AutoInjectBridgeScript=true but _webMessageBridgeEnabled=true
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        core.EnableWebMessageBridge(new WebMessageBridgeOptions());

        core.EnableSpaHosting(new SpaHostingOptions
        {
            DevServerUrl = "http://localhost:5173"
        });
    }

    [Fact]
    public void EnableSpaHosting_autoInject_disabled_covers_false_branch()
    {
        // Line 944: AutoInjectBridgeScript=false → short-circuit, skip auto-inject
        var dispatcher = new TestDispatcher();
        var adapter = MockWebViewAdapter.CreateFull();
        using var core = new WebViewCore(adapter, dispatcher);
        core.Attach(new TestPlatformHandle(IntPtr.Zero, "test-parent"));

        core.EnableSpaHosting(new SpaHostingOptions
        {
            DevServerUrl = "http://localhost:5173",
            AutoInjectBridgeScript = false
        });
    }

    #endregion

    #region Round 5 Tier 1: RuntimeBridgeService proxy branches

    [Fact]
    public void BridgeImportProxy_no_args_method_sends_null_params()
    {
        var proxy = DispatchProxy.Create<INoArgImport, BridgeImportProxy>();
        var bridgeProxy = (BridgeImportProxy)(object)proxy;

        string? capturedMethod = null;
        object? capturedParams = null;
        var mockRpc = new LambdaRpcService((method, p) =>
        {
            capturedMethod = method;
            capturedParams = p;
            return Task.CompletedTask;
        });
        bridgeProxy.Initialize(mockRpc, "TestSvc");

        _ = proxy.DoAsync();

        Assert.Equal("TestSvc.doAsync", capturedMethod);
        Assert.Null(capturedParams);
    }

    [Fact]
    public void BridgeImportProxy_non_task_return_throws_not_supported()
    {
        var proxy = DispatchProxy.Create<ISyncReturnImport, BridgeImportProxy>();
        var bridgeProxy = (BridgeImportProxy)(object)proxy;

        var mockRpc = new LambdaRpcService((_, _) => Task.CompletedTask);
        bridgeProxy.Initialize(mockRpc, "TestSvc");

        Assert.Throws<NotSupportedException>(() => proxy.GetValue());
    }

    #endregion

    #region Round 5 Tier 1: SpaAssetHotUpdateService NormalizeVersion

    [Fact]
    public void NormalizeVersion_whitespace_only_throws()
    {
        var method = typeof(SpaAssetHotUpdateService)
            .GetMethod("NormalizeVersion", BindingFlags.NonPublic | BindingFlags.Static)!;

        var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, ["   "]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    #endregion

    #region Round 5 Tier 2: WebViewHostCapabilityBridge metadata validation

    [Fact]
    public void HostCapabilityBridge_metadata_too_many_entries_denied()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < 10; i++)
            metadata[$"platform.extension.key{i}"] = $"value{i}";

        var request = new WebViewSystemIntegrationEventRequest
        {
            Source = "test",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "item1",
            Metadata = metadata
        };

        var result = bridge.DispatchSystemIntegrationEvent(request, Guid.NewGuid());
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    [Fact]
    public void HostCapabilityBridge_metadata_value_too_long_denied()
    {
        var provider = new MinimalHostCapabilityProvider();
        var bridge = new WebViewHostCapabilityBridge(provider);

        var request = new WebViewSystemIntegrationEventRequest
        {
            Source = "test",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
            ItemId = "item1",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["platform.extension.data"] = new string('x', 300)
            }
        };

        var result = bridge.DispatchSystemIntegrationEvent(request, Guid.NewGuid());
        Assert.Equal(WebViewHostCapabilityCallOutcome.Deny, result.Outcome);
    }

    #endregion
}
