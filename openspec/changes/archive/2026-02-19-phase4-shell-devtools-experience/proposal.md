## Why

Current shell policies cover new-window/download/permission/session, but DevTools runtime behavior is not governed by shell policy contracts. To complete the practical "Electron-like shell experience" path, DevTools open/close/query must be policy-controlled and auditable under the same shell governance model.

## What Changes

- Extend shell runtime options with DevTools policy contract and deterministic decision context.
- Add shell-facing DevTools operations (`open`, `close`, `query`) that execute through policy-first governance and explicit failure reporting.
- Add unit and automation integration coverage for allow/deny and domain-isolation behavior.

## Non-goals

- Keyboard shortcut wiring or global hotkey registration.
- Platform-specific DevTools UI customization.
- Any legacy fallback compatibility branch for old shell behavior.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `webview-shell-experience`: Extend shell policy domains to include DevTools governance with deterministic allow/deny semantics and explicit failure isolation.

## Impact

- **Roadmap alignment**: This is a Phase 4 shell-completion enhancement after M4.6 to close remaining DevTools governance gap in the original application-shell capability target.
- **Goal alignment**: Advances **E2 (Dev Tooling)** and **G4 (Contract-Driven Testability)**.
- **Affected systems**: shell runtime policy orchestration (`WebViewShellExperience`) and automation/unit governance tests.
