# bridge-mock-and-migration — Design

**ROADMAP**: Phase 1, Deliverables 1.5 + 1.6

## Overview

`MockBridgeService` implements `IBridgeService` in Core (no Runtime dependency). It records Expose/Remove state and returns configured proxies from GetProxy. Tests inject `MockBridgeService` where `IBridgeService` is expected — no WebView, no RPC, no platform adapters.

Migration tests verify that raw RPC (`Rpc.Handle`) and typed Bridge (`Bridge.Expose`) share the same `IWebViewRpcService` and work correctly together.

## MockBridgeService

```csharp
public sealed class MockBridgeService : IBridgeService
{
    void Expose<T>(T impl, BridgeOptions? options = null);
    T GetProxy<T>();  // throws if SetupProxy not called
    void Remove<T>();

    void SetupProxy<T>(T proxy);           // setup helper
    bool WasExposed<T>();                   // assertion helper
    T? GetExposedImplementation<T>();      // assertion helper
    void Reset();                           // clear all state
    int ExposedCount { get; }
}
```

- Uses `ConcurrentDictionary<Type, object>` for exposed services and proxies.
- `GetProxy<T>` throws `InvalidOperationException` if not configured.
- Supports `IDisposable` — operations throw after dispose.

## Migration / Backward Compatibility

- Raw `Rpc.Handle("method", handler)` and `Bridge.Expose<T>(impl)` coexist.
- Bridge auto-enable does not break subsequent raw RPC registration.
- `Bridge.Remove<T>` only unregisters Bridge handlers; raw handlers unaffected.
- Bridge is opt-in: raw RPC works alone without accessing Bridge.

## Testing

- **8 mock tests** (`MockBridgeServiceTests`): Expose records, WasExposed, GetProxy returns configured, GetProxy without setup throws, Remove clears, Reset clears all, Dispose throws, ExposedCount.
- **4 migration tests** (`BridgeMigrationTests`): raw+Bridge coexist, raw RPC alone, Bridge auto-enable + raw RPC, Remove typed does not affect raw.
