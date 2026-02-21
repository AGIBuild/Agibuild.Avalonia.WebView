## Why

Current shell diagnostics and metadata boundary governance are deterministic but still leave contract ambiguity in two places: configurable metadata budget policy and profile-hash canonical format. Closing these gaps now improves auditability and keeps CI/agent reasoning stable as Phase 5 transitions into next-stage governance hardening.

## What Changes

- Introduce configurable inbound metadata aggregate budget with bounded min/max contract and deterministic deny semantics when exceeded.
- Standardize profile revision diagnostics contract with canonical `profileHash` format and normalized `profileVersion` semantics.
- Refine app-shell template governance markers to reflect contract v2 guidance for ShowAbout opt-in and revision-aware diagnostics.
- Extend CT/IT/governance matrices and evidence for new boundary and diagnostic normalization branches.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `shell-system-integration`: metadata boundary requirement upgraded from fixed aggregate budget to bounded configurable contract with deterministic deny behavior.
- `webview-host-capability-bridge`: metadata validation requirement updated to include configurable aggregate budget bounds and stable validation outcomes.
- `webview-shell-experience`: federated pruning diagnostics requirement updated with canonical profile revision field semantics.
- `template-shell-presets`: governance marker requirements updated to reflect contract v2 snippet and revision-aware diagnostics path.

## Non-goals

- No expansion of capability surface or system actions beyond current typed model.
- No fallback/dual-path compatibility branch for legacy diagnostic formats.
- No host framework/platform expansion or Electron API parity work.
- No changes to policy-first execution order (`profile -> policy -> mutation`).

## Impact

- Goal alignment: reinforces **G3** (explicit capability governance) and **G4** (contract-verifiable deterministic behavior), with DX/governance value for **E1/E2**.
- Roadmap alignment: follows Phase 5 deliverables **5.3**, **5.4**, **5.5** as hardening continuity for observability/template/governance contracts.
- Affected areas: runtime bridge validation options, shell diagnostic normalization, template markers, CT/IT/governance matrix artifacts.
