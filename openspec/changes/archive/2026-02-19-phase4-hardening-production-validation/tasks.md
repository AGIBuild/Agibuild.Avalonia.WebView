## 1. Shell Soak Integration Coverage (Deliverable 4.6)

- [x] 1.1 Add long-run shell production soak integration test covering repeated shell-scope attach/detach with managed-window lifecycle assertions (Acceptance: deterministic cycle invariants pass with no stale windows/handlers).
- [x] 1.2 Cover host capability and policy behavior inside soak workload (Acceptance: external-open and permission/download outcomes remain deterministic across cycles).

## 2. Production Matrix & Governance Wiring (Deliverable 4.6)

- [x] 2.1 Add machine-readable shell production matrix artifact with platform coverage and evidence mappings (Acceptance: matrix includes Windows/macOS/Linux coverage metadata and executable evidence entries).
- [x] 2.2 Extend governance unit tests to validate matrix structure and evidence references (Acceptance: governance fails when matrix references missing files/tests).

## 3. Runtime Critical Path Hardening (Deliverable 4.6)

- [x] 3.1 Extend runtime-critical-path manifest with required shell soak scenarios (Acceptance: shell soak IDs map to existing automation tests).
- [x] 3.2 Extend governance critical-path assertions to require new shell soak scenario IDs (Acceptance: missing shell soak ID fails deterministic test diagnostics).

## 4. Milestone Exit Checks (M4.6)

- [x] 4.1 Run impacted contract/governance and runtime automation test suites (Acceptance: all targeted suites pass).
- [x] 4.2 Produce verification evidence and requirements traceability for shell-production-validation scenarios (Acceptance: evidence maps each scenario to executable tests).
