## 1. Core Contracts — Typed Platform Handle Interfaces

- [x] 1.1 Define `IWindowsWebView2PlatformHandle : IPlatformHandle` in `WebViewContracts.cs` with `nint CoreWebView2Handle` and `nint CoreWebView2ControllerHandle`
- [x] 1.2 Define `IAppleWKWebViewPlatformHandle : IPlatformHandle` in `WebViewContracts.cs` with `nint WKWebViewHandle`
- [x] 1.3 Define `IGtkWebViewPlatformHandle : IPlatformHandle` in `WebViewContracts.cs` with `nint WebKitWebViewHandle`
- [x] 1.4 Define `IAndroidWebViewPlatformHandle : IPlatformHandle` in `WebViewContracts.cs` with `nint AndroidWebViewHandle`

## 2. Core Contracts — Lifecycle Event Args and IWebView Update

- [x] 2.1 Define `AdapterCreatedEventArgs : EventArgs` with `IPlatformHandle? PlatformHandle { get; }` in `WebViewContracts.cs`
- [x] 2.2 Add `event EventHandler<AdapterCreatedEventArgs>? AdapterCreated` to `IWebView`
- [x] 2.3 Add `event EventHandler? AdapterDestroyed` to `IWebView`

## 3. Runtime — WebViewCore Lifecycle Events

- [x] 3.1 Add `AdapterCreated` and `AdapterDestroyed` events to `WebViewCore`
- [x] 3.2 Add `_adapterDestroyed` flag to guard at-most-once `AdapterDestroyed` firing
- [x] 3.3 Raise `AdapterCreated` in `Attach()` after `_adapter.Attach()` succeeds, calling `TryGetWebViewHandle()` to populate `AdapterCreatedEventArgs.PlatformHandle`
- [x] 3.4 Raise `AdapterDestroyed` in `Detach()` before calling `_adapter.Detach()`, guarded by `_adapterDestroyed` flag
- [x] 3.5 Raise `AdapterDestroyed` in `Dispose()` if not already raised (guard by same flag)
- [x] 3.6 Ensure `TryGetWebViewHandle()` returns `null` after `_adapterDestroyed` is set
- [x] 3.7 Ensure no events (NavigationStarted, NavigationCompleted, etc.) fire after `AdapterDestroyed`

## 4. WebView Control — Bubble Lifecycle Events

- [x] 4.1 Add `AdapterCreated` and `AdapterDestroyed` events to `WebView` control
- [x] 4.2 Subscribe to `_core.AdapterCreated` and `_core.AdapterDestroyed` in `SubscribeCoreEvents()`
- [x] 4.3 Unsubscribe in `UnsubscribeCoreEvents()`
- [x] 4.4 In `CreateNativeControlCore()`, ensure `AdapterCreated` fires before pending navigation (reorder if needed)

## 5. Windows Adapter — Typed Handle

- [x] 5.1 Create `WindowsWebView2PlatformHandle` record implementing `IWindowsWebView2PlatformHandle` with `CoreWebView2Handle`, `CoreWebView2ControllerHandle`, `Handle`, `HandleDescriptor`
- [x] 5.2 Update `WindowsWebViewAdapter.TryGetWebViewHandle()` to return `WindowsWebView2PlatformHandle` instead of generic `PlatformHandle`

## 6. macOS Adapter — Typed Handle

- [x] 6.1 Create `AppleWKWebViewPlatformHandle` record implementing `IAppleWKWebViewPlatformHandle` with `WKWebViewHandle`, `Handle`, `HandleDescriptor`
- [x] 6.2 Update `MacOSWebViewAdapter.TryGetWebViewHandle()` to return `AppleWKWebViewPlatformHandle`

## 7. iOS Adapter — Typed Handle

- [x] 7.1 Reuse or import `AppleWKWebViewPlatformHandle` record (shared with macOS or duplicated per adapter assembly)
- [x] 7.2 Update `iOSWebViewAdapter.TryGetWebViewHandle()` to return `AppleWKWebViewPlatformHandle`

## 8. Android Adapter — Typed Handle

- [x] 8.1 Create `AndroidWebViewPlatformHandle` record implementing `IAndroidWebViewPlatformHandle` with `AndroidWebViewHandle`, `Handle`, `HandleDescriptor`
- [x] 8.2 Update `AndroidWebViewAdapter.TryGetWebViewHandle()` to return `AndroidWebViewPlatformHandle`

## 9. GTK Adapter — Typed Handle

- [x] 9.1 Create `GtkWebViewPlatformHandle` record implementing `IGtkWebViewPlatformHandle` with `WebKitWebViewHandle`, `Handle`, `HandleDescriptor`
- [x] 9.2 Update `GtkWebViewAdapter.TryGetWebViewHandle()` to return `GtkWebViewPlatformHandle`

## 10. Test — Mock Adapter Update

- [x] 10.1 Update `MockWebViewAdapterWithHandle` to return a typed handle implementing the appropriate platform interface
- [x] 10.2 Add contract tests verifying `AdapterCreated` fires after `Attach()` with correct `PlatformHandle`
- [x] 10.3 Add contract tests verifying `AdapterDestroyed` fires before `Detach()` and at most once
- [x] 10.4 Add contract tests verifying `TryGetWebViewHandle()` returns `null` after `AdapterDestroyed`
- [x] 10.5 Add contract tests verifying no events fire after `AdapterDestroyed`
- [x] 10.6 Verify typed handle pattern-matching works: `e.PlatformHandle is IWindowsWebView2PlatformHandle` etc.
