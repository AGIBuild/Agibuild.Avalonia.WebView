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
    #region Easy: Constructor null checks

    [Fact]
    public void WebDialog_null_host_throws()
    {
        var adapter = new MockWebViewAdapter();
        var dispatcher = new TestDispatcher();
        Assert.Throws<ArgumentNullException>(() => new WebDialog(null!, adapter, dispatcher));
    }

    [Fact]
    public void WebAuthBrokerWithSemantics_null_inner_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WebAuthBrokerWithSemantics(null!));
    }

    [Fact]
    public void RuntimeBridgeService_null_rpc_throws()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger("test");
        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeBridgeService(null!, s => Task.FromResult<string?>(null), logger));
    }

    [Fact]
    public void RuntimeBridgeService_null_invokeScript_throws()
    {
        var rpc = new WebViewRpcService(s => Task.FromResult<string?>(null), NullLoggerFactory.Instance.CreateLogger("test"));
        var logger = NullLoggerFactory.Instance.CreateLogger("test");
        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeBridgeService(rpc, null!, logger));
    }

    [Fact]
    public void RuntimeBridgeService_null_logger_throws()
    {
        var rpc = new WebViewRpcService(s => Task.FromResult<string?>(null), NullLoggerFactory.Instance.CreateLogger("test"));
        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeBridgeService(rpc, s => Task.FromResult<string?>(null), null!));
    }

    #endregion

    #region Easy: WebViewAdapterRegistry Windows adapter path

    [Fact]
    public void TryCreateForCurrentPlatform_returns_adapter_when_current_platform_registered()
    {
        // Register an adapter for the current OS so this assertion is deterministic in CI matrix runs.
        var reg = new WebViewAdapterRegistration(
            WebViewLegacyAdapterCompatibility.GetCurrentPlatform(), "branch-coverage-test-adapter",
            () => new MockWebViewAdapter(), Priority: int.MaxValue);
        WebViewAdapterRegistry.Register(reg);

        var result = WebViewAdapterRegistry.TryCreateForCurrentPlatform(out var adapter, out var reason);
        Assert.True(result);
        Assert.NotNull(adapter);
        Assert.Null(reason);
    }

    #endregion

    #region Easy: ActivationRequest explicit receivedAtUtc

    [Fact]
    public void ActivationRequest_explicit_receivedAtUtc_covers_non_null_path()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var request = new WebViewShellActivationRequest(
            new Uri("https://example.test/activate"), receivedAtUtc: ts);
        Assert.Equal(ts, request.ReceivedAtUtc);
    }

    #endregion

    #region Easy: NormalizeProfileHash uppercase hex

    [Fact]
    public void NormalizeProfileHash_uppercase_hex_covers_branch()
    {
        // Using uppercase hex to cover the `isUpperHex` true branch (line 128)
        // and the `isDecimal` false branch (line 126)
        var method = typeof(WebViewSessionPermissionProfile).GetMethod(
            "NormalizeProfileHash", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var upperHash = "sha256:" + "AABBCCDD" + new string('E', 56);
        var result = method!.Invoke(null, [upperHash]);
        Assert.NotNull(result);
        Assert.StartsWith("sha256:", (string)result!);
    }

    [Fact]
    public void NormalizeProfileHash_mixed_case_hex_covers_all_char_branches()
    {
        var method = typeof(WebViewSessionPermissionProfile).GetMethod(
            "NormalizeProfileHash", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Mix of decimal, lowercase, and uppercase to cover all three branches
        var mixedHash = "sha256:" + "0123456789abcdefABCDEF" + new string('0', 42);
        var result = method!.Invoke(null, [mixedHash]);
        Assert.NotNull(result);
    }

    #endregion
}
