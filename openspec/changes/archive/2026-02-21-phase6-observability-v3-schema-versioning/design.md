## Context

Phase 5 delivered structured diagnostics (M5.3) and governance evidence (M5.5), but runtime diagnostic payloads still rely on implicit schema understanding. As observability evolves, consumers need explicit schema versioning for deterministic parsing and audit continuity. The current test assertions for diagnostic schema are duplicated across unit, integration, and governance lanes, which increases drift risk.

## Goals / Non-Goals

**Goals:**
- Add explicit `DiagnosticSchemaVersion` to host capability diagnostics and session profile diagnostics.
- Emit schema version deterministically from runtime constructors/emission paths.
- Introduce one shared assertion helper for diagnostic schema invariants and adopt it across CT/IT/governance tests.

**Non-Goals:**
- Redesign diagnostic payload structure beyond schema-version addition.
- Introduce fallback dual-schema parsing inside runtime.
- Change policy evaluation or permission semantics.

## Decisions

1. **Decision: Put schema version directly on diagnostic event args**
   - Choice: add `DiagnosticSchemaVersion` to `WebViewHostCapabilityDiagnosticEventArgs` and `WebViewSessionPermissionProfileDiagnosticEventArgs`.
   - Why: schema version travels with every emitted runtime event and is assertion-friendly.
   - Alternatives considered:
     - External schema registry only: rejected; tests and consumers still lack payload-level version signal.
     - Metadata dictionary field: rejected; weakly typed and easier to drift.

2. **Decision: Use per-type runtime constants with the same initial value**
   - Choice: each diagnostic event args type owns a public `CurrentDiagnosticSchemaVersion` constant (initially `1`) and instance property value.
   - Why: explicit ownership avoids cross-component coupling while keeping version progression deterministic.
   - Alternatives considered:
     - One global runtime constant: rejected; over-couples unrelated diagnostic types.

3. **Decision: Centralize lane assertions in `Agibuild.Avalonia.WebView.Testing`**
   - Choice: add a shared helper for schema assertions and migrate CT/IT tests to use it; governance tests assert the contract constant exists and is consumed.
   - Why: prevents assertion drift and enforces one source of schema invariants.
   - Alternatives considered:
     - Keep duplicate per-test assertions: rejected; high maintenance and inconsistency risk.

## Risks / Trade-offs

- **[Risk] Test helper adoption misses some tests** → **Mitigation:** update all known diagnostics-focused CT/IT tests and run targeted suites.
- **[Risk] Governance tests may become too source-string-dependent** → **Mitigation:** assert both helper usage and runtime contract symbol presence.
- **[Trade-off] Additional public fields become long-term contract surface** → **Mitigation:** version field is intentionally stable and forward-compatible.

## Migration Plan

1. Add schema-version constants and properties to diagnostic event args types.
2. Wire version emission where diagnostic events are constructed.
3. Add shared schema assertion helper in testing library.
4. Update CT/IT/governance tests to use shared helper and validate version stability.
5. Run unit + integration automation test slices and full strict OpenSpec validation.

## Open Questions

- Should future schema evolution use independent version increments per diagnostic type or synchronized release-train increments?
