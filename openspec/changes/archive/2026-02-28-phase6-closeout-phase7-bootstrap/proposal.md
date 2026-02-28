## Why

Phase 6 governance work is complete, but repository-level phase markers and closeout evidence are still anchored to the previous transition baseline. We need to close Phase 6 deterministically and bootstrap Phase 7 with machine-checkable continuity so CI governance remains stable for future roadmap increments.

## What Changes

- Update roadmap transition state from `phase5 -> phase6` to `phase6 -> phase7`, including Phase 6 completion status and Phase 7 active framing.
- Align closeout snapshot transition constants and governance tests with the new completed/active phase pair and refreshed closeout evidence mapping.
- Eliminate archived `TBD` purpose placeholders in specs to keep strict validation artifacts release-ready and auditable.
- Run strict validation and automation evidence commands to confirm no regression in transition-gate governance and spec-governance baselines.

## Capabilities

### New Capabilities
- `roadmap-phase-transition-management`: Defines deterministic requirements for machine-checkable roadmap phase rollover and closeout evidence continuity.

### Modified Capabilities
- `continuous-transition-gate`: Tighten continuity expectations for transition rollover updates between adjacent roadmap phases.
- `openspec-strict-validation-governance`: Require archived/spec baseline purpose fields to remain finalized (no placeholder text).

## Impact

- Affected docs/specs: `openspec/ROADMAP.md`, selected `openspec/specs/*/spec.md` purpose sections, new/updated change delta specs.
- Affected governance implementation: `build/Build.Governance.cs`.
- Affected regression tests: `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`.
- Goal/roadmap alignment: supports Goal `G4` (contract-driven testability) and Phase 6 deliverables `M6.2` + `M6.3` while opening Phase 7 governance baseline.

## Non-goals

- No new runtime feature delivery in bridge, adapter, or template behavior.
- No change to security policy semantics or host capability execution contracts.
