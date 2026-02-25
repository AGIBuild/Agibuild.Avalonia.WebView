## Why

System-integration diagnostics are emitted, but consumers still reconstruct export payloads ad hoc. Plan C requires a stable, machine-readable export protocol plus long-term automation evidence to improve AI-agent operability and CI regression comparability.

## What Changes

- Add a first-class diagnostic export record contract for host capability diagnostics.
- Add deterministic conversion from runtime diagnostic events to export records with stable field mapping.
- Extend tests to validate deny/failure taxonomy in exported records.
- Add a dedicated automation scenario/capability lane entry for diagnostic export regression.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `webview-host-capability-bridge`: add structured diagnostic export contract and deterministic mapper.
- `shell-system-integration`: enforce machine-readable deny/failure taxonomy assertions through export protocol tests.
- `runtime-automation-validation`: include long-term regression scenario for diagnostic export protocol.
- `shell-production-validation`: include production matrix capability entry for diagnostic export checks.

## Impact

- Runtime shell code gains export DTO/mapping API.
- Unit/integration/governance tests gain new assertions for export payload structure and taxonomy.
- Runtime/production manifests gain new scenario/capability ids for long-run evidence.

## Non-goals

- No external telemetry backend integration.
- No change to existing authorization semantics.
- No new host capabilities outside diagnostics export.
