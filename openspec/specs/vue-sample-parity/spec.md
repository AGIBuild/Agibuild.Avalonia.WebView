## Purpose
Define Vue sample parity requirements for web-first hybrid developer experience.

## Requirements

### Requirement: Vue sample SHALL provide executable bridge parity path
Repository SHALL provide a Vue-based hybrid sample proving typed bridge invocation flow parity with the React sample.

#### Scenario: Vue sample builds and references generated bridge types
- **WHEN** Vue sample TypeScript build runs
- **THEN** it resolves generated `bridge.d.ts` and compiles without bridge typing errors

#### Scenario: Vue sample includes typed bridge roundtrip example
- **WHEN** contributors inspect Vue sample app code
- **THEN** at least one typed C# bridge service call is demonstrated end-to-end
