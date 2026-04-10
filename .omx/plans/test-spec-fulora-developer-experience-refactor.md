# Test Spec — Fulora Developer Experience Refactor

## Scope under test

This test spec covers the planned DX refactor for Fulora's app-builder path, with emphasis on:
- primary path clarity (`new -> dev -> package`)
- generated RPC/client ergonomics
- dev-loop automation and diagnostics
- packaging-path productization

## Acceptance criteria

### AC1 — Primary path is singular and consistent
- README, getting-started docs, CLI help, templates, and samples present the same default app-builder path.
- First-contact examples prefer the canonical path and demote advanced platform concepts.

### AC2 — Generated app-facing client hides raw RPC strings in common usage
- Template/sample app code can consume generated services or a generated client factory without manually writing `window.agWebView.rpc.invoke("Service.method", ...)`.
- Raw RPC access remains available as an advanced escape hatch where explicitly needed.

### AC3 — Generation becomes a tooling concern, not a user-memory concern
- `dev` / build / package workflows either auto-maintain generated artifacts or fail with actionable drift guidance.
- Common bridge/generation failures surface clearer messages than before.

### AC4 — Packaging path is easier to understand and verify
- Named package profiles and/or preflight guidance are reflected in docs/CLI behavior.
- The path from scaffold to packaged app is documented as a coherent flow.

### AC5 — Migration is explicit
- Existing users have a documented migration or compatibility story for any changed generated client shapes.

## Verification matrix

### 1. Documentation and narrative checks
- README path verification
- docs landing/getting-started alignment check
- template starter README / sample narrative consistency check

Evidence:
- updated docs diff references
- grep/snapshot evidence that canonical examples use the new client API

### 2. Generator/client API checks
- unit tests for generated client naming/shape
- regression tests that verify new generated exports are present and typed
- compatibility tests for raw RPC escape hatch where retained
- checks for cancellation/stream/event generation semantics if touched

Evidence:
- targeted unit/integration test output
- generated fixture snapshots if used

### 3. Template and sample smoke checks
- scaffold/template smoke verification for canonical starter app
- sample build/typecheck where relevant
- verify bridge imports and default client usage compile/run as expected

Evidence:
- smoke-test logs
- sample/template verification output

### 4. Dev-loop and diagnostics checks
- verify generation is automatic or diagnostics are actionable during `dev`
- verify common drift/misconfiguration errors point to next steps
- verify bridge-ready path still works with template hooks and examples

Evidence:
- command output transcripts
- targeted diagnostics assertions

### 5. Packaging-path checks
- verify package profile docs/CLI guidance are aligned
- smoke verification for package path where feasible
- if doctor/preflight checks are added, verify success/failure messages

Evidence:
- CLI/test output
- docs references matching CLI behavior

## Proposed test layers

### Unit
- generated client API naming and shape
- helper/facade composition around raw RPC
- diagnostic message formatting

### Integration
- bridge generator/runtime compatibility for app-facing client surface
- template/project generation and build behavior
- CLI dev/package/preflight logic touched by the refactor

### E2E / smoke
- canonical starter path: create/scaffold -> dev -> package documentation and smoke path
- sample/template app runs using generated client API

### Observability / evidence
- machine-checkable logs or snapshots for CLI diagnostics if that surface changes
- documentation consistency checks where practical

## Risks to guard against

1. **Pretty façade, same confusion** — docs update but templates/generator still force raw RPC thinking.
2. **Compatibility breakage** — old generated examples or samples stop working without migration guidance.
3. **Narrative drift** — README/docs move faster than CLI/template reality.
4. **Overreach into transport rewrite** — implementation accidentally broadens beyond the approved boundary.

## Exit criteria for downstream execution mode

Before claiming completion, downstream execution must provide evidence for:
- updated canonical path across docs/templates/CLI
- working generated client ergonomics for common app usage
- verified dev-loop/generation behavior or diagnostics
- documented migration/compatibility story
- no unacknowledged divergence from the no-transport-rewrite boundary
