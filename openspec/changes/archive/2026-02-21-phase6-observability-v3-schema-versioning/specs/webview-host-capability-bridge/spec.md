## ADDED Requirements

### Requirement: Host capability diagnostics expose explicit schema version
`WebViewHostCapabilityDiagnosticEventArgs` SHALL expose a deterministic `DiagnosticSchemaVersion` field, and runtime emission SHALL set it for every diagnostic event.

#### Scenario: Allow/deny/failure diagnostics carry schema version
- **WHEN** host capability diagnostic events are emitted for allow, deny, or failure outcomes
- **THEN** each event includes a non-zero `DiagnosticSchemaVersion` matching the runtime contract constant
