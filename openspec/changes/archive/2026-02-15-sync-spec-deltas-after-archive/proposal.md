## Why

The previous archive for `refactor-test-layers-pipeline-and-api-boundaries` required `--skip-specs` because spec sync failed on `webview-contract-semantics-v1` normative validation (`Requirement must contain SHALL or MUST keyword`). This blocks a clean archive+sync closure in Phase 3 GA governance work.

## What Changes

- Add a focused delta for `webview-contract-semantics-v1` to normalize the requirement wording that fails normative validation.
- Make the target requirement text explicitly normative at the requirement body level (not only in bullets).
- Define a verification loop that proves the change can be archived with spec sync (without `--skip-specs`) and leaves no active change residue.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `webview-contract-semantics-v1`: Tighten normative wording in the `NavigateToStringAsync baseUrl semantics` requirement so spec sync/archive validation passes deterministically.

## Non-goals

- No runtime or API behavior changes in `WebViewCore`, `WebView`, adapters, or tests.
- No broad rewrite of historical legacy specs that currently fail unrelated structure checks.
- No CI/build pipeline restructuring.

## Impact

- Affected docs/specs: `openspec/specs/webview-contract-semantics-v1/spec.md` (via delta sync).
- Affected process: OpenSpec archive/sync path for this targeted change.
- Alignment: Supports Phase 3 deliverable `3.8` (API surface review + breaking-change/governance readiness), and reinforces G4 contract-driven testability/governance by keeping spec language machine-verifiable.
