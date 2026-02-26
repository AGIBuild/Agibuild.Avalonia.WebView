## ADDED Requirements

### Requirement: Sample bridge service layer SHALL converge on typed bridge client contract
TypeScript sample service layers SHALL use a consistent typed client contract surface so generated declaration and runtime invocation semantics remain aligned.

#### Scenario: React and Vue samples share typed client usage pattern
- **WHEN** sample bridge service code is inspected
- **THEN** both React and Vue service layers import and consume `@agibuild/bridge` typed client entry points
