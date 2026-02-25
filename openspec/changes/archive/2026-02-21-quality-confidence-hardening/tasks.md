## 1. Branch Coverage Governance

- [x] 1.1 Add `BranchCoverageThreshold` parameter to `BuildTask` and enforce Cobertura `branch-rate` gate in `Coverage` target.
- [x] 1.2 Include branch coverage in markdown summary output and phase closeout snapshot payload.
- [x] 1.3 Add/update governance tests that assert branch coverage gating remains wired in build orchestration.

## 2. Dependency Security Governance

- [x] 2.1 Add `DependencyVulnerabilityGovernance` target producing `artifacts/test-results/dependency-governance-report.json`.
- [x] 2.2 Wire dependency governance target into `Ci` and `CiPublish` dependency chain.
- [x] 2.3 Add governance tests asserting dependency scanner target exists and is required by CI targets.

## 3. Matrix / DevTools / Benchmark Hardening

- [x] 3.1 Extend matrix governance tests to enforce capability-evidence synchronization and non-empty per-platform coverage tokens.
- [x] 3.2 Add deterministic DevTools idempotence tests for open/close transitions in runtime contract tests.
- [x] 3.3 Add benchmark baseline governance test using deterministic baseline artifact comparison.

## 4. Validation

- [x] 4.1 Run targeted unit tests for updated governance/runtime test suites.
- [x] 4.2 Run `dotnet build build/Build.csproj` and `npx @fission-ai/openspec validate --all --strict`.
