## MODIFIED Requirements

### Requirement: Generator SHALL report error for CancellationToken parameters
~~The source generator SHALL emit diagnostic `AGBR004` with `DiagnosticSeverity.Error` when a bridge method has a `System.Threading.CancellationToken` parameter.~~

CancellationToken is now supported. The AGBR004 diagnostic SHALL be removed.

#### Scenario: CancellationToken parameter on JsExport method
- **WHEN** a `[JsExport]` interface declares `Task<string> Process(string input, CancellationToken ct)`
- **THEN** the generator does NOT report any diagnostic
- **AND** source code is emitted normally
