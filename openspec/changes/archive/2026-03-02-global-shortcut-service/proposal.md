## Why

The existing `WebViewShortcutRouter` handles window-local keyboard shortcuts (DevTools toggle, edit commands) but does not support OS-level global hotkeys — shortcuts that work even when the application window is not focused. Desktop applications like clipboard managers, screenshot tools, and productivity apps depend on global shortcuts. Currently developers must write platform-specific code to achieve this, breaking the cross-platform abstraction.

**Goal IDs**: G1 (Type-Safe Bridge — new typed service), G3 (Secure by Default — policy-governed shortcut registration), G4 (Testability — mockable shortcut service)

**ROADMAP justification**: Post-1.0 differentiation. Electron's `globalShortcut.register(accelerator, callback)` is stringly-typed and has no authorization model. Fulora can offer type-safe shortcut registration with policy governance and compile-time TS types.

## What Changes

- Add `IGlobalShortcutService` as a `[JsExport]` bridge service exposing: `Register(shortcut)`, `Unregister(shortcutId)`, `IsRegistered(shortcutId)`, `GetRegistered()`
- Add `IBridgeEvent<GlobalShortcutTriggeredEvent>` for push-based shortcut activation notifications to JS
- Implement platform-specific global hotkey registration (Windows: RegisterHotKey, macOS: CGEvent/MASShortcut pattern, Linux: X11/libkeybinder)
- Integrate with `WebViewHostCapabilityBridge` policy — global shortcut registration requires explicit authorization
- Define `ShortcutBinding` model with `Key`, `Modifiers`, `Id`, and conflict detection

## Non-goals

- Replacing `WebViewShortcutRouter` for window-local shortcuts — they coexist
- Media key handling (play/pause/next) — separate capability
- Shortcut recording/capture UI — application-level concern

## Capabilities

### New Capabilities
- `global-shortcut-bridge`: Typed bridge service for OS-level global hotkey registration, lifecycle management, conflict detection, and activation event dispatch with policy authorization

### Modified Capabilities
- `webview-shortcut-routing`: Extend specification to define coexistence semantics — when a gesture matches both a global shortcut and a window-local shortcut, define priority resolution

## Impact

- `src/Agibuild.Fulora.Core/` — `IGlobalShortcutService` interface, shortcut DTOs
- `src/Agibuild.Fulora.Runtime/` — platform-specific global hotkey implementations (P/Invoke on Windows, native interop on macOS/Linux)
- `src/Agibuild.Fulora.Avalonia/` — integration with Avalonia application lifecycle (register on activate, cleanup on exit)
- `tests/` — contract tests with mock shortcut provider, conflict detection tests
- Platform risk: Linux global shortcuts depend on X11/Wayland — may need conditional support
