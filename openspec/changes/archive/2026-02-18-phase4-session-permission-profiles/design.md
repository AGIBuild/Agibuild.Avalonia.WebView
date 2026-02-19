## Context

- M4.1/M4.2/M4.3 established shell policy domains, deterministic multi-window lifecycle, and typed host capability bridge.
- Session and permission governance is currently event-level and lacks profile abstraction for enterprise scenarios requiring per-window/per-scope isolation and auditable policy outcomes.
- ROADMAP **Phase 4 / Deliverable 4.4** requires `Session/permission profiles and governance rules`, dependent on 4.1 + G3.
- Architecture constraints from `agibuild_webview_design_doc.md`:
  - contract-first and platform-agnostic runtime semantics,
  - security at architecture level (policy pipeline),
  - CT-first with MockAdapter, then focused IT validation.

## Goals / Non-Goals

**Goals:**
- Define typed profile contracts that bind session scope and permission rules into one deterministic resolution model.
- Resolve profile per window (root/child) using explicit inheritance/override rules.
- Apply profile-driven permission decisions before fallback semantics in shell runtime.
- Emit stable, auditable profile decision metadata for diagnostics and lifecycle assertions.
- Add CT/IT coverage for profile resolution, inheritance/override, and deny-path behavior.

**Non-Goals:**
- Implement remote policy distribution/control plane.
- Add OS-specific permission UX surfaces outside existing adapter contracts.
- Change baseline opt-in semantics of existing shell features.

## Decisions

### 1) Unified profile contract (session + permission) over separate ad-hoc handlers
- **Decision:** Introduce a typed `WebViewSessionPermissionProfile` model resolved by one profile policy entry point.
- **Rationale:** keeps session and permission governance consistent and auditable; avoids divergent policy graphs.
- **Alternative considered:** keep separate session and permission policy pipelines. Rejected for drift and weaker traceability.

### 2) Window-context-based profile resolution
- **Decision:** Resolve profile using `(rootWindowId, parentWindowId, windowId, scopeIdentity, requestUri, permissionKind)` context and explicit parent-child inheritance semantics.
- **Rationale:** aligns with M4.2 stable window identity and deterministic lifecycle.
- **Alternative considered:** global profile selection by app scope only. Rejected due to insufficient multi-window control.

### 3) Profile-first permission evaluation with deterministic fallback
- **Decision:** Permission requests first query resolved profile; if profile has explicit decision, apply it; otherwise preserve baseline fallback behavior.
- **Rationale:** guarantees least privilege while preserving non-breaking compatibility.
- **Alternative considered:** fallback-first then optional profile override. Rejected because it weakens security intent and predictability.

### 4) Deterministic diagnostics and failure isolation
- **Decision:** Profile resolution and application failures are isolated per request and reported via existing shell policy error channels with profile identity metadata.
- **Rationale:** preserves runtime stability and enables auditability.
- **Alternative considered:** throw and fail fast. Rejected due to runtime disruption risk.

### 5) Testing strategy (CT/IT/MockBridge)
- **CT:** profile resolution matrix (root/child/inherit/override), permission decision precedence, fallback compatibility, failure isolation.
- **IT:** representative multi-window profile flow and stress run validating no stale profile/window correlation.

## Risks / Trade-offs

- **[Profile model complexity growth] →** Start with minimal required fields and deterministic defaults; defer advanced policy dimensions.
- **[Policy misconfiguration causes broad deny] →** require explicit default profile behavior and clear deny reason metadata.
- **[Cross-platform permission behavior variance] →** normalize profile decision semantics in runtime contracts; keep platform details behind adapters.

## Migration Plan

1. Add profile contracts and policy interfaces to runtime shell abstractions.
2. Wire profile resolution into session and permission execution points.
3. Add CT coverage for resolution and governance matrix.
4. Add focused IT for representative and stress profile scenarios.
5. Keep profile feature opt-in so unconfigured hosts retain existing behavior.

Rollback: remove profile configuration from shell options and fall back to existing M4.1/M4.3 policy behavior.

## Open Questions

- Should profile identity be host-defined string only, or include typed profile version/epoch metadata in M4.4?
- For child windows, should inherited profile be immutable by default, or allow selective override per permission kind?
- Should profile audit events be exposed as public runtime events in M4.4 or deferred to M4.6 observability hardening?
