## Why

Users need to know when the underlying native WebView adapter is fully initialized and when it is destroyed, so they can safely perform platform-specific operations (e.g., configuring native settings, accessing COM objects, attaching native event handlers). Today, `INativeWebViewHandleProvider` is implemented by all adapters but returns only an untyped `IPlatformHandle`—callers must know the raw handle descriptor string and cast blindly. There is no event to signal when the adapter is ready or torn down, forcing consumers to guess timing or poll.

Avalonia's official `NativeWebView` exposes `AdapterCreated` / `AdapterDestroyed` events and typed platform-specific handle interfaces (`IWindowsWebView2PlatformHandle`, `IAppleWKWebViewPlatformHandle`, etc.). Achieving parity here is critical for advanced interop scenarios.

## What Changes

- **Add adapter lifecycle events** (`AdapterCreated`, `AdapterDestroyed`) on `IWebView`, `WebViewCore`, and `WebView`, raised when the native adapter completes initialization and when it is detached/disposed.
- **Add typed platform-specific handle interfaces** in the Core project:
  - `IWindowsWebView2PlatformHandle` — exposes `CoreWebView2` and `CoreWebView2Controller` COM handles
  - `IAppleWKWebViewPlatformHandle` — exposes `WKWebView` `nint` pointer
  - `IGtkWebViewPlatformHandle` — exposes `WebKitWebView` `nint` pointer
  - `IAndroidWebViewPlatformHandle` — exposes Android `WebView` handle
- **Implement typed handles in each adapter** so `TryGetWebViewHandle()` returns a platform-specific typed handle (which also implements `IPlatformHandle`).
- **Add `AdapterCreatedEventArgs`** carrying the typed platform handle for immediate use in the event handler.

## Capabilities

### New Capabilities
- `adapter-lifecycle-events`: Adapter lifecycle events (AdapterCreated/AdapterDestroyed) raised at the IWebView/WebView level, enabling consumers to react to adapter readiness and teardown.
- `typed-platform-handles`: Typed platform-specific handle interfaces that provide strongly-typed access to native WebView handles, replacing the need to interpret raw `IPlatformHandle` descriptors.

### Modified Capabilities
- `webview-core-contracts`: Add `AdapterCreated`/`AdapterDestroyed` events to `IWebView`; add new typed handle interfaces and `AdapterCreatedEventArgs`.
- `webview-adapter-abstraction`: Adapters' `TryGetWebViewHandle()` returns typed handle instances; adapter lifecycle signaling from runtime to control layer.

## Impact

- **Core contracts** (`Agibuild.Fulora.Core`): New event args, typed handle interfaces, `IWebView` gains two events.
- **Runtime** (`Agibuild.Fulora.Runtime`): `WebViewCore` raises lifecycle events during `Attach`/`Detach`/`Dispose`.
- **Control** (`Agibuild.Fulora`): `WebView` subscribes and bubbles lifecycle events.
- **All adapters** (Windows, macOS, iOS, Android, Gtk): Return typed handle from `TryGetWebViewHandle()`.
- **Tests**: New contract tests for lifecycle event ordering guarantees and typed handle resolution.
- **No breaking changes**: `INativeWebViewHandleProvider.TryGetWebViewHandle()` return type stays `IPlatformHandle?`; new typed interfaces are additive.
