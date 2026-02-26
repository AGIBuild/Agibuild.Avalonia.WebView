## Why

Current shell diagnostics are structured but lack an explicit schema version in runtime payloads, making long-term machine consumers brittle as contracts evolve. We need an explicit versioned contract and a single cross-lane assertion entrypoint now to preserve deterministic observability quality as we transition from Phase 5 completion into Phase 6 preheat.

## What Changes

- Add explicit `DiagnosticSchemaVersion` to host capability and session profile diagnostic payload contracts.
- Standardize schema version emission at runtime event construction points.
- Introduce one shared testing assertion helper used by unit/integration/governance lanes for diagnostic schema invariants.
- Update CT/IT/governance tests to assert schema version via the shared helper instead of duplicating ad-hoc assertions.

## Capabilities

### New Capabilities
- `observability-diagnostic-schema-versioning`: Versioned runtime diagnostic contract and unified cross-lane assertion mechanism.

### Modified Capabilities
- `webview-host-capability-bridge`: Host capability diagnostics gain explicit schema-version field with deterministic emission.
- `webview-session-permission-profiles`: Session/profile diagnostics gain explicit schema-version field with deterministic emission.

## Non-goals

- Redesign diagnostic payload semantics beyond adding schema version and assertion unification.
- Introduce backward-compatibility fallback branches for old schema readers.
- Change capability policy execution order or authorization outcomes.

## Impact

- Runtime: `WebViewHostCapabilityDiagnosticEventArgs` and `WebViewSessionPermissionProfileDiagnosticEventArgs`.
- Tests: shared helper in `Agibuild.Fulora.Testing`, plus CT/IT/governance test updates.
- Governance: stronger machine-checkable observability baseline aligned with G4 and Phase 5 M5.3/M5.5 evidence quality.
