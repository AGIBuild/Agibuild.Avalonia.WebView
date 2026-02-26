## Why

The previous system-integration increment closed the bidirectional typed flow, but three contract-level decisions remain unresolved: `ShowAbout` whitelist policy, tray payload boundary, and menu pruning federation with permission profiles. Resolving them now removes policy ambiguity before the next runtime hardening cycle and preserves deterministic governance guarantees.

## What Changes

- Define explicit whitelist semantics for `ShowAbout` as a governed system action with deterministic allow/deny/failure outcomes.
- Define tray inbound event contract boundary: canonical typed semantic fields plus constrained platform metadata envelope.
- Add federated decision model for menu pruning that combines shell policy context and session permission profile decisions.
- Extend diagnostics and CT/IT/governance matrices for the three paths above to keep outcomes machine-checkable.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `shell-system-integration`: add requirements for `ShowAbout` whitelist behavior, tray metadata envelope constraints, and profile-aware menu pruning evaluation.
- `webview-host-capability-bridge`: add typed inbound payload contract and deterministic diagnostics requirements for semantic fields + metadata envelope.
- `webview-shell-experience`: add deterministic policy evaluation order for pruning federation (profile decision + shell policy composition).
- `template-shell-presets`: add app-shell marker requirements that demonstrate federated pruning and bounded tray metadata handling.

## Non-goals

- No bundled-browser full API parity expansion.
- No new host framework targets (WPF/WinForms/MAUI).
- No fallback/dual-path compatibility routing.
- No broad capability surface expansion beyond the three scoped decisions.

## Impact

- Goal alignment: advances **G3** (policy-first, explicit allowlist) and **G4** (deterministic, contract-testable behavior), reinforcing **E1/E2** template/governance reliability.
- Roadmap alignment: post-Phase-5 hardening for deliverables `5.2` (policy-first runtime), `5.3` (agent-friendly diagnostics), and `5.5` (production governance evidence continuity).
- Affected areas: runtime shell governance pipeline, typed bridge contracts, template app-shell preset markers, CT/IT/governance matrix artifacts.
