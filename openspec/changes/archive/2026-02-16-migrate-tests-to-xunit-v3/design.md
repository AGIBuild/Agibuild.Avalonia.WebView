## Context

The repository currently uses a mixed xUnit package baseline (`xunit` v2 + `xunit.runner.visualstudio`) across multiple test projects. This creates maintenance overhead and increases risk of inconsistent discovery/execution behavior. The requested change is to migrate test projects to xUnit v3 while preserving deterministic automation lanes.

This work aligns with:
- **PROJECT.md / G4**: keep contract-driven tests reliable and maintainable.
- **ROADMAP.md / Phase 3 / 3.3**: quality hardening for GA readiness.
- **ROADMAP.md / Phase 3 / 3.1**: template output should reflect current test stack.

## Goals / Non-Goals

**Goals:**
- Standardize test package references on xUnit v3 for the targeted test projects.
- Keep `dotnet test` and Nuke automation behavior unchanged from a user perspective.
- Keep template-generated test projects aligned with the repository test baseline.
- Validate migration with real test execution (CT/IT automation entry points as applicable).

**Non-Goals:**
- Redesign test architecture or rewrite broad test logic.
- Change WebView runtime behavior, contracts, or platform adapter semantics.
- Introduce alternate test frameworks.

## Decisions

### Decision 1: Replace `xunit` with `xunit.v3` in targeted projects
- **Choice:** Use `xunit.v3` package instead of `xunit` v2 in all scoped test projects.
- **Rationale:** xUnit v3 is the intended modern package line; this removes v2 drift and reduces future upgrade churn.
- **Alternative considered:** Keep v2 in integration or template projects to reduce immediate risk. Rejected because user explicitly requested full migration scope (option B).

### Decision 2: Retain VSTest adapter via `xunit.runner.visualstudio`
- **Choice:** Keep `xunit.runner.visualstudio` on a v3-compatible version to preserve `dotnet test` and IDE test discovery.
- **Rationale:** Existing pipeline/tooling already depends on this execution model; smallest-risk migration path.
- **Alternative considered:** Switch to a different runner model immediately. Rejected to avoid introducing execution-mode variables during package migration.

### Decision 3: Treat template as first-class migration target
- **Choice:** Update `templates/agibuild-hybrid/HybridApp.Tests` package references in the same change.
- **Rationale:** Prevents generating new projects with stale xUnit v2 references and keeps E1 deliverable quality consistent.
- **Alternative considered:** Delay template update. Rejected because it would reintroduce old dependencies for new consumers.

## Risks / Trade-offs

- **[Risk] Avalonia headless xUnit integration compatibility variance** -> **Mitigation:** run integration automation tests after migration and adjust only package-level wiring without test-semantic changes.
- **[Risk] Runner/discovery regression in CI** -> **Mitigation:** keep adapter family unchanged and validate with `dotnet test` on affected projects.
- **[Risk] Version skew between sample/template and core tests** -> **Mitigation:** migrate all scoped projects within one change and verify together.

## Migration Plan

1. Update OpenSpec delta specs and tasks for `webview-testing-harness` and `project-template`.
2. Replace package references in targeted `.csproj` files (`xunit` -> `xunit.v3`; align adapter versions as needed).
3. Restore and run tests for affected projects (`dotnet test`), including automation project coverage where feasible.
4. Fix any migration-related compile/test regressions.
5. Confirm final test pass state and report validation evidence.

Rollback:
- Revert package references in changed test project files if critical incompatibility is found.

## Open Questions

- Are there any environment-specific CI agents that pin a test host incompatible with xUnit v3 adapter behavior?
- Should test package versions move to central package management in a follow-up to prevent future drift?
