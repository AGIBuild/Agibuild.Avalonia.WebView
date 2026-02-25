## 1. Bridge Metadata Registry Hardening (Deliverable 5.3)

- [x] 1.1 Add reserved metadata key registry and extension-lane rule (`platform.extension.*`) in `WebViewHostCapabilityBridge` (Acceptance: reserved keys allow, unknown non-extension keys deterministic deny).
- [x] 1.2 Emit stable deny taxonomy for registry violations and assert policy evaluate count remains zero (Acceptance: unit tests verify deny reason + zero policy execution).

## 2. Canonical Timestamp Semantics (Deliverable 5.3)

- [x] 2.1 Normalize inbound `OccurredAtUtc` to UTC millisecond precision before dispatch (Acceptance: dispatch payload timestamp is canonicalized deterministically).
- [x] 2.2 Add CT/IT assertions for timestamp normalization in bridge and shell roundtrip paths (Acceptance: tests verify canonical timestamp wire value).

## 3. Template ShowAbout Toggle Strategy (Deliverable 5.4)

- [x] 3.1 Replace compile-time ShowAbout toggle with explicit runtime opt-in marker while keeping default deny (Acceptance: default run denies, opt-in marker path allowlist includes ShowAbout).
- [x] 3.2 Update app-shell demo/governance markers to reflect runtime toggle and canonical timestamp consumption (Acceptance: governance tests find required markers and baseline remains clean).

## 4. Governance and Matrices (Deliverable 5.5)

- [x] 4.1 Extend governance tests to detect reserved-key registry drift and canonical timestamp marker regressions (Acceptance: CI-facing governance checks include new marker assertions).
- [x] 4.2 Update CT/production/runtime matrices for new registry/timestamp branches (Acceptance: matrix references include allow/deny/failure evidence rows).

## 5. Verification and Quality Gate (Deliverable 5.5)

- [x] 5.1 Run full validation: `openspec validate --all --strict`, `nuke Test`, and `nuke Coverage` (Acceptance: all commands pass with coverage thresholds satisfied).
- [x] 5.2 Record verification evidence and archive-ready checklist updates (Acceptance: evidence maps KPI -> implementation -> test outcomes).
