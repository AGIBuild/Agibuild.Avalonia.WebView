## ADDED Requirements

### Requirement: Agibuild.Fulora naming is canonical for release-facing identity
The system SHALL use `Agibuild.Fulora` as the canonical product identity for all release-facing surfaces, including documentation titles, release notes, and package family naming.

#### Scenario: Release-facing artifacts use Agibuild.Fulora identity
- **WHEN** release artifacts are generated
- **THEN** product identity fields and user-facing labels use `Agibuild.Fulora` as canonical name

### Requirement: Legacy identifiers are disallowed in governed surfaces
Legacy canonical identifiers SHALL NOT appear in governed release-facing artifacts after `Agibuild.Fulora` canonicalization is enabled.

#### Scenario: Legacy identifier appears in governed scope
- **WHEN** a governed artifact contains a legacy canonical identifier
- **THEN** validation fails and reports artifact path with the offending token

### Requirement: Governance enforces identity consistency
Governance checks SHALL fail when any scoped release-facing artifact reintroduces legacy canonical naming after `Agibuild.Fulora` canonicalization is enabled.

#### Scenario: Legacy canonical naming regresses
- **WHEN** a governed artifact reintroduces legacy canonical naming in a scope marked as `Agibuild.Fulora`-canonical
- **THEN** governance fails with deterministic diagnostics including artifact path and expected canonical token
