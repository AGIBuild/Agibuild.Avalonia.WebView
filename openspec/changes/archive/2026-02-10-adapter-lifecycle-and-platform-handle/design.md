## Context

The WebView library currently follows a strict adapter lifecycle: `Initialize()` → `Attach()` → usage → `Detach()` → dispose. However, **consumers have no way to know when these transitions happen**. The `WebView` control creates `WebViewCore` inside `CreateNativeControlCore()` and destroys it in `DestroyNativeControlCore()`, but these are internal to the control; no public event signals readiness or teardown.

Additionally, `INativeWebViewHandleProvider` is implemented by all five adapters, and `WebViewCore.TryGetWebViewHandle()` already delegates to it. However, the returned `IPlatformHandle` is untyped — callers must know the `HandleDescriptor` string (`"WebView2"`, `"WKWebView"`, `"WebKitGTK"`, `"AndroidWebView"`) and interpret the raw `Handle` property correctly. This is error-prone and not discoverable.

Avalonia's official WebView provides `AdapterCreated`/`AdapterDestroyed` events and typed platform handle interfaces. Matching this is necessary for interop scenarios.

## Goals / Non-Goals

**Goals:**
- Expose `AdapterCreated` and `AdapterDestroyed` events on `IWebView`, `WebViewCore`, and the `WebView` control.
- Define typed platform-specific handle interfaces in Core for compile-time discovery.
- Have each adapter return typed handle instances from `TryGetWebViewHandle()`.
- Ensure lifecycle events fire with correct ordering guarantees relative to other events.

**Non-Goals:**
- Reparenting support (`BeginReparenting`) — deferred to a future milestone.
- `ICommandManager` implementation — deferred.
- Print support (`ShowPrintUI`, `PrintToPdfStreamAsync`) — deferred.
- Changing the adapter SPI (`IWebViewAdapter`) — the lifecycle signaling is raised by the runtime, not by adapters.

## Decisions

### 1) Lifecycle events are raised by WebViewCore, not by adapters

**Decision:** `AdapterCreated` is raised by `WebViewCore` immediately after `Attach()` succeeds. `AdapterDestroyed` is raised during `Detach()` or `Dispose()`, whichever comes first. Adapters are unaware of these events.

**Alternatives considered:**
- *Adapter raises lifecycle events* — Rejected. Would require changing `IWebViewAdapter` (breaking SPI change) and duplicating event logic across 5 adapters. The runtime already owns the lifecycle call sequence.

**Rationale:** The runtime (`WebViewCore`) is the single place that calls `adapter.Attach()` and `adapter.Detach()`. It knows exactly when the adapter transitions. Keeping this in the runtime follows the existing pattern (all public events flow through `WebViewCore`).

### 2) AdapterCreatedEventArgs carries the typed platform handle

**Decision:** `AdapterCreatedEventArgs` includes an `IPlatformHandle? PlatformHandle` property, populated by calling `TryGetWebViewHandle()` at event raise time.

**Rationale:** The most common use case for `AdapterCreated` is immediate access to the native handle for platform-specific configuration. Bundling it in the event args eliminates a round-trip call.

### 3) Typed handle interfaces live in Core, implementations in adapters

**Decision:** Define marker interfaces in `Agibuild.Fulora.Core`:
- `IWindowsWebView2PlatformHandle : IPlatformHandle` — `nint CoreWebView2Handle`, `nint CoreWebView2ControllerHandle`
- `IAppleWKWebViewPlatformHandle : IPlatformHandle` — `nint WKWebViewHandle`
- `IGtkWebViewPlatformHandle : IPlatformHandle` — `nint WebKitWebViewHandle`
- `IAndroidWebViewPlatformHandle : IPlatformHandle` — `nint AndroidWebViewHandle`

Each adapter's `TryGetWebViewHandle()` returns a concrete record that implements the platform-specific interface. Since the interfaces are in Core, consumers can pattern-match without referencing adapter assemblies:

```csharp
webView.AdapterCreated += (s, e) =>
{
    if (e.PlatformHandle is IWindowsWebView2PlatformHandle win)
    {
        // Use win.CoreWebView2Handle
    }
};
```

**Alternatives considered:**
- *Typed interfaces in adapter assemblies* — Rejected. Forces consumers to reference platform assemblies, defeats cross-platform coding.
- *Single generic handle with metadata dictionary* — Rejected. Not discoverable, no compile-time safety.

**Rationale:** Interfaces in Core means cross-platform code can pattern-match. Only the adapter that runs on the active platform will produce a matching instance. Other adapters' interfaces remain unused but harmless.

### 4) Event ordering guarantees

**Decision:**
- `AdapterCreated` fires **after** `Attach()` succeeds and **before** any pending navigation starts.
- `AdapterDestroyed` fires **before** `Detach()` is called on the adapter, giving consumers a last chance to use the handle.
- `AdapterDestroyed` fires at most once (guarded by a flag).
- After `AdapterDestroyed`, `TryGetWebViewHandle()` returns `null`.

**Rationale:** `AdapterCreated` after attach ensures the native handle is valid. `AdapterDestroyed` before detach gives consumers time to release their own native references. The "at most once" guard prevents double-fire if both `Detach()` and `Dispose()` are called.

### 5) IWebView gains lifecycle events without breaking changes

**Decision:** Add two events to `IWebView`:
- `event EventHandler<AdapterCreatedEventArgs>? AdapterCreated`
- `event EventHandler? AdapterDestroyed`

This is technically a **breaking change for implementers** of `IWebView`, but `IWebView` is only implemented by `WebViewCore` (internal) and tests. Consumers only subscribe to events, so this is safe in practice.

**Mitigation:** Since this project is pre-1.0 preview, interface evolution is expected.

### 6) AdapterDestroyedEventArgs is not needed

**Decision:** Use plain `EventHandler` (no custom args) for `AdapterDestroyed`. The event signals teardown; the handle is about to become invalid and should not be accessed.

**Alternatives considered:**
- *Include handle in destroyed args* — Rejected. Handle is about to be invalidated; encouraging access in the handler creates a use-after-free risk.

## Risks / Trade-offs

- **[Risk] IWebView interface change breaks custom implementers** → Mitigation: Pre-1.0 project; document as expected. Only `WebViewCore` and test mocks implement `IWebView`.
- **[Risk] Handle is used after AdapterDestroyed** → Mitigation: `TryGetWebViewHandle()` returns `null` after destroyed. Document that handles obtained before `AdapterDestroyed` become invalid.
- **[Risk] Typed handle interfaces with `nint` are not safe for COM interop on Windows** → Mitigation: Expose raw `nint` only; advanced users who need COM wrappers create their own. This matches Avalonia's approach.
- **[Trade-off] Typed interfaces in Core create a coupling to platform concepts** → Acceptable: interfaces are purely declarative marker types with no platform dependencies. The `nint` type is framework-standard.
