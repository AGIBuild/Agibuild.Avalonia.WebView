## 1. Production Evidence Closeout

- [x] 1.1 Extend `tests/shell-production-matrix.json` with DevTools policy isolation and shortcut routing capabilities (Acceptance: each capability has platform coverage and executable evidence).
- [x] 1.2 Extend `tests/runtime-critical-path.manifest.json` with matching release-critical scenario IDs (Acceptance: IDs map to existing test methods).

## 2. Governance Guardrails

- [x] 2.1 Update `AutomationLaneGovernanceTests` required scenario list with new critical-path IDs (Acceptance: missing IDs fail deterministically).
- [x] 2.2 Add required shell capability IDs assertion for production matrix (Acceptance: capability deletions fail governance).

## 3. Roadmap Sync & Validation

- [x] 3.1 Update `openspec/ROADMAP.md` completion status for completed Phase 3.5 and Phase 4 closeout (Acceptance: status reflects delivered reality).
- [x] 3.2 Run impacted tests and record verification evidence (Acceptance: relevant suites pass).
