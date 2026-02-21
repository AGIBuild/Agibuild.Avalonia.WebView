## 1. Baseline Diagnosis and Scope Lock

- [x] 1.1 Run `openspec validate --all --strict` and classify failure categories (structure, normative wording, missing scenarios).
- [x] 1.2 Confirm cleanup scope is governance-only and does not introduce runtime behavior changes.

## 2. Spec Corpus Remediation

- [x] 2.1 Normalize affected specs to strict structural format (`## Purpose` + `## Requirements`).
- [x] 2.2 Repair non-normative requirement statements to include `SHALL`/`MUST` without changing behavior.
- [x] 2.3 Add missing `#### Scenario:` blocks for requirements lacking executable acceptance shape.

## 3. Validation and Closeout

- [x] 3.1 Re-run full strict validation and confirm repository-wide green baseline (`51 passed, 0 failed`).
- [x] 3.2 Record governance artifacts (proposal/spec/design/tasks) and archive the change.
