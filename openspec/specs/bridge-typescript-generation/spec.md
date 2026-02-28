## Purpose
Define TypeScript declaration generation contracts for bridge-export/import interfaces.
## Requirements
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

### Requirement: Generated bridge declarations SHALL pass deterministic TypeScript compile validation
Build governance SHALL compile generated `bridge.d.ts` against a TypeScript harness to ensure declaration correctness.

#### Scenario: Declaration compile succeeds
- **WHEN** governance validation runs declaration compile checks
- **THEN** `bridge.d.ts` compiles without TypeScript errors

#### Scenario: Declaration shape regression is introduced
- **WHEN** generated declarations are invalid or missing required symbols
- **THEN** governance check fails with actionable TypeScript diagnostics

### Requirement: CancellationToken maps to AbortSignal options in TypeScript
When a bridge method has a CancellationToken parameter, the TypeScript declaration SHALL emit an `options?: { signal?: AbortSignal }` parameter instead of the CancellationToken.

#### Scenario: Method with CancellationToken generates AbortSignal option
- **WHEN** a `[JsExport]` method is `Task<string> Search(string query, CancellationToken ct)`
- **THEN** the TypeScript declaration is `search(query: string, options?: { signal?: AbortSignal }): Promise<string>`

### Requirement: IAsyncEnumerable maps to AsyncIterable in TypeScript
When a bridge method returns `IAsyncEnumerable<T>`, the TypeScript declaration SHALL emit `AsyncIterable<T>` as the return type.

#### Scenario: IAsyncEnumerable return generates AsyncIterable declaration
- **WHEN** a `[JsExport]` method is `IAsyncEnumerable<string> StreamChat(string prompt)`
- **THEN** the TypeScript declaration is `streamChat(prompt: string): AsyncIterable<string>`

### Requirement: TypeScript emitter generates overloaded function signatures
The TypeScript emitter SHALL generate multiple function signatures for overloaded methods in the same interface declaration.

#### Scenario: Overloaded methods produce multiple TypeScript signatures
- **WHEN** an interface has `Search(string query)` and `Search(string query, int limit)`
- **THEN** the generated TypeScript contains two `search(...)` signatures with different parameter lists

#### Scenario: Non-overloaded methods produce single signature
- **WHEN** an interface has no overloaded methods
- **THEN** each method produces exactly one TypeScript signature (unchanged behavior)

### Requirement: Sample bridge service layer SHALL converge on typed bridge client contract
TypeScript sample service layers SHALL use a consistent typed client contract surface so generated declaration and runtime invocation semantics remain aligned.

#### Scenario: React and Vue samples share typed client usage pattern
- **WHEN** sample bridge service code is inspected
- **THEN** both React and Vue service layers import and consume `@agibuild/bridge` typed client entry points

