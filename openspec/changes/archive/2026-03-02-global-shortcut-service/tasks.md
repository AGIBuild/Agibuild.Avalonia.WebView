## 1. Core Contracts

- [x] 1.1 Define `IGlobalShortcutService` interface with `[JsExport]`: `Register`, `Unregister`, `IsRegistered`, `GetRegistered`, and `IBridgeEvent<GlobalShortcutTriggeredEvent> ShortcutTriggered`
- [x] 1.2 Define DTOs: `GlobalShortcutBinding` (Id, Key, Modifiers), `GlobalShortcutResult` (Success/Denied/Conflict/Unsupported + Reason), `GlobalShortcutTriggeredEvent` (Id, Timestamp)
- [x] 1.3 Define `IGlobalShortcutPlatformProvider` interface: `IsSupported`, `Register`, `Unregister`, `ShortcutActivated` event
- [x] 1.4 Define `ShortcutKey` enum and `ShortcutModifiers` flags enum

## 2. Platform Providers

- [x] 2.1-2.3 Implement `SharpHookGlobalShortcutProvider` using SharpHook (libuiohook) — cross-platform (Win/Mac/Linux X11) in a single provider
- [x] 2.4 Implement `NullGlobalShortcutProvider` returning `IsSupported = false` for unsupported platforms (Wayland, mobile)
- [x] 2.5 Implement platform provider factory selecting correct provider based on `RuntimeInformation`
- [x] 2.6 Add unit tests for each provider with mock OS API layer where feasible

## 3. Service Implementation

- [x] 3.1 Implement `GlobalShortcutService : IGlobalShortcutService` with registration tracking (dictionary by ID)
- [x] 3.2 Integrate policy check via `WebViewHostCapabilityBridge` before each registration
- [x] 3.3 Implement conflict detection (duplicate ID, OS-level conflict)
- [x] 3.4 Wire `IGlobalShortcutPlatformProvider.ShortcutActivated` to `IBridgeEvent<GlobalShortcutTriggeredEvent>`
- [x] 3.5 Implement `IDisposable` with automatic unregistration of all shortcuts
- [x] 3.6 Add contract tests: register/unregister, duplicate ID rejection, policy deny, platform unsupported, dispose cleanup, event firing

## 4. Shortcut Router Coexistence

- [x] 4.1 Extend `WebViewShortcutRouter` to check global shortcut registry for priority resolution
- [x] 4.2 Add contract tests: window-local binding takes priority when both match, global fires when no local match

## 5. Integration

- [x] 5.1 Add integration test: register global shortcut via bridge → simulate trigger → verify JS receives event
- [x] 5.2 Add sample code to `samples/avalonia-react/` demonstrating global shortcut registration from React
