## Purpose
Enforce V1 bridge scope boundaries at compile time via Roslyn analyzer diagnostics, preventing the source generator from emitting invalid code for unsupported patterns.

## Requirements

### Requirement: Generator SHALL report error for generic methods
The source generator SHALL emit diagnostic `AGBR001` with `DiagnosticSeverity.Error` when a `[JsExport]` or `[JsImport]` interface contains a method with generic type parameters. The message SHALL suggest using concrete methods or generic interfaces resolved at registration time.

#### Scenario: Generic method on JsExport interface
- **WHEN** a `[JsExport]` interface declares `Task<T> GetItem<T>(string id)`
- **THEN** the generator reports `AGBR001` on the method declaration
- **AND** no source code is emitted for that interface

#### Scenario: AGBR001 includes actionable suggestion
- **WHEN** a `[JsExport]` interface has a generic method `T Get<T>(string key)`
- **THEN** AGBR001 is reported with a message suggesting concrete method alternatives

#### Scenario: Non-generic method is unaffected
- **WHEN** a `[JsExport]` interface declares `Task<string> GetItem(string id)`
- **THEN** no diagnostic is reported and source code is emitted normally

### Requirement: Generator SHALL report error for method overloads with same param count
The source generator SHALL emit diagnostic `AGBR002` with `DiagnosticSeverity.Error` only when two or more overloads of the same method name have the same visible parameter count (excluding CancellationToken). Overloads with distinct param counts SHALL NOT trigger AGBR002.

#### Scenario: Same param count overloads trigger AGBR002
- **WHEN** a `[JsExport]` interface has `Search(string query)` and `Search(int id)` (both 1 param)
- **THEN** diagnostic `AGBR002` is reported

#### Scenario: Different param count overloads do not trigger AGBR002
- **WHEN** a `[JsExport]` interface has `Search(string q)` and `Search(string q, int limit)`
- **THEN** no AGBR002 diagnostic is reported

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

### Requirement: Generator SHALL report error for open generic interfaces
The generator SHALL report diagnostic error `AGBR006` when a `[JsExport]` or `[JsImport]` interface has open generic type parameters.

#### Scenario: Open generic interface triggers AGBR006
- **WHEN** `[JsExport] interface IRepository<T>` is compiled
- **THEN** diagnostic `AGBR006` is reported with message indicating open generic interfaces are not supported

#### Scenario: Closed generic interface does not trigger AGBR006
- **WHEN** a non-generic interface `[JsExport] interface IUserService` is compiled
- **THEN** no AGBR006 diagnostic is reported

### Requirement: Generator SHALL skip code emission for interfaces with diagnostics
When any `AGBR001`–`AGBR003` or `AGBR006` diagnostic is reported for an interface, the generator SHALL NOT emit any source files (BridgeRegistration, BridgeProxy, JS stub) for that interface. Other valid interfaces in the same compilation SHALL still be processed normally.

#### Scenario: One invalid and one valid interface in same compilation
- **WHEN** the compilation contains `[JsExport] IInvalid` with a generic method and `[JsExport] IValid` with normal methods
- **THEN** diagnostics are reported only for `IInvalid`
- **AND** source code is emitted only for `IValid`
