using System;
using System.Linq;
using System.Threading.Tasks;
using Agibuild.Fulora;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

/// <summary>
/// Desktop implementation of <see cref="IGlobalShortcutDemoService"/>.
/// Wraps the framework's <see cref="GlobalShortcutService"/> to demonstrate
/// global shortcut registration from React.
/// </summary>
internal sealed class GlobalShortcutDemoService : IGlobalShortcutDemoService, IDisposable
{
    private readonly GlobalShortcutService _inner;
    private readonly BridgeEvent<ShortcutFiredEvent> _onShortcutFired = new();

    public GlobalShortcutDemoService(GlobalShortcutService inner)
    {
        _inner = inner;
        ((BridgeEvent<GlobalShortcutTriggeredEvent>)_inner.ShortcutTriggered)
            .Connect(OnTriggered);
    }

    public IBridgeEvent<ShortcutFiredEvent> OnShortcutFired => _onShortcutFired;

    public async Task<ShortcutRegistrationResult> RegisterShortcut(ShortcutRegistrationRequest request)
    {
        if (!Enum.TryParse<ShortcutKey>(request.Key, true, out var key))
            return new ShortcutRegistrationResult { Success = false, Reason = $"Unknown key: {request.Key}" };

        var mods = ShortcutModifiers.None;
        if (request.Modifiers is not null)
        {
            foreach (var m in request.Modifiers)
            {
                if (Enum.TryParse<ShortcutModifiers>(m, true, out var mod))
                    mods |= mod;
            }
        }

        var result = await _inner.Register(new GlobalShortcutBinding
        {
            Id = request.Id,
            Key = key,
            Modifiers = mods
        });

        return new ShortcutRegistrationResult
        {
            Success = result.Status == GlobalShortcutResultStatus.Success,
            Reason = result.Reason
        };
    }

    public async Task<ShortcutRegistrationResult> UnregisterShortcut(ShortcutUnregisterRequest request)
    {
        var result = await _inner.Unregister(request.Id);
        return new ShortcutRegistrationResult
        {
            Success = result.Status == GlobalShortcutResultStatus.Success,
            Reason = result.Reason
        };
    }

    public async Task<ShortcutBindingInfo[]> GetRegistered()
    {
        var bindings = await _inner.GetRegistered();
        return bindings.Select(b => new ShortcutBindingInfo
        {
            Id = b.Id,
            Key = b.Key.ToString(),
            Modifiers = GetModifierNames(b.Modifiers)
        }).ToArray();
    }

    private static string[] GetModifierNames(ShortcutModifiers mods)
    {
        var result = new System.Collections.Generic.List<string>();
        if ((mods & ShortcutModifiers.Ctrl) != 0) result.Add("Ctrl");
        if ((mods & ShortcutModifiers.Alt) != 0) result.Add("Alt");
        if ((mods & ShortcutModifiers.Shift) != 0) result.Add("Shift");
        if ((mods & ShortcutModifiers.Meta) != 0) result.Add("Meta");
        return result.ToArray();
    }

    private void OnTriggered(GlobalShortcutTriggeredEvent evt)
    {
        _onShortcutFired.Emit(new ShortcutFiredEvent
        {
            Id = evt.Id,
            Timestamp = evt.Timestamp.ToString("O")
        });
    }

    public void Dispose()
    {
        // Inner service is disposed by the host
    }
}
