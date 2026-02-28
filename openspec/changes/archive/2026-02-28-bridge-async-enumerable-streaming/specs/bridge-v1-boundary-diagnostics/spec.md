## MODIFIED Requirements

### Requirement: Generator SHALL report error for IAsyncEnumerable return type
~~The source generator SHALL emit diagnostic `AGBR005` with `DiagnosticSeverity.Error` when a bridge method returns `IAsyncEnumerable<T>`.~~

IAsyncEnumerable is now supported. The AGBR005 diagnostic SHALL be removed.

#### Scenario: IAsyncEnumerable return type on JsExport method
- **WHEN** a `[JsExport]` interface declares `IAsyncEnumerable<int> StreamData()`
- **THEN** the generator does NOT report any diagnostic
- **AND** source code is emitted normally with streaming handler
