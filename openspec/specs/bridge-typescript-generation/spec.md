## Purpose
Define TypeScript declaration generation contracts for bridge-export/import interfaces.

## Requirements

### Requirement: TypeScript emitter maps supported CLR types deterministically
The generator SHALL map supported CLR types to TypeScript declarations and SHALL emit per-service interfaces with deterministic naming and JSDoc metadata.

#### Scenario: CLR-to-TypeScript mapping is deterministic
- **WHEN** bridge interfaces include primitive and common structured CLR types
- **THEN** generated declarations use stable TypeScript mappings and service signatures

### Requirement: Source generator emits declaration artifacts
`WebViewBridgeGenerator` SHALL emit declaration artifacts alongside existing bridge outputs so runtime/tooling can access generated type definitions.

#### Scenario: Build emits bridge declaration artifacts
- **WHEN** source generation runs during build
- **THEN** declaration artifacts are generated and available for downstream packaging

### Requirement: MSBuild target writes bridge.d.ts with configurable output
Build integration SHALL write `bridge.d.ts` after generation and SHALL support configuration through `GenerateBridgeTypeScript` and `BridgeTypeScriptOutputDir`.

#### Scenario: Configurable output directory is honored
- **WHEN** `BridgeTypeScriptOutputDir` is configured
- **THEN** `bridge.d.ts` is written to the configured directory deterministically
