## Context

- M4.1 established shell policy foundations (new-window/download/permission/session) but still lacks explicit lifecycle semantics for opening, routing, and closing multiple windows.
- Roadmap **Phase 4 / Deliverable 4.2** requires a cross-platform strategy framework for multi-window behavior, with deterministic teardown under stress.
- The architecture must remain contract-first and adapter-agnostic:
  - Runtime defines lifecycle semantics,
  - adapters provide platform execution,
  - tests validate behavior through MockAdapter + focused platform automation.
- Stakeholders:
  - host app developers migrating from desktop-grade window workflows,
  - framework maintainers responsible for lifecycle stability,
  - enterprise teams requiring auditable policy/session behavior across windows.

## Goals / Non-Goals

**Goals:**
- Define runtime contracts for multi-window strategy decisions:
  - in-place navigation,
  - open managed dialog window,
  - external browser handoff,
  - host delegate strategy.
- Define deterministic window lifecycle states and event ordering for:
  - creation,
  - attach/ready,
  - close request,
  - teardown completion.
- Define window identity and parent-child relationship contracts to correlate:
  - NewWindow requests,
  - session scope decisions,
  - teardown assertions.
- Ensure all semantics are testable via CT first, then validated with focused IT/stress.

**Non-Goals:**
- Typed system capability bridge APIs (clipboard/file dialog/notification) for M4.3.
- Template preset delivery and migration scaffolds for M4.5.
- Full parity with all bundled-browser BrowserWindow options.

## Decisions

### 1) Runtime-owned lifecycle state machine
- **Decision:** Introduce a runtime lifecycle state model as the single source of truth for managed shell windows.
- **Rationale:** Keeps behavior deterministic across platforms and avoids adapter-specific state drift.
- **Alternative considered:** Let each platform adapter define its own window lifecycle model. Rejected due to inconsistent semantics and weaker CT coverage.

### 2) Strategy result contract instead of direct side effects
- **Decision:** New-window handling produces a strategy decision object (`in-place`, `managed-window`, `external`, `delegate`) that runtime executes.
- **Rationale:** Decouples policy decision from execution, enabling testable ordering and clearer fallback behavior.
- **Alternative considered:** Execute host callbacks directly in event handlers. Rejected due to opaque ordering and hard-to-test branch interactions.

### 3) Window identity and relationship envelope
- **Decision:** Every managed window gets a stable window id plus optional parent window id in runtime contracts.
- **Rationale:** Enables traceability for teardown, session reuse/isolation, and future capability governance.
- **Alternative considered:** Infer relationships from platform handles only. Rejected because handles are platform-specific and unstable for contract tests.

### 4) Teardown determinism before feature breadth
- **Decision:** M4.2 must guarantee ordered close/teardown semantics and bounded completion before expanding feature options.
- **Rationale:** Stability risk is highest in multi-window lifecycle; deterministic teardown is prerequisite for M4.3+.
- **Alternative considered:** Add broad strategy features first, then harden teardown later. Rejected due to high regression risk under stress.

### 5) Testing strategy: CT baseline + targeted IT stress
- **Decision:** Implement broad contract coverage with MockAdapter/TestDispatcher, then add targeted integration stress for managed windows.
- **CT focus:** strategy decision mapping, lifecycle order, fallback, window id correlation, session linkage.
- **IT focus:** representative desktop managed-window flow and repeated open/close stress with deterministic pass/fail markers.

## Risks / Trade-offs

- **[Cross-platform window primitive mismatch] →** Keep platform-neutral lifecycle contract and map adapter specifics behind strategy executors.
- **[Lifecycle race conditions during concurrent close/open] →** Serialize managed window lifecycle transitions by runtime-owned ordering.
- **[Session leakage across unintended windows] →** Bind session decisions to explicit window identity and parent-child context in contracts.
- **[Too much API surface too early] →** Keep M4.2 surface minimal and defer advanced options to later milestones.

## Migration Plan

1. Add multi-window lifecycle contracts and strategy decision model.
2. Extend shell experience/runtime wiring to evaluate strategy then execute through lifecycle orchestrator.
3. Add CT coverage for ordering, fallback, correlation, and session linkage.
4. Add focused IT + stress scenarios for repeated open/close lifecycle stability.
5. Keep existing single-window behavior unchanged when multi-window strategy is not enabled.

Rollback: disable multi-window strategy configuration and fall back to M4.1 shell behavior.

## Open Questions

- Should managed window creation be exposed as a standalone host API in M4.2, or remain event-driven only?
- Should external browser strategy be part of core strategy enum now, or introduced as an extension in M4.3?
- What is the minimum lifecycle event set for GA of M4.2 (Created/Ready/Closing/Closed vs. additional failure events)?
