## Context

The runtime already emits `WebViewHostCapabilityDiagnosticEventArgs`, but integrations currently consume them directly and re-map per consumer. This increases drift risk and weakens machine-checkable governance for deny/failure taxonomy.

## Goals / Non-Goals

**Goals:**
- Provide a stable export DTO for host capability diagnostics.
- Add deterministic mapping from event args to export DTO.
- Prove taxonomy integrity in unit/integration tests.
- Register a dedicated long-run automation lane marker.

**Non-Goals:**
- No persistent storage pipeline.
- No new authorization logic.
- No change in capability call execution ordering.

## Decisions

### Decision 1: Export protocol as runtime DTO
- Choice: Add `WebViewHostCapabilityDiagnosticExportRecord` in runtime shell namespace.
- Rationale: Keeps mapping close to source contract and avoids external mapper divergence.

### Decision 2: Explicit mapper API
- Choice: Add `ToExportRecord()` method on `WebViewHostCapabilityDiagnosticEventArgs`.
- Rationale: Deterministic, single ownership, easy testability.

### Decision 3: Lane evidence via existing automation test suite
- Choice: Add a dedicated integration test and register it in runtime-critical-path and shell-production matrix.
- Rationale: No new harness complexity while improving regression observability.

## Risks / Trade-offs

- [Risk] Export DTO version drift from diagnostic schema version.  
  → Mitigation: map schema version directly from event arg and assert equality in tests.
- [Risk] Taxonomy assertions may become brittle if deny reasons evolve.  
  → Mitigation: assert structured fields and known reason families, keep intentional updates explicit.

## Testing Strategy

- Unit: export mapping for allow/deny/failure, including failure category and reason fields.
- Integration: end-to-end export records from system-integration flow.
- Governance: runtime-critical-path/shell-production manifests include new scenario and capability ids.
