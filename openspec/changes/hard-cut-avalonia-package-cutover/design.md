## Context

`Agibuild.Fulora.Core`, `Agibuild.Fulora.Runtime`, and adapter abstractions are already host-framework-neutral, but the public Avalonia host package identity is still `Agibuild.Fulora`. This creates product-surface ambiguity between host-neutral runtime layers and Avalonia-specific integration.

Roadmap alignment note: the active roadmap phase is Phase 8 (Bridge V2 & parity), while this change is a release/distribution identity hardening task. It is treated as a packaging-governance correction aligned with Phase 7 distribution determinism outcomes and current template adoption expectations.

Stakeholders:
- Package consumers (template users and direct NuGet users)
- Build/release governance pipeline maintainers
- Template/sample maintainers

Constraints:
- Hard cut only: no compatibility package, no forwarding package, no deprecation transition in this change.
- Preserve host-neutral architecture boundaries from the design document (contracts/runtime remain framework-neutral).

## Goals / Non-Goals

**Goals:**
- Make the primary Avalonia host package identity explicit as `Agibuild.Fulora.Avalonia`.
- Keep runtime/core/adapter neutrality unchanged and avoid cross-layer coupling regression.
- Make CI/governance deterministically fail when old package identity is reintroduced.
- Keep template and sample output consistent with the new package identity.

**Non-Goals:**
- Backward compatibility for existing `Agibuild.Fulora` package consumers.
- Runtime behavior changes in bridge, shell, SPA hosting, or adapter execution.
- New capability surface in public API beyond package identity cutover.

## Decisions

### Decision 1: Hard-cut package identity to `Agibuild.Fulora.Avalonia`
- Chosen: rename the primary Avalonia host package identity directly with no compatibility package.
- Rationale: matches explicit host-layer ownership and prevents ambiguous dependency semantics.
- Alternatives considered:
  - Keep `Agibuild.Fulora` and add documentation only: rejected (identity ambiguity remains).
  - Keep `Agibuild.Fulora` as metapackage: rejected per hard-cut requirement and adds ongoing governance complexity.

### Decision 2: Keep runtime/core package and namespace contracts stable
- Chosen: no identity changes for `Core/Runtime/Adapters` package family and no runtime contract changes.
- Rationale: preserves host-neutral design invariants and avoids unnecessary API churn.
- Alternatives considered:
  - Rename broad package family in one wave: rejected (high blast radius without architectural benefit).

### Decision 3: Enforce identity via deterministic governance checks
- Chosen: update canonical package-set and release metadata assertions to require `Agibuild.Fulora.Avalonia` as primary host package.
- Rationale: avoids drift and prevents accidental reintroduction of legacy package identity in CI/publish flows.
- Alternatives considered:
  - Manual checklist validation: rejected (non-deterministic and not enforceable at scale).

## Risks / Trade-offs

- [Consumer breakage on package restore] → Mitigation: hard-fail governance and template references in same change so repository surfaces stay coherent.
- [Missed reference in templates/samples/tests] → Mitigation: add deterministic assertions in template E2E and governance scans for package identity tokens.
- [Release pipeline drift on canonical package set] → Mitigation: update distribution-readiness spec assertions and machine-readable evidence checks together.
- [Perceived roadmap scope drift] → Mitigation: constrain change strictly to identity/governance wiring; no runtime feature work.

## Migration Plan

1. Update host package identity in the Avalonia host project/package metadata and packaging targets.
2. Update build/release governance expected package IDs and canonical set checks.
3. Update template/sample/NuGet integration test references to `Agibuild.Fulora.Avalonia`.
4. Update contract/governance tests asserting package identity and distribution readiness.
5. Run deterministic verification (`openspec validate --all --strict`, targeted unit/integration governance tests, package validation).

Rollback strategy:
- Revert this change set in one commit scope if publication blocking regressions appear before release cut.

## Testing Strategy

- CT: governance/unit tests that assert canonical package identity, package-set completeness, and template dependency tokens.
- IT: template E2E and NuGet-package integration tests validating package restore/use path with `Agibuild.Fulora.Avalonia`.
- Pipeline checks: packaging and release-distribution validation tasks must produce deterministic pass/fail evidence for the new identity.

## Open Questions

- Should the host project file/folder name be physically renamed in this change, or only `PackageId`/build identity first?
- Should docs site project-source list switch immediately to new project path in the same cut, or as a follow-up documentation-only change?
