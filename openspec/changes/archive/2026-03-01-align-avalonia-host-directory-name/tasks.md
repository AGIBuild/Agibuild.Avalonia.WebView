## 1. Directory canonicalization

- [x] 1.1 Rename `src/Agibuild.Fulora` to `src/Agibuild.Fulora.Avalonia` (Deliverable: canonical host directory; acceptance: old directory path no longer exists).
- [x] 1.2 Update all repository references to host directory path (Deliverable: canonical path adoption; acceptance: governed files no longer reference `src/Agibuild.Fulora` for host artifacts).

## 2. Validation and closure

- [x] 2.1 Run targeted unit/integration/governance checks for path-sensitive references (Deliverable: deterministic validation; acceptance: selected checks pass).
- [x] 2.2 Mark tasks complete and run strict OpenSpec validation for this change (Deliverable: apply-ready state; acceptance: `openspec validate --changes align-avalonia-host-directory-name --strict` passes).
