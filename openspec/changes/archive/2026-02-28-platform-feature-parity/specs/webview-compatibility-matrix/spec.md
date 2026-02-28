## MODIFIED Requirements

### Requirement: Compatibility matrix documents GTK PrintToPdf as unsupported
The compatibility matrix SHALL mark GTK/Linux PrintToPdf as ❌ (Not Supported) with a note explaining WebKitGTK lacks a PDF export API.

#### Scenario: Matrix shows GTK PrintToPdf status
- **WHEN** a developer consults the compatibility matrix for PrintToPdf
- **THEN** GTK/Linux is marked ❌ with explanation

### Requirement: Compatibility matrix documents macOS DevTools toggle limitation
The compatibility matrix SHALL mark macOS OpenDevTools/CloseDevTools as ⚠️ (No-op) with a note that the Web Inspector is available via right-click when EnableDevTools is set.

#### Scenario: Matrix shows macOS DevTools toggle status
- **WHEN** a developer consults the compatibility matrix for DevTools
- **THEN** macOS shows ⚠️ with note about right-click access

### Requirement: Compatibility matrix documents IAsyncPreloadScriptAdapter as Windows-only
The compatibility matrix SHALL include an IAsyncPreloadScriptAdapter row marking Windows as ✅ and all other platforms as ❌ (fallback to sync IPreloadScriptAdapter).

#### Scenario: Matrix shows async preload cross-platform status
- **WHEN** a developer consults the compatibility matrix for async preload
- **THEN** only Windows is marked ✅, others show ❌ with fallback note
