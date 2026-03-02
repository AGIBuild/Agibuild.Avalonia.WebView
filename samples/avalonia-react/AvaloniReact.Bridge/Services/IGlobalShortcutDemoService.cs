using Agibuild.Fulora;

namespace AvaloniReact.Bridge.Services;

/// <summary>
/// Demonstrates global shortcut registration via the Fulora bridge.
/// Wraps the framework's <see cref="IGlobalShortcutService"/> for the sample app.
/// </summary>
[JsExport]
public interface IGlobalShortcutDemoService
{
    Task<ShortcutRegistrationResult> RegisterShortcut(ShortcutRegistrationRequest request);
    Task<ShortcutRegistrationResult> UnregisterShortcut(ShortcutUnregisterRequest request);
    Task<ShortcutBindingInfo[]> GetRegistered();
    IBridgeEvent<ShortcutFiredEvent> OnShortcutFired { get; }
}

public sealed class ShortcutRegistrationRequest
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public string[]? Modifiers { get; init; }
}

public sealed class ShortcutUnregisterRequest
{
    public required string Id { get; init; }
}

public sealed class ShortcutRegistrationResult
{
    public required bool Success { get; init; }
    public string? Reason { get; init; }
}

public sealed class ShortcutBindingInfo
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string[] Modifiers { get; init; }
}

public sealed class ShortcutFiredEvent
{
    public required string Id { get; init; }
    public required string Timestamp { get; init; }
}
