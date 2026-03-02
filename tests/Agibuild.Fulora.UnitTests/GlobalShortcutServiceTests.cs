using Agibuild.Fulora;
using Agibuild.Fulora.Shell;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

#region Test Doubles

internal sealed class MockPlatformProvider : IGlobalShortcutPlatformProvider
{
    private readonly Dictionary<string, (ShortcutKey, ShortcutModifiers)> _registrations = new();
    private readonly HashSet<(ShortcutKey, ShortcutModifiers)> _conflicts = new();

    public bool IsSupported { get; set; } = true;

    public event Action<string>? ShortcutActivated;

    public bool Register(string id, ShortcutKey key, ShortcutModifiers modifiers)
    {
        if (_conflicts.Contains((key, modifiers)))
            return false;

        _registrations[id] = (key, modifiers);
        return true;
    }

    public bool Unregister(string id) => _registrations.Remove(id);

    public void Dispose() { }

    public void SimulateActivation(string id) => ShortcutActivated?.Invoke(id);

    public void AddConflict(ShortcutKey key, ShortcutModifiers modifiers)
        => _conflicts.Add((key, modifiers));

    public int RegistrationCount => _registrations.Count;
}

internal sealed class AllowAllPolicy : IWebViewHostCapabilityPolicy
{
    public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
        => WebViewHostCapabilityDecision.Allow();
}

internal sealed class DenyAllPolicy : IWebViewHostCapabilityPolicy
{
    public string Reason { get; init; } = "Denied by policy.";

    public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
        => WebViewHostCapabilityDecision.Deny(Reason);
}

#endregion

public class GlobalShortcutServiceTests
{
    private static GlobalShortcutBinding MakeBinding(string id, ShortcutKey key = ShortcutKey.A,
        ShortcutModifiers modifiers = ShortcutModifiers.Ctrl)
        => new() { Id = id, Key = key, Modifiers = modifiers };

    // ─── Registration ───────────────────────────────────────────────────

