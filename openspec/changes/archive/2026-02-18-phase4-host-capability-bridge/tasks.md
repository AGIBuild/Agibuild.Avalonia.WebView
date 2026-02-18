## 1. Typed Capability Contracts (Deliverable 4.3)

- [x] 1.1 Define typed capability operations and payload contracts for clipboard, file dialog, external open, and notification (Acceptance: contracts compile with no platform-specific dependency leakage).
- [x] 1.2 Define capability authorization policy contracts and decision/result models (Acceptance: policy can return deterministic allow/deny semantics with reason metadata).
- [x] 1.3 Define host capability provider abstraction and bridge options/wiring contracts (Acceptance: bridge can be configured opt-in and remains non-breaking when disabled).

## 2. Runtime Bridge Implementation (Deliverable 4.3)

- [x] 2.1 Implement host capability bridge runtime service with operation dispatch per capability type (Acceptance: typed operations execute through one deterministic bridge pipeline).
- [x] 2.2 Implement authorization-first execution flow (policy gate before provider invocation) (Acceptance: denied calls never execute provider).
- [x] 2.3 Integrate shell external-open strategy with typed capability bridge (Acceptance: external-browser strategy routes through bridge when configured, fallback remains deterministic when not configured).
- [x] 2.4 Implement failure isolation and error reporting semantics for capability and policy exceptions (Acceptance: failure in one capability path does not corrupt unrelated capability operations).

## 3. Contract Test Coverage (G3/G4, Deliverable 4.3)

- [x] 3.1 Add CT for typed request/response semantics across clipboard, file dialog, external open, and notification (Acceptance: each capability has at least one success-path assertion).
- [x] 3.2 Add CT for authorization allow/deny branches and reason propagation (Acceptance: deny branch bypasses provider execution and returns typed denied result).
- [x] 3.3 Add CT for bridge disabled behavior and shell non-breaking fallback (Acceptance: no bridge config preserves baseline shell behavior).
- [x] 3.4 Add CT for capability failure isolation and deterministic error classification (Acceptance: failure path assertions pass without cross-capability side effects).

## 4. Integration Validation (G3/G4, Deliverable 4.3)

- [x] 4.1 Add focused IT for representative capability flow in shell runtime (Acceptance: capability calls and policy enforcement pass in runtime automation lane).
- [x] 4.2 Add stress IT for repeated external-open/capability invocations with policy gating (Acceptance: deterministic pass criteria and no state leakage).

## 5. Milestone Exit Checks (M4.3)

- [x] 5.1 Run impacted unit/contract/integration suites and record command/output evidence (Acceptance: all impacted suites pass with reproducible command set).
- [x] 5.2 Produce requirements-to-tests traceability for `webview-host-capability-bridge`, `webview-shell-experience`, and `webview-multi-window-lifecycle` deltas (Acceptance: each scenario is mapped to CT or IT coverage).
