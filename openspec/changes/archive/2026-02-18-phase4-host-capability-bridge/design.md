## Context

- M4.1/M4.2 established shell policy and multi-window lifecycle, but host integration is still fragmented and callback-centric.
- Roadmap **Phase 4 / Deliverable 4.3** requires a typed host capability bridge for common shell powers (clipboard, file dialogs, external open, notifications).
- Security and testability constraints remain strict:
  - opt-in behavior by default,
  - explicit allow/deny policy per capability request,
  - deterministic runtime semantics and mock-first validation.
- Stakeholders:
  - host app teams migrating from bundled-browser APIs,
  - security/governance owners needing auditable capability decisions,
  - framework maintainers ensuring cross-platform consistency.

## Goals / Non-Goals

**Goals:**
- Define typed contracts for host capabilities:
  - clipboard (read/write text),
  - file dialogs (open/save intents),
  - external open (URI handoff),
  - notifications (user-visible toast intent).
- Introduce capability authorization policy contracts with explicit decisions (`Allow`/`Deny`) and reason metadata.
- Integrate bridge with shell runtime so multi-window and external-open strategies can route through typed capability handlers.
- Guarantee non-breaking opt-in behavior when capability bridge is not configured.
- Ensure CT/IT coverage for policy enforcement, failure isolation, and deterministic result semantics.

**Non-Goals:**
- Implementing platform-specific native UX details for every OS variant in M4.3.
- Exposing all bundled-browser host APIs.
- Coupling capability bridge directly to web JS bridge protocols in this milestone.

## Decisions

### 1) Typed capability operations over generic command bus
- **Decision:** Use strongly typed request/response contracts per capability domain.
- **Rationale:** Compile-time safety and clearer policy governance; aligns with G1-style typed surface expectations.
- **Alternative considered:** Generic string-based capability bus. Rejected due to weak type guarantees and harder auditing.

### 2) Separate authorization policy from capability execution
- **Decision:** Evaluate policy first, then execute capability provider only when allowed.
- **Rationale:** Enforces explicit deny path and auditable governance (G3).
- **Alternative considered:** Capability provider internally deciding allow/deny. Rejected because policy concerns leak into execution adapters.

### 3) Runtime bridge as shell-level orchestrator extension
- **Decision:** Host capability bridge lives in shell runtime and is consumed by shell/multi-window orchestration points.
- **Rationale:** Keeps platform adapters focused on WebView primitives; shell remains product semantics layer.
- **Alternative considered:** Put capabilities in core `IWebView`. Rejected to avoid inflating core contract with shell-only concerns.

### 4) Deterministic error and fallback behavior
- **Decision:** Denied requests return typed denied results; provider exceptions are isolated and surfaced through policy error channels.
- **Rationale:** predictable behavior under stress and easier test assertions.
- **Alternative considered:** throw-only failure semantics. Rejected due to inconsistent host control flow.

### 5) Testing strategy: CT-first, targeted IT
- **CT coverage:** authorization allow/deny branches, typed request/response mapping, failure isolation, no-op when bridge disabled.
- **IT coverage:** representative desktop flow for capability calls in shell scenarios and repeated external-open/dialog stress lanes.

## Risks / Trade-offs

- **[Capability overexposure] →** Require explicit registration + allow policy checks per capability operation.
- **[Platform behavior divergence] →** Define normalized typed outcomes in contracts; keep adapter-specific differences behind provider layer.
- **[API surface growth] →** Restrict M4.3 to initial capability set and defer advanced operations.
- **[Integration complexity with M4.2 lifecycle] →** Keep clear orchestration boundaries: strategy decision -> policy check -> provider execution.

## Migration Plan

1. Add typed host capability contracts + policy contracts in shell runtime.
2. Add runtime bridge implementation and integrate with existing shell strategy execution points.
3. Add CT for authorization and result semantics.
4. Add focused IT for representative capability bridge usage and stress behavior.
5. Keep existing shell behavior unchanged when host capability bridge is not configured.

Rollback: disable host capability bridge options and fall back to existing M4.1/M4.2 behavior paths.

## Open Questions

- Should notification capability include action callbacks in M4.3 or defer to M4.4/M4.5?
- For file dialogs, is single-path support sufficient in M4.3, or must multi-select be in initial set?
- Should capability policy deny reasons be standardized enum-only, or allow host-defined extension codes?
