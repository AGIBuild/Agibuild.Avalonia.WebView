## MODIFIED Requirements

### Requirement: TypeScript emitter maps supported CLR types deterministically
The generator SHALL map supported CLR types to TypeScript declarations and SHALL emit per-service interfaces with deterministic naming and JSDoc metadata. The type mapper SHALL correctly handle nested generic types by using bracket-depth-aware parsing instead of simple comma splitting.

#### Scenario: CLR-to-TypeScript mapping is deterministic
- **WHEN** bridge interfaces include primitive and common structured CLR types
- **THEN** generated declarations use stable TypeScript mappings and service signatures

#### Scenario: Nested generic type mapping produces correct output
- **WHEN** a bridge method uses `Dictionary<string, List<int>>` as a parameter or return type
- **THEN** the TypeScript mapping produces `Record<string, number[]>`

#### Scenario: Deeply nested generic type mapping
- **WHEN** a bridge method uses `Dictionary<string, Dictionary<int, List<string>>>` as a type
- **THEN** the TypeScript mapping produces `Record<string, Record<number, string[]>>`

#### Scenario: Generic collection with complex inner type
- **WHEN** a bridge method uses `List<Dictionary<string, bool>>` as a type
- **THEN** the TypeScript mapping produces `Record<string, boolean>[]`
