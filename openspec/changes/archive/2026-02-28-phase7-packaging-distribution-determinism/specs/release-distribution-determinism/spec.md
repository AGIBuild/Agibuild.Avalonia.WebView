## ADDED Requirements

### Requirement: Distribution readiness SHALL assert canonical package set completeness
Release distribution governance MUST verify that the canonical package set expected for the current lane context is present and structurally valid before publication decisions are finalized.

#### Scenario: Canonical package set is complete
- **WHEN** release governance evaluates packed artifacts for the current lane
- **THEN** all required canonical package identities are present and marked complete in a machine-readable distribution report

#### Scenario: Canonical package set is incomplete
- **WHEN** one or more required canonical package identities are missing
- **THEN** distribution readiness is marked failed with deterministic package identity diagnostics

### Requirement: Distribution readiness SHALL enforce package metadata policy
Distribution governance MUST validate package metadata policy for publication context, including stable metadata quality constraints and canonical identity conventions.

#### Scenario: Stable metadata policy passes
- **WHEN** a stable publication candidate is evaluated
- **THEN** all required metadata policy assertions pass without preview-language violations

#### Scenario: Stable metadata policy fails
- **WHEN** stable candidate metadata violates policy
- **THEN** distribution readiness emits deterministic expected-vs-actual metadata diagnostics

### Requirement: Distribution readiness SHALL emit deterministic machine-readable evidence
Distribution governance MUST emit a machine-readable report with stable schema fields for policy status, failed assertions, and artifact lineage references.

#### Scenario: Distribution report schema is complete
- **WHEN** CI generates distribution readiness evidence
- **THEN** report fields include schema version, lane context, evaluated assertions, and deterministic failure entries
