## Context

The project is in Phase 3 (Polish & GA). Contract tests are strong, but recent incidents showed that contract-pass does not always imply runtime-pass for platform adapters and package-consumption workflows. We need a deterministic, auditable path from mock confidence to runtime confidence and from local pass to CI pass.

This design aligns with:
- **PROJECT.md**: **G4** (Contract-Driven Testability), plus quality expectations of secure/testable architecture.
- **ROADMAP.md Phase 3**: deliverables **3.5** (platform smoke validation quality) and **3.8** (API/breaking-change audit discipline).
- **docs/agibuild_webview_design_doc.md**: contract-first layering, runtime as the single semantic owner, deterministic testing.

## Goals / Non-Goals

**Goals:**
- Establish a strict two-lane automation model: `ContractAutomation` (mock adapter) and `RuntimeAutomation` (real adapter/runtime integration).
- Make package smoke/build pipeline behavior deterministic and diagnosable under CI variance.
- Close remaining async-boundary governance gaps (blocking waits, global option coupling, reflection-only test seams) with explicit rules and checks.
- Keep verification evidence explicit so release decisions can rely on measurable confidence levels.

**Non-Goals:**
- Introducing new end-user WebView features.
- Replacing Nuke or changing CI vendor.
- Rewriting all existing tests; only targeted restructuring and governance hardening are included.

## Decisions

### D1. Two-lane automation taxonomy with explicit CI gates
- **Decision**: Split automation expectations into:
  - `ContractAutomation`: mock-driven determinism and semantic invariants.
  - `RuntimeAutomation`: real adapter/runtime checks for platform-critical behavior.
- **Why**: A single “all green” result hides whether failures are contract issues, adapter/runtime issues, or pipeline/environment issues.
- **Alternative considered**:
  - Keep a unified suite only: lower maintenance, but poor failure attribution and weaker release confidence.

### D2. Runtime critical-path matrix is required, not optional
- **Decision**: Define a minimal required runtime matrix (Windows mandatory; Linux/macOS lanes when available in CI) for async-boundary paths, lifecycle transitions, and package-consumption smoke.
- **Why**: Prevent regressions where mock tests pass but runtime thread/lifecycle behavior diverges.
- **Alternative considered**:
  - Nightly-only runtime checks: faster PR cycle but delays detection and increases rollback cost.

### D3. Pipeline resilience policy for NuGet/package smoke
- **Decision**: Standardize cache root discovery, classify transient packaging failures, and allow bounded retries only for classified transient categories.
- **Why**: Improves reproducibility while avoiding blind retries that can hide deterministic defects.
- **Alternative considered**:
  - Unconditional retry: simpler but can mask real defects and increase CI latency.

### D4. Boundary governance extends to build/test orchestration
- **Decision**: Extend blocking-wait governance from `src/` into selected build/test orchestration paths where async boundaries can still deadlock or stall pipelines.
- **Why**: Real regressions appeared outside production runtime code.
- **Alternative considered**:
  - Keep governance in runtime code only: misses high-impact CI orchestration waits.

### D5. Reduce reflection-only lifecycle test seams
- **Decision**: Introduce narrowly scoped test hooks/facades for lifecycle wiring assertions to reduce fragile private-reflection usage.
- **Why**: Reflection-heavy tests are brittle and block safe refactors.
- **Alternative considered**:
  - Keep private reflection indefinitely: no production changes needed, but high long-term maintenance risk.

## Risks / Trade-offs

- **[Risk] Runtime automation increases CI time** → **Mitigation**: Keep runtime suite focused on critical-path scenarios and run broader matrix on scheduled/nightly jobs.
- **[Risk] New governance checks create short-term friction** → **Mitigation**: provide clear failure diagnostics and owner/rationale templates.
- **[Risk] Test-hook exposure could leak into production API** → **Mitigation**: internal-only hooks with explicit test assembly visibility and zero public-surface expansion.

## Migration Plan

1. Add taxonomy and reporting labels to current automation suites without moving behavior first.
2. Move/refactor tests into lane-specific folders and CI targets.
3. Introduce pipeline resilience and transient classification in build targets.
4. Add governance tests for orchestration blocking waits and boundary closure checks.
5. Enforce gate policy (contract + runtime + package smoke) for release branches.

Rollback strategy: keep previous target aliases for one cycle; revert gate strictness if critical CI instability appears.

## Testing Strategy

- **CT (mock lane)**: deterministic semantic checks, no sleeps, no platform dependencies.
- **IT/Automation (runtime lane)**: off-thread marshaling, lifecycle attach/detach/dispose, option isolation, package-consumption smoke.
- **Governance tests**: blocking-wait allowlist (production + orchestration), async-boundary audit checks, CI report schema validation.

## Open Questions

- Should Linux/macOS runtime lanes be required on PR for all changes, or required only for boundary-touching changes with label-based gating?
- What is the accepted retry budget per transient category in package smoke before marking the pipeline unstable?
