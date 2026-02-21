## 1. Bridge Metadata Budget Hardening (Deliverable 5.3)

- [x] 1.1 Add aggregate metadata payload budget validation in `WebViewHostCapabilityBridge` with deterministic deny reason for over-budget events (acceptance: within-budget allows path, over-budget denied before policy/dispatch).
- [x] 1.2 Extend bridge unit tests for aggregate budget boundary (`exact budget`, `over budget`) and diagnostics reason stability (acceptance: new tests pass in `HostCapabilityBridgeTests` and existing metadata tests remain green).

## 2. Federated Pruning Diagnostic Attribution (Deliverable 5.3)

- [x] 2.1 Extend session/profile diagnostic contracts to carry `ProfileVersion` and `ProfileHash` and propagate through menu-pruning federation emission path (acceptance: diagnostics include revision fields when profile provides them; behavior unchanged when omitted).
- [x] 2.2 Add/adjust shell unit + automation integration assertions for federated pruning diagnostics with profile revision metadata (acceptance: deterministic assertions pass in unit and integration lanes).

## 3. Template ShowAbout Opt-in Guidance (Deliverable 5.4)

- [x] 3.1 Update app-shell template host wiring with explicit markerized ShowAbout opt-in snippet while preserving default deny behavior (acceptance: template still deny-by-default unless snippet is enabled).
- [x] 3.2 Update template governance tests/web marker assertions to verify opt-in marker presence and bounded metadata marker continuity (acceptance: governance tests pass and no raw payload bypass markers are introduced).

## 4. Governance Evidence and Change Closure (Deliverable 5.5)

- [x] 4.1 Update CT matrix/evidence artifacts for metadata budget and profile revision diagnostics branches (acceptance: matrix references concrete tests for allow/deny/failure coverage).
- [x] 4.2 Run focused unit/integration verification commands and record outcomes in change evidence files; mark all tasks complete (acceptance: all targeted tests pass and `openspec validate shell-federated-governance-followup --strict` succeeds).
