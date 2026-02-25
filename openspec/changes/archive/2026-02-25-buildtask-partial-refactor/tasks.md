## 1. OpenSpec and Entry Rename

- [x] 1.1 Create and complete proposal/design/spec/tasks artifacts for build script partial refactor.
- [x] 1.2 Rename build entry class from `_Build` to `BuildTask` and update `Execute<T>()` bootstrap.

## 2. Partial File Modularization

- [x] 2.1 Split warning governance records/methods into `Build.WarningGovernance.cs`.
- [x] 2.2 Split general helper methods into `Build.Helpers.cs`.
- [x] 2.3 Split NuGet/publishing helpers into `Build.Publishing.cs`.
- [x] 2.4 Split react/npm helpers into `Build.React.cs`.

## 3. Governance + Validation

- [x] 3.1 Update build governance assertions for partial layout compatibility.
- [x] 3.2 Run `openspec validate --all --strict`.
- [x] 3.3 Run `nuke Test` and `nuke Coverage`.
- [x] 3.4 Archive change after all checks pass.
