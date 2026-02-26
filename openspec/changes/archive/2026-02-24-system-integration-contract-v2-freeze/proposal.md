## Why

Phase 5 has shipped the system-integration v2 baseline, but three contract hardening gaps remain: reserved metadata key governance, timestamp wire normalization, and template-level ShowAbout enablement strategy. Closing these gaps now improves G3 (secure-by-default) and G4 (machine-checkable testability) before Phase 5 evidence freeze.

## What Changes

- Introduce a reserved-key registry under `platform.*` for tray/system-integration metadata and enforce deterministic deny for registry violations.
- Normalize inbound `OccurredAtUtc` to canonical UTC millisecond precision before dispatch and add deterministic serialization assertions.
- Replace template compile-time `ShowAbout` switch with explicit runtime opt-in toggle strategy while keeping default deny.
- Extend governance and CT/IT evidence to cover registry, timestamp canonicalization, and template toggle markers.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `shell-system-integration`: tighten v2 payload governance with reserved key registry and canonical timestamp contract.
- `webview-host-capability-bridge`: enforce reserved metadata key policy and timestamp normalization boundary behavior.
- `webview-shell-experience`: keep deterministic schema-first behavior with canonicalized inbound event semantics.
- `template-shell-presets`: expose explicit ShowAbout runtime toggle markers with default deny and no baseline bypass.

## Impact

- Affected runtime code: `WebViewHostCapabilityBridge` validation/normalization path.
- Affected template code: app-shell preset ShowAbout toggle and inbound metadata usage markers.
- Affected tests: unit/integration/governance matrices and contract branches.
- Roadmap impact: Phase 5 hardening/evidence quality uplift without expanding bundled-browser parity scope.

## Non-goals

- No bundled-browser full API parity expansion.
- No dual-path fallback for old payload contracts.
- No new host capability surface beyond existing v2 contracts.