    [Fact]
    public async Task Register_Success()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);

        var result = await service.Register(MakeBinding("s1"));

        Assert.Equal(GlobalShortcutResultStatus.Success, result.Status);
        Assert.True(await service.IsRegistered("s1"));
    }

    [Fact]
    public async Task Register_DuplicateId_ReturnsDuplicateId()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);

        await service.Register(MakeBinding("s1", ShortcutKey.A, ShortcutModifiers.Ctrl));
        var result = await service.Register(MakeBinding("s1", ShortcutKey.B, ShortcutModifiers.Alt));

        Assert.Equal(GlobalShortcutResultStatus.DuplicateId, result.Status);
    }

    [Fact]
    public async Task Register_Conflict_ReturnsConflict()
    {
        var provider = new MockPlatformProvider();
        provider.AddConflict(ShortcutKey.F5, ShortcutModifiers.Ctrl);
        using var service = new GlobalShortcutService(provider);

        var result = await service.Register(MakeBinding("s1", ShortcutKey.F5, ShortcutModifiers.Ctrl));

        Assert.Equal(GlobalShortcutResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Register_Unsupported_ReturnsUnsupported()
    {
        var provider = new MockPlatformProvider { IsSupported = false };
        using var service = new GlobalShortcutService(provider);

        var result = await service.Register(MakeBinding("s1"));

        Assert.Equal(GlobalShortcutResultStatus.Unsupported, result.Status);
    }

    [Fact]
    public async Task Register_PolicyDeny_ReturnsDenied()
    {
        var provider = new MockPlatformProvider();
        var policy = new DenyAllPolicy { Reason = "Not allowed." };
        using var service = new GlobalShortcutService(provider, policy);

        var result = await service.Register(MakeBinding("s1"));

        Assert.Equal(GlobalShortcutResultStatus.Denied, result.Status);
        Assert.Equal("Not allowed.", result.Reason);
        Assert.Equal(0, provider.RegistrationCount);
    }

    [Fact]
    public async Task Register_PolicyAllow_RegistersSuccessfully()
    {
        var provider = new MockPlatformProvider();
        var policy = new AllowAllPolicy();
        using var service = new GlobalShortcutService(provider, policy);

        var result = await service.Register(MakeBinding("s1"));

        Assert.Equal(GlobalShortcutResultStatus.Success, result.Status);
    }

    // ─── Unregister ─────────────────────────────────────────────────────

    [Fact]
    public async Task Unregister_Success()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1"));

        var result = await service.Unregister("s1");

        Assert.Equal(GlobalShortcutResultStatus.Success, result.Status);
        Assert.False(await service.IsRegistered("s1"));
    }

    [Fact]
    public async Task Unregister_NotFound()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);

        var result = await service.Unregister("missing");

        Assert.Equal(GlobalShortcutResultStatus.NotFound, result.Status);
    }

    // ─── GetRegistered ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRegistered_ReturnsAllBindings()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1", ShortcutKey.A, ShortcutModifiers.Ctrl));
        await service.Register(MakeBinding("s2", ShortcutKey.B, ShortcutModifiers.Alt));

        var bindings = await service.GetRegistered();

        Assert.Equal(2, bindings.Length);
        Assert.Contains(bindings, b => b.Id == "s1");
        Assert.Contains(bindings, b => b.Id == "s2");
    }

    // ─── Dispose ────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_UnregistersAll()
    {
        var provider = new MockPlatformProvider();
        var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1", ShortcutKey.A, ShortcutModifiers.Ctrl));
        await service.Register(MakeBinding("s2", ShortcutKey.B, ShortcutModifiers.Alt));

        service.Dispose();

        Assert.Equal(0, provider.RegistrationCount);
    }

    // ─── Event Firing ───────────────────────────────────────────────────

    [Fact]
    public async Task ShortcutActivated_FiresBridgeEvent()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1"));

        GlobalShortcutTriggeredEvent? captured = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(evt => captured = evt);

        provider.SimulateActivation("s1");

        Assert.NotNull(captured);
        Assert.Equal("s1", captured.Id);
        Assert.True(captured.Timestamp <= DateTimeOffset.UtcNow);
    }

    // ─── NullProvider ───────────────────────────────────────────────────

    [Fact]
    public void NullProvider_IsNotSupported()
    {
        using var p = new NullGlobalShortcutProvider();
        Assert.False(p.IsSupported);
    }

    [Fact]
    public void NullProvider_RegisterReturnsFalse()
    {
        using var p = new NullGlobalShortcutProvider();
        Assert.False(p.Register("x", ShortcutKey.A, ShortcutModifiers.Ctrl));
    }

    // ─── SharpHook Key Mapping (static, no OS APIs) ────────────────────

    [Theory]
    [InlineData(SharpHook.Data.KeyCode.VcA, ShortcutKey.A)]
    [InlineData(SharpHook.Data.KeyCode.VcZ, ShortcutKey.Z)]
    [InlineData(SharpHook.Data.KeyCode.VcF12, ShortcutKey.F12)]
    [InlineData(SharpHook.Data.KeyCode.VcSpace, ShortcutKey.Space)]
    [InlineData(SharpHook.Data.KeyCode.VcEscape, ShortcutKey.Escape)]
    public void MapKeyCode_CorrectMapping(SharpHook.Data.KeyCode input, ShortcutKey expected)
    {
        Assert.Equal(expected, SharpHookGlobalShortcutProvider.MapKeyCode(input));
    }

    [Fact]
    public void MapKeyCode_UnknownReturnsNone()
    {
        Assert.Equal(ShortcutKey.None, SharpHookGlobalShortcutProvider.MapKeyCode((SharpHook.Data.KeyCode)9999));
    }

    [Theory]
    [InlineData(SharpHook.Data.EventMask.Ctrl, ShortcutModifiers.Ctrl)]
    [InlineData(SharpHook.Data.EventMask.Alt, ShortcutModifiers.Alt)]
    [InlineData(SharpHook.Data.EventMask.Shift, ShortcutModifiers.Shift)]
    [InlineData(SharpHook.Data.EventMask.Meta, ShortcutModifiers.Meta)]
    public void MapModifiers_SingleModifier(SharpHook.Data.EventMask input, ShortcutModifiers expected)
    {
        Assert.Equal(expected, SharpHookGlobalShortcutProvider.MapModifiers(input));
    }

    [Fact]
    public void MapModifiers_Combined()
    {
        var combined = SharpHook.Data.EventMask.Ctrl | SharpHook.Data.EventMask.Shift;
        var result = SharpHookGlobalShortcutProvider.MapModifiers(combined);
        Assert.Equal(ShortcutModifiers.Ctrl | ShortcutModifiers.Shift, result);
    }

    // ─── Suppression (coexistence) ──────────────────────────────────────

    [Fact]
    public async Task SuppressNextActivation_PreventsOneEmission()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1"));

        GlobalShortcutTriggeredEvent? captured = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(evt => captured = evt);

        service.SuppressNextActivation("s1");
        provider.SimulateActivation("s1");

        Assert.Null(captured);
    }

    [Fact]
    public async Task SuppressNextActivation_OnlySuppressesOnce()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1"));

        GlobalShortcutTriggeredEvent? captured = null;
        var bridgeEvent = (BridgeEvent<GlobalShortcutTriggeredEvent>)service.ShortcutTriggered;
        bridgeEvent.Connect(evt => captured = evt);

        service.SuppressNextActivation("s1");
        provider.SimulateActivation("s1"); // suppressed
        Assert.Null(captured);

        provider.SimulateActivation("s1"); // not suppressed
        Assert.NotNull(captured);
        Assert.Equal("s1", captured.Id);
    }

    [Fact]
    public async Task FindIdByChord_ReturnsIdForRegisteredChord()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);
        await service.Register(MakeBinding("s1", ShortcutKey.Space, ShortcutModifiers.Ctrl | ShortcutModifiers.Shift));

        Assert.Equal("s1", service.FindIdByChord(ShortcutKey.Space, ShortcutModifiers.Ctrl | ShortcutModifiers.Shift));
    }

    [Fact]
    public void FindIdByChord_ReturnsNull_WhenNoMatch()
    {
        var provider = new MockPlatformProvider();
        using var service = new GlobalShortcutService(provider);

        Assert.Null(service.FindIdByChord(ShortcutKey.A, ShortcutModifiers.Ctrl));
    }
}
