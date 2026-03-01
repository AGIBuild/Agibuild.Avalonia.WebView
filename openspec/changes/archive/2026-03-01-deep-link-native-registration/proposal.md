## Why

Phase 8 has delivered in-process activation orchestration, but real products still lack the platform-native deep-link registration path required to enter the app from OS/browser contexts.  
Adding this closes the last functional gap from contract-only orchestration to production-ready activation flow, directly advancing ROADMAP Phase 8 M8.5 (platform parity closure) and supporting G2/G4 through deterministic host lifecycle routing and testable contracts.

## What Changes

- Introduce a typed host-facing deep-link registration contract for platform-native URI scheme activation.
- Add deterministic activation ingest pipeline from native entrypoint -> registration validation -> shell activation coordinator.
- Add dedup/idempotency rules for repeated activation delivery during cold start and single-instance forward.
- Add policy hooks for accepted scheme/host/path constraints before activation dispatch.
- Add contract and unit/integration tests for success, deny, malformed payload, and duplicate activation paths.

## Capabilities

### New Capabilities
- `shell-deep-link-registration`: Typed, deterministic contract for app-scoped URI scheme registration and native activation ingestion.

### Modified Capabilities
- `shell-activation-orchestration`: Extend orchestration from in-process forwarding to include OS-triggered activation ingestion and idempotent dispatch semantics.
- `webview-compatibility-matrix`: Add deep-link registration support matrix and parity evidence expectations by platform.

## Impact

- Runtime shell activation pipeline under `src/Agibuild.Fulora.Runtime/Shell/` (registration contract, validation, dispatch, diagnostics).
- Platform adapter integration points for activation handoff (Windows/macOS/Linux/Android/iOS entrypath mapping).
- Policy/evidence flow in governance and compatibility assertions for parity validation.
- Tests in unit/integration suites covering deterministic activation behavior and duplicate/invalid paths.

## Non-goals

- No app-installer authoring or packaging automation for OS-level protocol registration.
- No universal-link/app-link trust chain implementation (HTTP association files).
- No cross-process transport redesign beyond current activation coordinator contract.
