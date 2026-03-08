## ADDED Requirements

### Requirement: Structural refactoring SHALL preserve all existing public API contracts

Internal restructuring (file splits, method extractions, deduplication) SHALL NOT change any public type signatures, namespaces, or observable behavior. All existing tests SHALL pass without modification to test assertions.

#### Scenario: All unit and integration tests pass after refactoring

- **WHEN** the full test suite is executed after structural refactoring
- **THEN** all 1,879 unit tests and 209 integration tests SHALL pass with zero assertion changes

#### Scenario: Public API surface remains identical

- **WHEN** the public API surface is compared before and after refactoring
- **THEN** all public types, methods, properties, and namespaces SHALL be identical

### Requirement: Build system helpers SHALL eliminate serialization duplication

The build system SHALL provide a single `WriteJsonReport` helper method used by all governance targets instead of inline `JsonSerializer.Serialize` calls.

#### Scenario: JSON report serialization uses shared helper

- **WHEN** any governance target writes a JSON report file
- **THEN** it SHALL use the shared `WriteJsonReport` helper with consistent `WriteIndented = true` formatting

### Requirement: Platform adapters SHALL use shared bridge script factory

Adapters that maintain inline WebView bridge bootstrap scripts SHALL obtain script content from shared `WebViewBridgeScriptFactory` instead of maintaining duplicated inline copies.

#### Scenario: Bridge script is sourced from factory

- **WHEN** an adapter with inline bridge bootstrap injection injects initialization script (Windows/Android)
- **THEN** it SHALL call `WebViewBridgeScriptFactory` to obtain the script content

### Requirement: Governance contract tests SHALL use syntax-level assertions for high-risk build contracts

Governance tests that validate build target graph, `DependsOn` dependencies, evidence schema assignments, and release/bridge governance command wiring SHALL use syntax-level assertions (Roslyn-based) rather than raw source-string matching for high-risk checks.

#### Scenario: Build governance contracts are validated structurally

- **WHEN** governance tests validate target declarations, dependency graph inclusion, and evidence schema member assignments
- **THEN** they SHALL assert syntax structure (nodes/assignments/invocations/literals) instead of relying only on substring or regex matching
