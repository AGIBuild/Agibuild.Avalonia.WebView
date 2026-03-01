## Purpose
Define deterministic CLR-to-TypeScript bridge declaration generation, including typed binary mapping for `byte[]`.

## Requirements

### Requirement: TypeScript emitter maps supported CLR types deterministically
The generator SHALL map supported CLR types to TypeScript declarations and SHALL emit per-service interfaces with deterministic naming and JSDoc metadata. The type mapper SHALL correctly handle nested generic types by using bracket-depth-aware parsing instead of simple comma splitting. The binary CLR type `byte[]` SHALL map to `Uint8Array`.

#### Scenario: CLR-to-TypeScript mapping is deterministic
- **WHEN** bridge interfaces include primitive and common structured CLR types
- **THEN** generated declarations use stable TypeScript mappings and service signatures

#### Scenario: byte array mapping produces typed binary declaration
- **WHEN** a bridge method uses `byte[]` as parameter or return type
- **THEN** generated TypeScript declaration uses `Uint8Array` for that position
