## 1. OpenSpec alignment and dependency baseline

- [x] 1.1 Finalize delta specs for `webview-testing-harness` and `project-template` (Deliverables: G4 + Phase 3/3.1, 3.3).
- [x] 1.2 Inventory all repository test projects using `xunit` v2 and define exact migration targets with acceptance: no scoped project keeps v2 references.

## 2. Package migration implementation

- [x] 2.1 Update scoped test project `.csproj` files from `xunit` to `xunit.v3` with compatible runner references (Deliverables: G4 + Phase 3/3.3).
- [x] 2.2 Update `templates/agibuild-hybrid/HybridApp.Tests` test dependencies to xUnit v3 baseline (Deliverables: E1 + Phase 3/3.1).

## 3. Verification and regression control

- [x] 3.1 Run restore/build/test for affected projects and ensure test discovery/execution passes under `dotnet test`.
- [x] 3.2 Resolve any migration regressions and capture final verification evidence with clear pass/fail status.

## Verification evidence (current)

- PASS: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj` -> `Passed: 682`.
- PASS: `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj` -> `Passed: 123`.
- PASS: `dotnet test samples/avalonia-react/AvaloniReact.Tests/AvaloniReact.Tests.csproj` -> `Passed: 23`.
- PASS: `dotnet restore templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj` -> restore success.
- PASS: `dotnet build templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj` -> build success.
- PASS: `dotnet test templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj` -> `Passed: 3`.
- PASS: repo scan confirms no scoped `*.csproj` keeps `xunit` v2 (`<PackageReference Include="xunit"` => no matches).
