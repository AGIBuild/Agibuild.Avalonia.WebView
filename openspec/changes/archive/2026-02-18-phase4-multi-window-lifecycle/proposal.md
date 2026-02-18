## Why

Phase 4 M4.2 requires first-class multi-window lifecycle semantics on top of the M4.1 shell policy base so host apps can build Electron-like workflows without duplicating fragile window orchestration code. This closes a major gap between "embedded WebView" and "application shell platform" while preserving G3 and G4.

## What Changes

- Introduce a runtime multi-window lifecycle model covering window creation, routing strategy, ownership, activation, close, and deterministic teardown.
- Add host-facing strategy contracts for `NewWindowRequested` outcomes: in-place, open dialog window, external browser, and host-delegate.
- Define stable window identity and lifecycle events so navigation/session decisions can be correlated per window scope.
- Add contract/integration test requirements for multi-window routing correctness, lifecycle ordering, and no-leak teardown behavior.

## Non-goals

- Delivering the full typed host capability bridge (clipboard/file dialogs/notifications), which belongs to M4.3.
- Building template presets and migration helpers, which belong to M4.5.
- Replacing platform-native window managers or exposing all Chromium window APIs.

## Capabilities

### New Capabilities
- `webview-multi-window-lifecycle`: Defines cross-platform contracts for shell window strategies, lifecycle state transitions, and deterministic teardown semantics.

### Modified Capabilities
- `webview-shell-experience`: Extend shell policy requirements to integrate multi-window strategy decisions and window-scoped routing behavior.
- `shell-session-policy`: Extend session policy requirements to define how session scope is resolved and reused across multi-window relationships.

## Impact

- **Roadmap alignment**: Implements **Phase 4 M4.2 / Deliverable 4.2** in `openspec/ROADMAP.md`.
- **Goal alignment**: Advances **G4** through contract-first, mock-testable lifecycle semantics and reinforces **G3** through explicit window/session governance.
- **Affected systems**: Runtime shell orchestration layer, WebViewCore new-window handling integration points, session policy composition path, and CT/IT automation lanes for lifecycle stress validation.
