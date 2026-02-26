## Context

Phase 5 is marked complete with strong automation evidence, but governance quality is still coupled to textual assertions in `AutomationLaneGovernanceTests`. This increases drift risk when build orchestration or evidence artifacts evolve. The change aligns with G4 (Contract-Driven Testability) and G3 (Secure by Default governance posture) by making CI quality gates machine-semantic, versioned, and auditable.

ROADMAP currently ends at Phase 5 completion and does not yet define formal Phase 6 deliverable numbers. This design is therefore a transition hardening slice that protects existing Phase 5 exit criteria while preparing the observability/governance foundation already implied by archived `phase6-observability-*` changes.

## Goals / Non-Goals

**Goals:**
- Introduce semantic governance assertions that validate structure/invariants instead of brittle string snippets.
- Standardize a versioned release evidence contract (v2) with provenance metadata in `CiPublish`.
- Extend package distribution governance for `@agibuild/bridge` with package-manager and Node LTS parity checks.
- Keep all checks testable via CT/IT + machine-readable artifacts.

**Non-Goals:**
- Add unrelated runtime features or alter bridge/policy execution semantics.
- Introduce compatibility fallback branches for legacy evidence schemas.
- Provide migration or compatibility adapters for legacy governance/evidence structures.
- Redesign adapter architecture or platform runtime contracts.

## Decisions

### Decision 1: Governance assertion strategy
- **Option A (selected):** semantic assertions over parsed JSON/target graph invariants.
- **Option B:** keep string-contains assertions with stricter naming conventions.
- **Rationale:** Option A lowers false positives and refactor coupling while preserving deterministic gates.

### Decision 2: Evidence schema evolution
- **Option A (selected):** introduce explicit `schemaVersion` and required provenance fields in v2.
- **Option B:** infer version implicitly from file name or pipeline target.
- **Rationale:** explicit versioning is auditable, easier for CI agents, and safer for contract evolution.

### Decision 3: Bridge distribution validation depth
- **Option A (selected):** add npm/pnpm/yarn + Node LTS matrix smoke checks.
- **Option B:** npm-only checks with best-effort manual parity.
- **Rationale:** parity is required for release-grade DX claims and reduces ecosystem surprises.

### Testing strategy
- CT: governance/unit tests verify schema parsing, invariant checks, and failure diagnostics.
- IT/automation: runtime-critical-path evidence remains executable and mapped to CI context.
- Build verification: deterministic `CiPublish` artifacts validated against v2 contract.

## Risks / Trade-offs

- **[Risk] Initial cutover noise from new v2 constraints** → **Mitigation:** direct cutover with deterministic error messages and strict validation.
- **[Risk] Over-constraining CI can block unrelated work** → **Mitigation:** scope gates to governed artifacts and keep diagnostics actionable.
- **[Risk] Multi-manager matrix increases CI time** → **Mitigation:** lightweight smoke subset and cache-aware execution.

## Rollout Plan (Direct Cutover)

1. Add semantic assertion helpers and update governance tests.
2. Introduce v2 evidence schema and update `CiPublish` artifact producers.
3. Switch governed evidence validation to v2 without compatibility layer.
4. Enable bridge package parity checks in `CiPublish` release lane.
5. Remove obsolete textual-coupling checks after semantic parity is proven.

## Confirmed Constraints

- v2 evidence SHALL NOT require content hash fields for all upstream artifacts.
- Package-manager parity and Node LTS governance SHALL run only in `CiPublish`.
- No migration or legacy-compatibility branch SHALL be introduced.
