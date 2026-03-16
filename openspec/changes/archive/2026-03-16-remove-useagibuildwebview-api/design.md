## Context

`UseAgibuildWebView` and `UseFulora` currently coexist as equivalent startup APIs in Avalonia and DI extension layers. This duplicates public bootstrap semantics and leaves first-party assets (templates/samples/tests/comments) split across two names for the same operation.

This change is a post-stable API hardening task aligned with Roadmap Phase 9 `M9.2 API Surface Freeze` intent (single canonical surface) and ongoing Phase 12 maintenance quality goals. The implementation must remain consistent with `docs/agibuild_webview_design_doc.md` principles: contract-first boundaries, DI-based platform isolation, and deterministic/testable runtime initialization.

Stakeholders:
- Framework maintainers (public API governance and release quality)
- Template/sample consumers (bootstrap consistency)
- Downstream app teams currently using legacy alias methods

## Goals / Non-Goals

**Goals:**
- Remove legacy alias startup APIs `UseAgibuildWebView` from supported public extension surfaces.
- Standardize first-party startup code and comments to `UseFulora`.
- Preserve runtime initialization behavior (`WebViewEnvironment.Initialize(...)`) while changing only API naming surface.
- Provide deterministic migration path and verification coverage through unit/integration tests.

**Non-Goals:**
- No changes to adapter runtime behavior, bridge transport, or security policy pipeline.
- No fallback/compat wrappers that retain deprecated alias behavior.
- No unrelated startup architecture refactor beyond canonical API naming.

## Decisions

### Decision 1: Hard-remove alias methods instead of keeping `[Obsolete]` wrappers
- **Choice**: Delete `UseAgibuildWebView` extension methods in Avalonia and DI extension classes.
- **Why**: Keeping wrappers preserves API ambiguity and allows legacy naming regression in new code.
- **Alternatives considered**:
  - Keep alias + mark `[Obsolete]`: lower immediate break risk, but fails canonicalization objective and prolongs dual-path DX.
  - Keep alias indefinitely: rejected due to API surface drift and governance inconsistency.

### Decision 2: Migrate all first-party call sites in the same change
- **Choice**: Update templates, samples, smoke tests, and inline references to `UseFulora`.
- **Why**: Canonical API changes are incomplete if official assets still advertise removed names.
- **Alternatives considered**:
  - Partial migration (code only, docs later): rejected due to mixed guidance and avoidable confusion.

### Decision 3: Keep initialization semantics unchanged
- **Choice**: Retain existing `WebViewEnvironment.Initialize(...)` wiring and only rename call surface.
- **Why**: Change scope is API canonicalization, not behavior modification.
- **Alternatives considered**:
  - Opportunistic startup refactor: rejected to avoid coupling naming cleanup with behavior risk.

### Decision 4: Validate via unit + integration evidence
- **Choice**: Update/add tests that assert canonical `UseFulora` startup path initializes environment correctly.
- **Why**: Aligns with G4 contract-driven testability and prevents regressions in bootstrap path.
- **Alternatives considered**:
  - Rely on compile success only: rejected; does not validate runtime initialization side effects.

## Risks / Trade-offs

- **[Breaking API for legacy callers]** → Mitigation: explicit migration mapping (`UseAgibuildWebView(...)` -> `UseFulora(...)`) in spec/tasks and first-party usage updates.
- **[Missed legacy references in repository]** → Mitigation: repository-wide search gate for `UseAgibuildWebView` before completion.
- **[Perceived behavior change by consumers]** → Mitigation: preserve initializer internals and verify with unit/integration tests.
- **[Template drift after update]** → Mitigation: include template startup assertion in modified `project-template` spec and validate generated assets.

## Migration Plan

1. Remove alias extensions from:
   - `src/Agibuild.Fulora.Avalonia/AppBuilderExtensions.cs`
   - `src/Agibuild.Fulora.DependencyInjection/WebViewServiceCollectionExtensions.cs`
2. Replace all first-party call sites and references (`templates/`, `samples/`, `tests/`, comment/docs mentions) with `UseFulora`.
3. Update tests to cover canonical startup entrypoint behavior (unit and integration/smoke paths).
4. Run targeted validation (`dotnet test` for affected projects) and repository-wide legacy token scan.
5. Ship as a breaking-change cleanup item in release notes/changelog stream (no rollback shim planned).

Rollback strategy:
- If downstream breakage severity is unacceptable before release cut, revert this change set as a whole (code + template/docs) rather than reintroducing partial alias behavior.

## Open Questions

- Should a dedicated changelog migration snippet be added immediately in this change, or handled in the release aggregation change?
- Are any external template snapshots/tests expecting the legacy method name and requiring synchronized fixture updates?
