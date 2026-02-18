## Why

Phase 4 defines "Application Shell Capabilities" as the next product step, but the current shell support is still minimal and not yet a complete policy foundation. We need a unified, opt-in shell policy layer now to unlock M4.2+ capabilities without fragmenting behavior across host apps.

## What Changes

- Expand shell policy contracts from basic hooks to a structured policy model covering new-window routing, download governance, permission governance, and session scope.
- Define deterministic runtime semantics for policy execution order, fallback behavior, and failure handling so host apps get consistent cross-platform outcomes.
- Add contract/integration automation requirements for shell policy flows (including stress-oriented scenarios), aligned with Phase 4 M4.1.

## Non-goals

- Building full multi-window orchestration primitives (M4.2 scope).
- Introducing full host capability bridge APIs (clipboard/file dialog/notifications; M4.3 scope).
- Changing baseline WebView behavior when shell policy is not enabled.

## Capabilities

### New Capabilities
- `shell-session-policy`: Defines session scope/isolation policy contracts for shell-level governance and future multi-window composition.

### Modified Capabilities
- `webview-shell-experience`: Extend requirements from basic handlers to a complete, deterministic shell policy foundation (new-window/download/permission lifecycle semantics, fallback and error behavior, testability guarantees).

## Impact

- **Roadmap alignment**: Directly implements **Phase 4 M4.1 / Deliverable 4.1** in `openspec/ROADMAP.md`.
- **Goal alignment**: Advances **G3** (secure-by-default policy control) and **G4** (contract-driven testability).
- **Affected systems**: Runtime shell policy contracts/wiring, existing shell spec baseline, contract/integration automation lanes, and template-facing integration points for later Phase 4 milestones.
