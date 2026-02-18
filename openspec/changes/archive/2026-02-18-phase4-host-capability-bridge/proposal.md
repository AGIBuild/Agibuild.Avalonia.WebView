## Why

M4.2 solved multi-window lifecycle orchestration, but host capability access is still ad-hoc and not governed by a typed, policy-driven bridge. M4.3 is needed to expose core host powers (clipboard, file dialogs, external open, notifications) with explicit allow/deny control to satisfy Phase 4 deliverable 4.3 and G3/G4.

## What Changes

- Introduce a typed host capability bridge contract with capability-specific APIs for clipboard, file dialogs, external open, and notifications.
- Add capability authorization policy contracts that evaluate each request with explicit allow/deny semantics and deterministic failure behavior.
- Provide runtime wiring that executes capability calls through the bridge, with non-breaking opt-in behavior when not configured.
- Add contract/integration test requirements for policy enforcement, typed result semantics, and error isolation.

## Non-goals

- Full system shell parity with Electron API surface.
- OS-specific advanced integrations beyond the initial capability set.
- Reworking M4.2 lifecycle semantics or replacing existing shell policy contracts.

## Capabilities

### New Capabilities
- `webview-host-capability-bridge`: Defines typed contracts, authorization, and runtime behavior for host capabilities (clipboard, file dialogs, external open, notifications).

### Modified Capabilities
- `webview-shell-experience`: Extend shell requirements to route managed/external workflows through typed host capability bridge where applicable.
- `webview-multi-window-lifecycle`: Extend lifecycle requirements with capability bridge integration points for external-open and managed-window adjunct actions.

## Impact

- **Roadmap alignment**: Implements **Phase 4 M4.3 / Deliverable 4.3** in `openspec/ROADMAP.md`.
- **Goal alignment**: Advances **G3** through explicit capability authorization and **G4** through contract-testable typed capability behavior.
- **Affected systems**: Shell runtime contracts and orchestration, host-facing integration APIs, policy enforcement flow, and CT/IT automation coverage.
