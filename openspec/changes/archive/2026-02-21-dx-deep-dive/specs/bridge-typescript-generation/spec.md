## ADDED Requirements

### Requirement: Generated bridge declarations SHALL pass deterministic TypeScript compile validation
Build governance SHALL compile generated `bridge.d.ts` against a TypeScript harness to ensure declaration correctness.

#### Scenario: Declaration compile succeeds
- **WHEN** governance validation runs declaration compile checks
- **THEN** `bridge.d.ts` compiles without TypeScript errors

#### Scenario: Declaration shape regression is introduced
- **WHEN** generated declarations are invalid or missing required symbols
- **THEN** governance check fails with actionable TypeScript diagnostics
