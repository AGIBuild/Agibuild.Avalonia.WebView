## 1. Contracts & Strategy Model (Deliverable 4.2)

- [x] 1.1 Define multi-window lifecycle contracts (window id, parent id, lifecycle states, strategy decision enum) in runtime shell abstractions (Acceptance: contracts compile and stay platform-agnostic).
- [x] 1.2 Extend shell new-window policy contract to support strategy outcomes (`in-place`, `managed-window`, `external-browser`, `delegate`) (Acceptance: compile-time API surface reflects all required strategies).
- [x] 1.3 Extend session policy context contracts with parent-child window relationship data (Acceptance: session policy can resolve inheritance vs isolation based on explicit window context).

## 2. Runtime Lifecycle Orchestration (Deliverable 4.2)

- [x] 2.1 Implement runtime-managed window registry keyed by stable window id (Acceptance: runtime can create, lookup, and remove managed window entries deterministically).
- [x] 2.2 Implement deterministic lifecycle transition pipeline (`Created -> Attached -> Ready -> Closing -> Closed`) (Acceptance: transitions are emitted in strict order with terminal `Closed` semantics).
- [x] 2.3 Implement strategy executor pipeline that resolves strategy before execution and routes to in-place/managed/external/delegate branches (Acceptance: strategy resolution and execution ordering is deterministic).
- [x] 2.4 Implement bounded close/teardown completion for managed windows (Acceptance: repeated open/close cycles do not retain stale active-window references).

## 3. Contract Test Coverage (G4, Deliverable 4.2)

- [x] 3.1 Add CT for strategy decision mapping and fallback behavior (Acceptance: tests verify branch correctness for all strategy outcomes).
- [x] 3.2 Add CT for lifecycle ordering and terminal state invariants (Acceptance: tests assert deterministic transition order and no post-closed transitions).
- [x] 3.3 Add CT for session inheritance/isolation decisions in parent-child window context (Acceptance: tests verify policy-driven scope reuse vs isolation decisions).
- [x] 3.4 Add CT for failure isolation in strategy execution and lifecycle teardown paths (Acceptance: failures are surfaced through defined error paths without corrupting unrelated windows).

## 4. Integration & Stress Validation (G3/G4, Deliverable 4.2)

- [x] 4.1 Add focused desktop IT for representative managed-window flow (Acceptance: create/open/route/close flow passes in runtime automation lane).
- [x] 4.2 Add stress IT for repeated multi-window open/close lifecycle (Acceptance: deterministic pass criteria with no active-window leaks or teardown regressions).

## 5. Milestone Exit Checks (M4.2)

- [x] 5.1 Run impacted unit/contract/integration suites and record outcomes in change notes (Acceptance: all impacted suites pass with reproducible commands and results).
- [x] 5.2 Produce requirements-to-tests traceability for `webview-multi-window-lifecycle`, `webview-shell-experience`, and `shell-session-policy` deltas (Acceptance: each scenario has a mapped CT or IT case).
