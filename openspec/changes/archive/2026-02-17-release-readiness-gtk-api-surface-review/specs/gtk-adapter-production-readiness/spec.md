## ADDED Requirements

### Requirement: GTK DevTools toggle is wired to native inspector APIs
When the GTK/WebKitGTK adapter runs with DevTools enabled, the adapter SHALL wire the DevTools toggle APIs to the native WebKitGTK inspector so developers can open and close the inspector deterministically.

#### Scenario: DevTools can be opened on GTK
- **WHEN** a GTK-backed WebView is created with `EnableDevTools=true` and the application calls `OpenDevTools()`
- **THEN** the WebKitGTK inspector UI SHALL be shown for that WebView instance

#### Scenario: DevTools can be closed on GTK
- **WHEN** DevTools are currently open for a GTK-backed WebView and the application calls `CloseDevTools()`
- **THEN** the WebKitGTK inspector UI SHALL be closed for that WebView instance

### Requirement: GTK/Linux runtime smoke validation lane exists
The system SHALL provide a Linux runtime smoke validation lane for the GTK adapter to reduce “untested Linux” risk as part of Phase 3 GA readiness (ROADMAP Phase 3 Deliverable 3.5).

#### Scenario: Linux smoke lane executes on CI Linux runners
- **WHEN** CI runs the governed `Ci` target on Linux runners
- **THEN** the runtime automation report SHALL include an executed GTK/Linux lane OR mark it as skipped with an explicit reason

### Requirement: GTK/Linux smoke suite covers core WebView flows end-to-end
The GTK/Linux smoke suite MUST validate a minimal, release-critical set of end-to-end flows on a real WebKitGTK-backed WebView:
- navigation start and completion
- cancellation via `NavigationStarted.Cancel`
- minimal script execution
- minimal WebMessage receive path (when enabled by configuration)

#### Scenario: Smoke suite validates cancellation
- **WHEN** a GTK smoke test starts a navigation and cancels it via `NavigationStarted.Cancel=true`
- **THEN** the navigation SHALL complete as `Canceled` and SHALL NOT reach a successful completion status

#### Scenario: Smoke suite validates script execution
- **WHEN** a GTK smoke test calls `InvokeScriptAsync` against a loaded page
- **THEN** the call SHALL return the expected value without timing sleeps

### Requirement: GTK/Linux smoke evidence is published and auditable
CI SHALL publish machine-readable outputs for the GTK/Linux smoke lane (results + diagnostics) so failures are actionable and regressions are detectable.

#### Scenario: CI publishes artifacts for review
- **WHEN** the GTK/Linux smoke lane completes in CI
- **THEN** the run SHALL publish test results and lane report artifacts with enough context to reproduce locally

