## Why

NuGet package smoke runs a real WebView2 lifecycle and currently treats Chromium teardown errors (e.g. `Failed to unregister class Chrome_WidgetWin_0`) as a lane failure. We need deterministic Attach/Detach/exit behavior to meet Phase 3 GA readiness and keep CI signal stable.

## What Changes

- Make Windows WebView2 teardown deterministic and verifiable in automation (no Chromium teardown errors, no deadlocks, no leaked window subclassing/state).
- Add a small “shell experience” layer to standardize desktop-grade host behaviors around new windows, downloads, permissions, and DevTools without expanding Core contracts unnecessarily.
- Expand integration/contract tests so lifecycle regressions are caught before packaging.

## Non-goals

- Bundling Chromium or Node.js, or pursuing full bundled-browser API parity (per PROJECT.md non-goals).
- Implementing OS-wide shell features (tray, global shortcuts, auto-update) inside this library; those belong in app hosts/templates.
- Redesigning the bridge or SPA hosting stacks (G1/G2 are out of scope unless required to fix teardown determinism).

## Capabilities

### New Capabilities
- `webview2-teardown-stability`: Deterministic WebView2 teardown semantics and acceptance criteria for Windows (automation-friendly, no Chromium teardown errors).
- `webview-shell-experience`: Optional runtime helpers/policies to handle new-window requests and polish download/permission/DevTools behaviors in a consistent, desktop-grade way.

### Modified Capabilities
<!-- None (initially). -->

## Impact

- **Goals/Roadmap alignment**: Supports Phase 3 “Polish & GA” readiness and resilience (quality gates for runtime automation), aligned with **G4 (Contract-Driven Testability)** and **E2 (Dev Tooling)** by making lifecycle correctness observable and testable.
- **Affected areas**: `Agibuild.Avalonia.WebView.Adapters.Windows` teardown path (`Detach` / WebView2 controller/environment cleanup, Win32 subclass restore), runtime smoke automation, and any new runtime helper APIs (non-breaking, opt-in).
