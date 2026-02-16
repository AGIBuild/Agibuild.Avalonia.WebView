## Why

`WebViewCore` still has a low-coverage tail (94.3%) concentrated in complex branch paths, and CI logs still contain high-noise warnings (`WindowsBase` conflict warnings and xUnit analyzer warnings). In Phase 3 quality hardening, we need deterministic test evidence and warning governance so quality signals stay actionable.

## What Changes

- Add targeted coverage-governance requirements for `WebViewCore` branch hotspots, including explicit hotspot-to-test traceability and verification gates.
- Add CI warning-governance requirements for `WindowsBase` conflict warnings and xUnit analyzer warnings, with ownership, classification, and bounded suppression rules.
- Define machine-readable reporting and validation expectations to keep coverage and warning quality stable over time.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `webview-testing-harness`: add requirements for targeted `WebViewCore` hotspot branch coverage evidence and governance checks.
- `build-pipeline-resilience`: add requirements for CI warning classification/governance and auditable warning reports.

## Non-goals

- No functional runtime/API behavior changes in `WebViewCore`, adapters, or bridge contracts.
- No cross-project cleanup of all historical warnings in a single batch.
- No lowering of existing coverage thresholds or broad weakening of CI gates.

## Impact

- Affected specs: `webview-testing-harness`, `build-pipeline-resilience`.
- Expected code areas (for later implementation): unit tests around `WebViewCore`, build orchestration warning governance/tests, CI report artifacts.
- Goal alignment: advances **G4** (contract-driven testability) and aligns with ROADMAP Phase 3 quality hardening (3.4 performance & quality) plus release governance intent (3.8).
