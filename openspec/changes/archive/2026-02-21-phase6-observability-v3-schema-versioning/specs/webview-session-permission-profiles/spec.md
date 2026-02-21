## ADDED Requirements

### Requirement: Session permission profile diagnostics expose explicit schema version
`WebViewSessionPermissionProfileDiagnosticEventArgs` SHALL expose a deterministic `DiagnosticSchemaVersion` field, and runtime emission SHALL set it for every profile diagnostic event.

#### Scenario: Profile diagnostics include schema version across evaluation outcomes
- **WHEN** session/profile diagnostics are emitted for resolved, denied, or fallback permission evaluations
- **THEN** each diagnostic event includes `DiagnosticSchemaVersion` matching the runtime contract constant
