## Context

- Shell experience currently governs new-window/download/permission/session domains but does not include DevTools policy governance.
- DevTools APIs are present on `IWebView`, so the missing layer is policy orchestration and deterministic auditing in shell runtime.
- The change must remain opt-in and non-breaking, consistent with existing shell architecture.

## Goals / Non-Goals

**Goals:**
- Add explicit shell DevTools policy contract with deterministic context.
- Provide shell-level DevTools operations that enforce policy-first execution.
- Preserve failure isolation semantics via existing policy-error pipeline.
- Add CT/IT coverage for allow/deny and isolation behavior.

**Non-Goals:**
- Shortcut registration and keyboard event handling.
- Platform-native DevTools customization.
- Broad refactors of existing shell policy domains.

## Decisions

### 1) Add dedicated DevTools policy domain
- **Decision:** Extend `WebViewShellPolicyDomain` with `DevTools` and add dedicated policy abstractions (`action`, `context`, `decision`, `policy interface`).
- **Rationale:** keeps policy semantics explicit and auditable without overloading permission/download policies.
- **Alternative considered:** reuse host capability bridge for DevTools. Rejected because DevTools is already a core `IWebView` operation and should not require host capability provider wiring.

### 2) Policy-first shell DevTools APIs
- **Decision:** Add shell methods for `OpenDevToolsAsync`, `CloseDevToolsAsync`, and `IsDevToolsOpenAsync` that execute policy before delegating to underlying `IWebView`.
- **Rationale:** deterministic runtime entry points for app-shell orchestration.
- **Alternative considered:** policy hook only on underlying `IWebView` calls. Rejected because it spreads policy across unrelated callsites and weakens shell encapsulation.

### 3) Failure classification and isolation reuse existing path
- **Decision:** Deny/exception paths report through existing `ReportPolicyFailure` and do not mutate unrelated domains.
- **Rationale:** preserves established error pipeline and test strategy.

## Risks / Trade-offs

- **[Policy denial ambiguity for query operation] →** treat deny as deterministic `false` result and emit policy error for auditability.
- **[Overlapping app-level direct IWebView calls] →** shell methods are opt-in; host apps that want governance must route DevTools through shell entry points.
- **[Potential domain coupling regressions] →** add tests that verify denied DevTools operations do not break permission policy behavior.

## Migration Plan

1. Add DevTools policy contracts and shell runtime integration.
2. Add unit tests for allow/deny and error reporting.
3. Add automation integration test for domain isolation under denied DevTools policy.
4. Run targeted + full suites.

Rollback: remove DevTools policy contracts/options and shell methods; existing direct `IWebView` DevTools behavior remains unchanged.

## Open Questions

- Whether a future milestone should add a shell-level toggle API and keyboard shortcut abstraction for unified desktop UX.
