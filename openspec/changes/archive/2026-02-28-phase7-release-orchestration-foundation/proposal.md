## Why

Phase 6 completed transition governance hardening, but Phase 7 needs a concrete release-orchestration contract that turns governance outputs into deterministic publish/no-publish decisions. Without this baseline, release quality remains distributed across loosely coupled checks and is harder to audit as one gate.

## What Changes

- Introduce a Phase 7 release-orchestration capability that defines a machine-checkable release decision gate driven by CI evidence, package validation, and governance diagnostics.
- Extend build pipeline resilience requirements with explicit release-orchestration gate ordering and failure taxonomy for publication-blocking conditions.
- Extend CI evidence contract v2 with release-decision payload requirements that summarize acceptance state and blocking reasons.
- Extend release versioning strategy requirements so tag/version progression is tied to release-orchestration acceptance rather than tag semantics alone.

## Capabilities

### New Capabilities
- `release-orchestration-gate`: Defines deterministic release decision contract (`ready`, `blocked`), required evidence sources, and machine-readable blocking diagnostics.

### Modified Capabilities
- `build-pipeline-resilience`: Add release-orchestration gate sequencing and deterministic publish-block conditions.
- `ci-evidence-contract-v2`: Add release decision summary section with blocking reason schema.
- `release-versioning-strategy`: Bind version/tag publication eligibility to release-orchestration gate output.

## Impact

- OpenSpec artifacts: new Phase 7 capability spec + delta specs for pipeline resilience, CI evidence v2, and release versioning strategy.
- Build/governance implementation (future tasks): `build/Build*.cs`, governance test suite, release evidence JSON payloads.
- Goal alignment: advances `G4` (contract-driven testability/auditability) and supports roadmap Phase 7 release-orchestration deliverables.

## Non-goals

- No runtime bridge/adapter/shell feature expansion.
- No package publication automation implementation in this proposal-only stage.
