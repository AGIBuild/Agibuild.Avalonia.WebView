## Why

Phase 5 closed the core web-first baseline, but three governance gaps remain from the archived follow-up checklist: global tray metadata budget, federated pruning audit identity, and template-level ShowAbout opt-in snippet clarity. Closing them now strengthens policy-first determinism and machine-checkable diagnostics for AI-agent and CI workflows.

## What Changes

- Add a global payload size budget for inbound system-integration metadata envelope, in addition to existing key/value and entry-count constraints.
- Extend federated menu-pruning diagnostics with profile version/hash fields so pruning decisions are traceable across policy revisions.
- Update app-shell template markers to include a minimal ShowAbout allowlist opt-in snippet while keeping deny-by-default behavior.
- Expand CT/IT/governance evidence to cover budget boundary, profile-hash diagnostics, and template opt-in marker path.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `shell-system-integration`: define global metadata budget semantics and deterministic boundary outcomes.
- `webview-host-capability-bridge`: enforce metadata total-size budget before dispatch and emit boundary-stage diagnostics.
- `webview-shell-experience`: include federated pruning diagnostic fields for profile version/hash attribution.
- `template-shell-presets`: require explicit ShowAbout opt-in snippet marker without enabling default allow path.

## Non-goals

- No expansion of system action surface beyond existing typed actions.
- No fallback or dual-path compatibility routing.
- No bundled-browser full API parity work or new host framework targets.

## Impact

- Goal alignment: advances **G3** (policy-first, explicit capability boundary) and **G4** (contract-verifiable deterministic outcomes), while improving **E1/E2** template and diagnostics clarity.
- Roadmap alignment: Phase 5 follow-up hardening for deliverables **5.3** (agent-friendly observability), **5.4** (web-first template flow), and **5.5** (governance evidence continuity).
- Affected areas: runtime shell/bridge boundary validation, shell pruning diagnostic contracts, app-shell template markers, CT/IT/governance matrix evidence.
