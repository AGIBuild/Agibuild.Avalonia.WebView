## Context

- `webview-shell-experience` currently provides lightweight hooks (new-window/download/permission), but not a full policy foundation for Phase 4 shell evolution.
- Roadmap Phase 4 explicitly positions **M4.1 / Deliverable 4.1** as the base for later milestones (multi-window lifecycle, host capability bridge, session governance).
- The architecture must stay contract-first and UI-agnostic: Runtime defines semantics, platform adapters remain isolated, and tests must work with MockAdapter without a real browser.
- Stakeholders:
  - framework maintainers (stability + API evolution),
  - host app developers (reduced shell boilerplate),
  - enterprise/security users (policy control + auditability).

## Goals / Non-Goals

**Goals:**
- Define a unified shell policy model that covers:
  - new-window handling,
  - download governance,
  - permission governance,
  - session scope policy (new capability).
- Preserve non-breaking behavior: if shell policy is not enabled, existing runtime semantics remain unchanged.
- Make policy execution deterministic and testable via CT/IT:
  - clear execution order,
  - fallback rules,
  - failure semantics,
  - threading guarantees.
- Align directly with Roadmap **Phase 4 M4.1 / Deliverable 4.1** and goals **G3 + G4**.

**Non-Goals:**
- Full multi-window orchestration framework (M4.2).
- Full host capability bridge (M4.3).
- New external dependencies or UI framework coupling.

## Decisions

### 1) Policy object model over ad-hoc callbacks
- **Decision:** Standardize shell governance around policy objects/contracts, while still allowing delegate-based adapters for host ergonomics.
- **Rationale:** Policy objects are composable, testable, and versionable; ad-hoc callbacks alone become inconsistent across features.
- **Alternative considered:** Keep independent callbacks only. Rejected due to weak cross-feature consistency and difficult governance evolution.

### 2) Explicit fallback semantics as contract requirements
- **Decision:** Define fallback behavior for each policy domain (especially new-window and permission defaults) in spec requirements rather than implementation comments.
- **Rationale:** Phase 4 needs stable semantics for future milestones; contract-level fallback avoids hidden behavior drift.
- **Alternative considered:** Implicit fallback in runtime code. Rejected due to poor portability and auditability.

### 3) Session policy as separate capability (`shell-session-policy`)
- **Decision:** Introduce session governance as a dedicated capability now, while keeping implementation scope minimal in M4.1.
- **Rationale:** Session isolation is foundational for M4.2/M4.4; separate capability avoids overloading `webview-shell-experience`.
- **Alternative considered:** Put session requirements into `webview-shell-experience` only. Rejected due to coupling and unclear milestone boundaries.

### 4) Testing strategy: CT-first with focused IT
- **Decision:** Use CT as primary semantics gate (MockAdapter/TestDispatcher), and targeted IT only for platform-sensitive validation.
- **Rationale:** Matches project design principles (G4), improves speed and determinism, and keeps platform smoke focused.
- **CT coverage:** policy execution order, fallback behavior, no-op when disabled, permission/download state transitions.
- **IT coverage:** representative runtime flow on desktop targets and stress-oriented shell scenarios where platform integration matters.

## Risks / Trade-offs

- **[Policy surface grows too fast] →** Keep M4.1 constrained to foundation contracts; defer advanced orchestration to M4.2+.
- **[Cross-platform behavior divergence] →** Lock semantics in specs first, then enforce with CT baseline and platform IT deltas.
- **[Security bypass via host misuse] →** Require explicit opt-in and least-privilege policy defaults; document safe presets.
- **[Migration friction for existing hosts] →** Preserve default behavior and provide adapter/delegate wrappers for incremental adoption.

## Migration Plan

1. Introduce/extend shell policy contracts as opt-in.
2. Add/modify specs for `webview-shell-experience` and `shell-session-policy`.
3. Add CT for deterministic policy semantics and fallback behavior.
4. Add focused IT to validate runtime behavior where CT is insufficient.
5. Keep old host integrations functioning unchanged when shell policy is not configured.

Rollback: because changes are opt-in and non-breaking by contract, disabling shell policy usage reverts hosts to baseline behavior.

## Open Questions

- Should M4.1 define a canonical "secure default policy preset" or defer presets to M4.5 templates?
- For session policy, is minimum viable scope "shared vs isolated" only, or include partition identifiers in M4.1?
- Which IT lane should own shell stress validation initially (existing runtime automation lane vs dedicated shell lane)?
