## Context

- `IWebView` already exposes `ICommandManager` for standard editing commands, but shell policy orchestration currently does not govern those operations.
- Existing shell domains (new-window/download/permission/devtools/session) use policy-first deterministic execution and explicit failure reporting; command shortcuts should follow same model.

## Goals / Non-Goals

**Goals:**
- Add command/shortcut policy contracts to shell runtime.
- Add policy-governed command execution API in `WebViewShellExperience`.
- Preserve deterministic error reporting/isolation semantics.
- Add CT/IT coverage for allow/deny and domain isolation behavior.

**Non-Goals:**
- Global keyboard shortcut registration.
- New command taxonomy beyond existing `WebViewCommand`.
- Cross-module refactoring outside shell experience.

## Decisions

### 1) Command policy as first-class shell domain
- **Decision:** Add `Command` policy domain plus command action/context/decision abstractions.
- **Rationale:** keeps governance explicit and auditable like existing domains.
- **Alternative:** route via permission policy. Rejected due to semantic mismatch.

### 2) Single command execution API
- **Decision:** Provide `ExecuteCommandAsync(WebViewCommand command)` in shell experience.
- **Rationale:** deterministic entry point for host shortcut integration and menu command dispatch.
- **Alternative:** expose six dedicated methods (Copy/Cut/Paste/SelectAll/Undo/Redo). Rejected for API duplication.

### 3) Deterministic failure semantics
- **Decision:** deny → return `false` + report policy failure; missing `ICommandManager` → return `false` + report capability-not-supported failure.
- **Rationale:** no silent behavior, explicit diagnostics, stable cross-domain behavior.

## Risks / Trade-offs

- **[Host bypasses shell API and calls ICommandManager directly] →** document shell API as governance path; tests enforce shell behavior only.
- **[Adapter without command manager] →** explicit failure reporting and deterministic false result.
- **[Policy coupling regressions] →** integration test asserting permission domain still deterministic after command denial.

## Migration Plan

1. Add command policy contracts and shell execution API.
2. Add unit tests for allow/deny and missing command manager behavior.
3. Add integration test for domain isolation.
4. Run targeted and full suites.

Rollback: remove command policy domain and API; existing direct command manager usage remains unchanged.

## Open Questions

- Whether to add host-level shortcut profile composition (e.g., contextual command allowlists by window profile) in a later milestone.
