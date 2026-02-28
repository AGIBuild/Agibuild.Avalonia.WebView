## Context

ROADMAP Phase 7 M7.3 calls for adoption-oriented evidence in addition to packaging and governance pass/fail signals. Current release evidence emphasizes test/coverage/governance contracts, but does not yet provide a deterministic adoption-readiness view across docs, templates, and runtime critical-path confidence.

This design supports PROJECT goals **G4 (Contract-Driven Testability)** and experience goals **E1 (Project Template)** and **E2 (Dev Tooling)** by formalizing adoption confidence as machine-auditable CI evidence.

## Goals / Non-Goals

**Goals:**
- Define a deterministic adoption-readiness capability with KPI-like evidence semantics.
- Emit structured adoption readiness data in release evidence (`ci-evidence-contract-v2`).
- Define release-orchestration policy for adoption signals (blocking vs advisory classification).
- Keep adoption governance compatible with existing CI lane contracts and deterministic diagnostics.

**Non-Goals:**
- Building product analytics/telemetry ingestion systems.
- Rewriting sample/template architecture in this change.
- Turning all adoption indicators into hard blockers from day one.

## Decisions

### Decision 1: Create dedicated `adoption-readiness-signals` capability
- **Why:** Adoption evidence spans docs/templates/runtime and needs unified semantics.
- **Alternative considered:** Spread adoption checks into existing capabilities without a dedicated contract.
- **Trade-off:** New spec surface, but much better clarity and ownership.

### Decision 2: Classify adoption findings into blocking and advisory tiers
- **Why:** Not all adoption regressions should stop preview flow immediately.
- **Alternative considered:** Fully blocking or fully advisory policy.
- **Trade-off:** Slightly more policy complexity, but better operational flexibility.

### Decision 3: Publish adoption readiness in release evidence v2
- **Why:** Operators and auditors need machine-readable readiness state without parsing raw logs.
- **Alternative considered:** Console-only diagnostics.
- **Trade-off:** Additional payload schema maintenance, but deterministic auditability.

### Decision 4: Keep adoption checks aligned with existing CI governance architecture
- **Why:** Reuse current report-driven governance pattern and preserve lane consistency assumptions.
- **Alternative considered:** Introduce a separate out-of-band verification pipeline.
- **Trade-off:** Less architectural novelty, but lower integration and maintenance risk.

## Risks / Trade-offs

- **[Risk] Advisory signals may be ignored over time** → **Mitigation:** Require explicit advisory counts and category mapping in release evidence.
- **[Risk] Overly strict blocking criteria slows phase throughput** → **Mitigation:** Start with minimal blocking set and review thresholds at each phase checkpoint.
- **[Risk] KPI definitions become ambiguous** → **Mitigation:** Require stable field schema and deterministic scenario wording in specs.
- **[Risk] Duplicate checks across capabilities** → **Mitigation:** Define ownership boundaries between adoption capability and existing governance capabilities.

## Testing Strategy

- Add governance tests for:
  - adoption readiness report schema and deterministic field presence;
  - release evidence v2 adoption section contract;
  - release-orchestration policy behavior for blocking vs advisory adoption findings.
- Validate OpenSpec integrity using `openspec validate --all --strict`.
- Validate pipeline behavior via `nuke Test` and target-level governance execution in `CiPublish`.

## Migration Plan

1. Add new adoption capability spec and delta specs for modified capabilities.
2. Wire adoption evidence emission into governed build lane flow.
3. Extend release evidence and decision payload validation tests.
4. Calibrate blocking/advisory mapping and archive with verification evidence.

## Open Questions

- Which adoption indicators become hard blockers in stable lane vs advisory in preview lane?
- Should adoption readiness include trend windows (N recent runs) or only current-run deterministic status?
