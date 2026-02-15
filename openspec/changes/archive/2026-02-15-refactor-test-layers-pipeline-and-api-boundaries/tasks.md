## 1. Scheme 1 - Test Layer Refactor (ContractAutomation vs RuntimeAutomation)

- [x] 1.1 Define lane taxonomy constants, naming rules, and suite grouping for `ContractAutomation` and `RuntimeAutomation`. (Deliverable: D1, AC: test projects expose lane-specific groupings and CI can run each lane independently.)
- [x] 1.2 Refactor existing automation test placement to follow lane ownership without changing validated behavior. (Deliverable: D1, AC: moved suites preserve pass results and no scenario is lost.)
- [x] 1.3 Create runtime critical-path manifest mapping required boundary scenarios to concrete runtime tests. (Deliverable: D1, AC: manifest covers off-thread marshaling, lifecycle transitions, option isolation, package smoke path.)
- [x] 1.4 Add CI/report output that publishes lane-specific pass/fail and skipped-lane reasons. (Deliverable: D1, AC: CI artifacts show distinct lane status and skip rationale.)

## 2. Scheme 2 - Build/Pipeline Resilience Hardening

- [x] 2.1 Implement deterministic NuGet package-root resolution and log normalized effective path in smoke targets. (Deliverable: D2, AC: smoke run always reports exact cache root and cleanup scope.)
- [x] 2.2 Add transient-failure classifier for packaging/smoke steps with bounded retry policy per category. (Deliverable: D2, AC: deterministic failures fail fast; transient categories retry within configured budget.)
- [x] 2.3 Add machine-readable retry telemetry (category, attempt count, final outcome) for pipeline diagnostics. (Deliverable: D2, AC: logs can be parsed to reconstruct retry decisions.)
- [x] 2.4 Add integration validation for package-consumption smoke under clean-cache conditions. (Deliverable: D2, AC: smoke test proves package dependency closure and produces actionable failure diagnostics.)

## 3. Scheme 3 - API/Boundary Governance Closure

- [x] 3.1 Extend blocking-wait governance scans to build/orchestration code and approved synchronization helpers. (Deliverable: D3, AC: new unapproved blocking waits fail governance tests with owner/rationale guidance.)
- [x] 3.2 Refactor lifecycle wiring assertions to approved internal test hooks/facades where reflection-only checks are currently brittle. (Deliverable: D3, AC: boundary tests pass with reduced private-reflection dependency.)
- [x] 3.3 Add API audit traceability linking boundary-sensitive public APIs to contract and runtime evidence. (Deliverable: D3, AC: audit checklist includes API -> test mapping and closure status.)
- [x] 3.4 Enforce instance-scoped environment option semantics in runtime-facing validation matrix. (Deliverable: D3, AC: runtime automation fails if global options mutate implicitly.)

## 4. Verification and Release Readiness

- [x] 4.1 Run unit + integration automation with lane-specific targets on Windows/Linux/macOS where available. (Deliverable: D4, AC: lane reports are complete and reproducible.)
- [x] 4.2 Run package smoke validation with transient-classification logs and verify retry/fail-fast behavior. (Deliverable: D4, AC: telemetry proves expected retry strategy.)
- [x] 4.3 Update OpenSpec change checklist with final evidence for D1-D4 and validate change consistency. (Deliverable: D4, AC: `openspec validate` passes for this change.)
