## 1. WebViewCore Hotspot Coverage Targeting (Deliverable D1)

- [x] 1.1 Define a machine-readable `WebViewCore` hotspot branch manifest (method/branch intent/test owner/lane). (Deliverable: D1, AC: manifest exists and every hotspot has owner + lane + test reference.)
- [x] 1.2 Add or refactor ContractAutomation tests to cover hotspot branches deterministically (no unbounded sleeps). (Deliverable: D1, AC: hotspot-owner tests pass deterministically and close declared branch gaps.)
- [x] 1.3 Add governance tests that fail when hotspot manifest entries have no executable evidence. (Deliverable: D1, AC: invalid/missing mapping fails test harness governance in CI.)

## 2. CI Warning Governance (Deliverable D2)

- [x] 2.1 Add warning-classification pipeline/reporting (`known-baseline` / `actionable` / `new-regression`) as machine-readable artifact output. (Deliverable: D2, AC: CI emits warning report artifact and categorizes all warnings.)
- [x] 2.2 Introduce governance metadata for `WindowsBase` conflict warnings (owner/rationale/review point) and gate ungoverned entries. (Deliverable: D2, AC: unmanaged conflict warnings fail the governance gate.)
- [x] 2.3 Enforce xUnit analyzer policy for touched test files and reject blanket suppression patterns. (Deliverable: D2, AC: touched-file analyzer warnings are zero or explicitly scoped with approved metadata.)

## 3. Verification and Closure (Deliverable D3)

- [x] 3.1 Run `nuke Coverage` and verify `WebViewCore` hotspot closure evidence plus updated aggregate coverage report. (Deliverable: D3, AC: coverage report and hotspot traceability artifact are generated and consistent.)
- [x] 3.2 Run CI-equivalent warning governance checks and verify report-driven gating behavior on synthetic regression cases. (Deliverable: D3, AC: new warning regression path is proven to fail; baseline path passes.)
- [x] 3.3 Update change evidence notes and validate OpenSpec artifacts (`openspec validate improve-webviewcore-coverage-and-ci-warning-governance`). (Deliverable: D3, AC: change validates and is ready for `/opsx:apply`.)

## 4. Evidence

- `nuke Coverage` succeeded; `artifacts/coverage-report/Summary.txt` reports line coverage `99%`, branch coverage `92.3%`, and `WebViewCore` line coverage `98.3%`.
- Hotspot traceability verification passed: all entries in `tests/webviewcore-hotspots.manifest.json` were found in `artifacts/coverage/unit-tests.trx` (`ALL_HOTSPOT_TESTS_PRESENT`).
- `nuke WarningGovernance` succeeded; `artifacts/test-results/warning-governance-report.json` summary is `knownBaseline=26`, `actionable=0`, `newRegression=0`.
- `nuke WarningGovernanceSyntheticCheck` succeeded and validated synthetic baseline/regression classification paths.
- `openspec validate improve-webviewcore-coverage-and-ci-warning-governance` succeeded.
