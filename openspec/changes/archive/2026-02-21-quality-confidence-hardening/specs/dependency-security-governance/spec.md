## ADDED Requirements

### Requirement: CI MUST perform deterministic dependency vulnerability scans
The repository SHALL run dependency vulnerability scans for NuGet and npm ecosystems during governed CI targets, and SHALL fail when known vulnerabilities are detected.

#### Scenario: NuGet vulnerability detected
- **WHEN** `dotnet list <solution> package --vulnerable --include-transitive` reports vulnerable packages
- **THEN** governance target fails with an actionable report listing package id, affected version, and advisory severity

#### Scenario: npm vulnerability detected
- **WHEN** npm audit on governed web workspaces reports vulnerabilities at or above configured severity
- **THEN** governance target fails with actionable output and package path context

### Requirement: Dependency governance output SHALL be machine-readable
Dependency scan outcomes SHALL be written to a deterministic JSON artifact for CI evidence and auditability.

#### Scenario: Scan completes successfully
- **WHEN** dependency governance target completes
- **THEN** `artifacts/test-results/dependency-governance-report.json` exists and includes scanner command metadata, findings summary, and pass/fail result
