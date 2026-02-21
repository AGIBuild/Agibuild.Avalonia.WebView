## 1. OpenSpec Strict Governance Gate

- [x] 1.1 Add `OpenSpecStrictGovernance` target in `build/Build.cs` to execute `openspec validate --all --strict` as a hard-fail gate.
- [x] 1.2 Wire `Ci` and `CiPublish` targets to depend on `OpenSpecStrictGovernance`.

## 2. Governance Assertions

- [x] 2.1 Add/update governance tests to assert gate target presence, strict command usage, and CI dependency continuity.
- [x] 2.2 Ensure diagnostic schema evolution governance checks still enforce shared expectation source continuity.

## 3. Verification

- [x] 3.1 Run targeted governance/unit tests for build contract assertions.
- [x] 3.2 Execute `OpenSpecStrictGovernance` target and confirm it passes on current tree.
- [x] 3.3 Run `openspec validate --all --strict` and ensure zero failures.
