## 1. Release orchestration contract implementation

- [x] 1.1 Add release-orchestration decision target(s) in build governance with deterministic `ready/blocked` output.
- [x] 1.2 Wire `CiPublish` to require release-orchestration gate before publish side-effect targets.

## 2. Evidence payload and diagnostics

- [x] 2.1 Extend CI evidence v2 payload with release decision summary and structured blocking reason entries.
- [x] 2.2 Ensure blocking taxonomy categories are deterministic and machine-checkable.

## 3. Governance and regression tests

- [x] 3.1 Add/update governance tests for `CiPublish` release-orchestration gate ordering.
- [x] 3.2 Add/update governance tests for evidence v2 release decision schema and blocking reason fields.
- [x] 3.3 Add/update release-versioning tests to enforce stable publish requires `ready` decision state.

## 4. Verification

- [x] 4.1 Run `dotnet test` for impacted governance/versioning tests.
- [x] 4.2 Run `nuke Test`.
- [x] 4.3 Run `nuke Coverage`.
- [x] 4.4 Run `openspec validate --all --strict`.
