## 1. Project identity rename

- [x] 1.1 Rename `src/Agibuild.Fulora/Agibuild.Fulora.csproj` to `src/Agibuild.Fulora/Agibuild.Fulora.Avalonia.csproj` (Deliverable: canonical filename; acceptance: old file path no longer exists in repo).
- [x] 1.2 Update all direct project-file references across solution/build/test/template assets (Deliverable: canonical path adoption; acceptance: repository search returns no old `.csproj` path in governed sources).

## 2. Validation and coverage

- [x] 2.1 Run targeted build/test/governance checks impacted by host project path references (Deliverable: deterministic verification; acceptance: selected checks pass).
- [x] 2.2 Update this change tasks to completed and ensure OpenSpec validation passes for this change (Deliverable: apply-ready change state; acceptance: `openspec validate --changes align-avalonia-host-project-name --strict` passes).
