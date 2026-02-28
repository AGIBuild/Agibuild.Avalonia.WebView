## Purpose
Enforce V1 bridge scope boundaries at compile time via Roslyn analyzer diagnostics, preventing the source generator from emitting invalid code for unsupported patterns.

## ADDED Requirements

### Requirement: Generator SHALL report error for generic methods
The source generator SHALL emit diagnostic `AGBR001` with `DiagnosticSeverity.Error` when a `[JsExport]` or `[JsImport]` interface contains a method with generic type parameters.

#### Scenario: Generic method on JsExport interface
- **WHEN** a `[JsExport]` interface declares `Task<T> GetItem<T>(string id)`
- **THEN** the generator reports `AGBR001` on the method declaration
- **AND** no source code is emitted for that interface

#### Scenario: Non-generic method is unaffected
- **WHEN** a `[JsExport]` interface declares `Task<string> GetItem(string id)`
- **THEN** no diagnostic is reported and source code is emitted normally

### Requirement: Generator SHALL report error for method overloads
The source generator SHALL emit diagnostic `AGBR002` with `DiagnosticSeverity.Error` when a `[JsExport]` or `[JsImport]` interface contains two or more methods with the same name.

#### Scenario: Overloaded methods on JsExport interface
- **WHEN** a `[JsExport]` interface declares both `Task Search(string query)` and `Task Search(string query, int limit)`
- **THEN** the generator reports `AGBR002` for each overloaded method
- **AND** no source code is emitted for that interface

#### Scenario: Distinct method names are unaffected
- **WHEN** a `[JsExport]` interface declares `Task Search(string query)` and `Task GetById(string id)`
- **THEN** no diagnostic is reported

### Requirement: Generator SHALL report error for ref/out/in parameters
The source generator SHALL emit diagnostic `AGBR003` with `DiagnosticSeverity.Error` when a bridge method has a parameter with `ref`, `out`, or `in` modifier.

#### Scenario: ref parameter on bridge method
- **WHEN** a `[JsExport]` interface declares `Task Process(ref string data)`
- **THEN** the generator reports `AGBR003` on the parameter
- **AND** no source code is emitted for that interface

#### Scenario: out parameter on bridge method
- **WHEN** a `[JsImport]` interface declares `Task<bool> TryParse(string input, out int result)`
- **THEN** the generator reports `AGBR003` on the `out` parameter

### Requirement: Generator SHALL report error for CancellationToken parameters
The source generator SHALL emit diagnostic `AGBR004` with `DiagnosticSeverity.Error` when a bridge method has a `System.Threading.CancellationToken` parameter. The diagnostic message SHALL indicate this will be supported in a future version.

#### Scenario: CancellationToken parameter on JsExport method
- **WHEN** a `[JsExport]` interface declares `Task<string> Process(string input, CancellationToken ct)`
- **THEN** the generator reports `AGBR004` on the method
- **AND** the message includes "will be supported in a future version"

### Requirement: Generator SHALL report error for IAsyncEnumerable return type
The source generator SHALL emit diagnostic `AGBR005` with `DiagnosticSeverity.Error` when a bridge method returns `IAsyncEnumerable<T>`. The diagnostic message SHALL indicate this will be supported in a future version.

#### Scenario: IAsyncEnumerable return type on JsExport method
- **WHEN** a `[JsExport]` interface declares `IAsyncEnumerable<int> StreamData()`
- **THEN** the generator reports `AGBR005` on the method
- **AND** the message includes "will be supported in a future version"

### Requirement: Generator SHALL skip code emission for interfaces with diagnostics
When any `AGBR001`–`AGBR005` diagnostic is reported for an interface, the generator SHALL NOT emit any source files (BridgeRegistration, BridgeProxy, JS stub) for that interface. Other valid interfaces in the same compilation SHALL still be processed normally.

#### Scenario: One invalid and one valid interface in same compilation
- **WHEN** the compilation contains `[JsExport] IInvalid` with a generic method and `[JsExport] IValid` with normal methods
- **THEN** diagnostics are reported only for `IInvalid`
- **AND** source code is emitted only for `IValid`
