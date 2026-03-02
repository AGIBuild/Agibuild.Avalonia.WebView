## Context

The existing `WebViewShortcutRouter` in `Agibuild.Fulora.Avalonia` provides window-local keyboard shortcut routing with deterministic gesture matching. It maps key combinations to `WebViewShortcutAction` values (DevTools toggle, edit commands) within the focused window.

OS-level global shortcuts (hotkeys that work when the app is not focused) require platform-specific APIs:
- Windows: `RegisterHotKey` / `UnregisterHotKey` Win32 APIs
- macOS: `CGEvent` tap or `NSEvent.addGlobalMonitorForEvents`
- Linux: X11 `XGrabKey` or `libkeybinder` (Wayland has no global shortcut API in most compositors)

## Goals / Non-Goals

**Goals:**
- Provide `IGlobalShortcutService` as a `[JsExport]` bridge service for OS-level global hotkey registration
- Support register, unregister, list, and is-registered operations
- Push hotkey activation to JS via `IBridgeEvent<GlobalShortcutTriggeredEvent>`
- Integrate with capability bridge policy for shortcut registration authorization
- Define priority resolution between global shortcuts and window-local `WebViewShortcutRouter` bindings
- Support Windows and macOS; Linux X11 as best-effort

**Non-Goals:**
- Replacing `WebViewShortcutRouter` for window-local shortcuts
- Media key handling (play/pause/volume)
- Shortcut recording/capture UI (application-level concern)
- Wayland global shortcut support (no stable protocol exists)

## Decisions

### D1: Service architecture — [JsExport] with policy guard

**Choice**: `IGlobalShortcutService` as `[JsExport]` with an internal policy check before each registration. Registration mutates OS state, so it uses capability bridge policy evaluation for the `GlobalShortcutRegister` operation before calling the platform provider.

**Rationale**: Unlike theme (read-only), shortcut registration has side effects (OS-level state mutation). Policy governance ensures web content cannot register arbitrary global shortcuts without host authorization.

### D2: Platform abstraction

**Choice**: `IGlobalShortcutPlatformProvider` interface with platform-specific implementations. Factory selects the correct provider at startup based on `RuntimeInformation.IsOSPlatform`.

**Rationale**: Each platform has fundamentally different APIs. A shared interface with `Register`, `Unregister`, `IsSupported` methods keeps the service layer clean.

Implementations:
- `WindowsGlobalShortcutProvider` — `RegisterHotKey` / `UnregisterHotKey` via P/Invoke, window message loop for `WM_HOTKEY`
- `MacOSGlobalShortcutProvider` — `NSEvent.addGlobalMonitorForEvents` or `CGEvent` tap
- `LinuxX11GlobalShortcutProvider` — `XGrabKey` via X11 interop (X11 only, not Wayland)
- `NullGlobalShortcutProvider` — returns unsupported for platforms without support

### D3: Shortcut model

**Choice**: `GlobalShortcutBinding` record with `Id` (string, user-defined), `Key` (enum), `Modifiers` (flags enum: Ctrl, Alt, Shift, Meta/Super). Conflict detection at registration time.

**Rationale**: User-defined ID allows JS to track registrations. Key + Modifiers model is universal across platforms. Conflict detection prevents silent failures.

### D4: Priority resolution — global vs window-local

**Choice**: When the application window is focused and a key combination matches both a global shortcut and a `WebViewShortcutRouter` binding, the window-local binding takes priority. Global shortcuts only fire when the window is NOT focused, or when no window-local binding matches.

**Rationale**: This matches Electron's behavior and user expectations — in-app shortcuts should not be intercepted by global registrations.

### D5: Lifecycle — automatic cleanup

**Choice**: All global shortcuts are automatically unregistered when the service is disposed (app shutdown). Registrations are tracked in a dictionary keyed by ID.

**Rationale**: Leaked global shortcuts after app crash are a known problem. Automatic cleanup on dispose prevents stale OS-level registrations.

## Testing Strategy

- **Contract tests**: Mock `IGlobalShortcutPlatformProvider` → test service logic (register, unregister, duplicate detection, conflict, policy integration)
- **Unit tests**: Each platform provider tested with mock OS APIs where feasible
- **Integration tests**: Register shortcut via bridge → simulate trigger → verify JS receives event

## Risks / Trade-offs

- **[Linux Wayland]** No stable global shortcut protocol → Mitigation: `NullGlobalShortcutProvider` returns unsupported; document limitation
- **[macOS permissions]** Global event monitoring requires Accessibility permission → Mitigation: detect permission status, return clear error via bridge
- **[Conflict with other apps]** Another app may already hold the hotkey → Mitigation: `Register` returns success/failure with conflict reason
- **[Platform P/Invoke complexity]** Win32/macOS interop is error-prone → Mitigation: consider wrapping existing libraries (e.g., `SharpHook` for cross-platform input) before writing raw P/Invoke
