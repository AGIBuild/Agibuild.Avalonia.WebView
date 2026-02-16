## 1. Dependency boundary normalization (Deliverable D1)

- [x] 1.1 Refactor Windows adapter and pack project WebView2 references to disable `build/buildTransitive` injection and use explicit `Microsoft.Web.WebView2.Core` compile binding where required. (AC: any host can build/pack affected projects without `MSB3277`+`WindowsBase`)
- [x] 1.2 Remove project-level suppressions/comments that treat `MSB3277` as expected noise and align project files with source-elimination policy. (AC: no `NoWarn` usage remains for this warning class in affected projects)

## 2. Warning governance policy tightening (Deliverable D2)

- [x] 2.1 Update warning-governance classification logic to treat any `MSB3277`+`WindowsBase` occurrence as `new-regression` after elimination. (AC: classifier output marks recurrence as regression and fails gate)
- [x] 2.2 Update warning baseline metadata/schema usage for `windowsBaseConflicts` to match new invariant. (AC: baseline file no longer accepts active WindowsBase conflict entries)

## 3. Tests and cross-host verification (Deliverable D3)

- [x] 3.1 Add/update governance unit tests and synthetic checks for zero-conflict invariant and recurrence failure path. (AC: tests fail before fix and pass after fix)
- [x] 3.2 Run governed restore/build/test verification on affected targets and capture evidence in warning-governance report artifact. (AC: report shows zero accepted WindowsBase conflicts and no new regressions; packaging remains host-agnostic)
