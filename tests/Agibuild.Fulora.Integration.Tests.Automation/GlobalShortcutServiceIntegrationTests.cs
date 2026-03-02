using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.Integration.Tests.Automation;

/// <summary>
/// Integration tests verifying the end-to-end flow:
/// register global shortcut via service → simulate trigger → verify bridge event fires.
/// Uses mock platform provider to avoid OS-level hook in CI.
/// </summary>
public sealed class GlobalShortcutServiceIntegrationTests
{
    private sealed class MockProvider : IGlobalShortcutPlatformProvider
    {
        private readonly Dictionary<string, (ShortcutKey, ShortcutModifiers)> _regs = new();
        public bool IsSupported => true;
        public event Action<string>? ShortcutActivated;

        public bool Register(string id, ShortcutKey key, ShortcutModifiers modifiers)
        {
            _regs[id] = (key, modifiers);
            return true;
        }

        public bool Unregister(string id) => _regs.Remove(id);
        public void Dispose() { }
        public void FireShortcut(string id) => ShortcutActivated?.Invoke(id);
    }

    private sealed class AllowAllPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => WebViewHostCapabilityDecision.Allow();
    }

    [Fact]
    public async Task Register_then_trigger_fires_bridge_event()
    {
        var provider = new MockProvider();
        using var service = new GlobalShortcutService(provider, new AllowAllPolicy());

        var result = await service.Register(new GlobalShortcutBinding
        {
            Id = "toggle-overlay",
            Key = ShortcutKey.Space,
            Modifiers = ShortcutModifiers.Ctrl | ShortcutModifiers.Shift
        });

        Assert.Equal(GlobalShortcutResultStatus.Success, result.Status);

        GlobalShortcutTriggeredEvent? evt = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(e => evt = e);

        provider.FireShortcut("toggle-overlay");

        Assert.NotNull(evt);
        Assert.Equal("toggle-overlay", evt.Id);
    }

    [Fact]
    public async Task Unregister_then_trigger_does_not_fire()
    {
        var provider = new MockProvider();
        using var service = new GlobalShortcutService(provider);

        await service.Register(new GlobalShortcutBinding
        {
            Id = "test",
            Key = ShortcutKey.F5,
            Modifiers = ShortcutModifiers.Ctrl
        });

        await service.Unregister("test");

        GlobalShortcutTriggeredEvent? evt = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(e => evt = e);

        provider.FireShortcut("test");
        Assert.Null(evt);
    }

    [Fact]
    public async Task Full_lifecycle_register_query_unregister()
    {
        var provider = new MockProvider();
        using var service = new GlobalShortcutService(provider);

        var r1 = await service.Register(new GlobalShortcutBinding
        {
            Id = "a",
            Key = ShortcutKey.A,
            Modifiers = ShortcutModifiers.Ctrl
        });
        var r2 = await service.Register(new GlobalShortcutBinding
        {
            Id = "b",
            Key = ShortcutKey.B,
            Modifiers = ShortcutModifiers.Alt
        });

        Assert.Equal(GlobalShortcutResultStatus.Success, r1.Status);
        Assert.Equal(GlobalShortcutResultStatus.Success, r2.Status);

        Assert.True(await service.IsRegistered("a"));
        Assert.True(await service.IsRegistered("b"));
        Assert.False(await service.IsRegistered("c"));

        var all = await service.GetRegistered();
        Assert.Equal(2, all.Length);

        await service.Unregister("a");
        Assert.False(await service.IsRegistered("a"));
        Assert.Equal(1, (await service.GetRegistered()).Length);
    }

    [Fact]
    public async Task Suppression_prevents_event_from_global_service()
    {
        var provider = new MockProvider();
        using var service = new GlobalShortcutService(provider);

        await service.Register(new GlobalShortcutBinding
        {
            Id = "ctrl-c",
            Key = ShortcutKey.C,
            Modifiers = ShortcutModifiers.Ctrl
        });

        GlobalShortcutTriggeredEvent? evt = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(e => evt = e);

        service.SuppressNextActivation("ctrl-c");
        provider.FireShortcut("ctrl-c");
        Assert.Null(evt);

        provider.FireShortcut("ctrl-c");
        Assert.NotNull(evt);
    }
}
