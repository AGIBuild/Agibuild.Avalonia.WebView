## Why

`openspec validate --all --strict` was failing across a large set of legacy spec files, which weakened contract governance and made CI-level quality signals noisy. We need a deterministic strict-validation baseline now to keep Phase 5 governance evidence reliable and to preserve G4 (Contract-Driven Testability) at the specification layer.

## What Changes

- Normalize legacy spec structure to strict format (`## Purpose`, `## Requirements`, normative requirement text, and scenario blocks).
- Repair invalid normative wording by ensuring each requirement is machine-parseable with `SHALL`/`MUST`.
- Backfill missing scenarios on requirements that previously had none.
- Establish a repeatable strict-validation gate baseline (`openspec validate --all --strict` must pass).

## Capabilities

### New Capabilities
- `openspec-strict-validation-governance`: Defines repository-level governance rules for strict OpenSpec schema compliance and deterministic validation outcomes.

### Modified Capabilities
- None.

## Non-goals

- Introduce new runtime product features.
- Change bridge/shell execution semantics beyond spec-format compliance.
- Add compatibility paths for legacy spec formatting.

## Impact

- Affected systems: OpenSpec specification corpus under `openspec/specs/*`.
- Affected workflow: proposal/spec/design/tasks archival completeness for strict-governance cleanup.
- Verification impact: strict validation becomes green across all repository specs and can be used as a stable CI quality signal.
