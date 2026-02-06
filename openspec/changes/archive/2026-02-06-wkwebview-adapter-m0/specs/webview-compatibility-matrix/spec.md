## ADDED Requirements

### Requirement: Compatibility matrix records macOS WKWebView M0 acceptance criteria
The compatibility matrix SHALL include an entry for macOS (WKWebView) that documents M0 coverage for Embedded mode navigation and minimal script/message-bridge behavior.

The matrix entry SHALL identify acceptance criteria using both CT and IT as applicable:
- CT: contract semantics scenarios that are platform-independent and deterministic
- IT: macOS-only smoke scenarios that validate WKWebView behavior for native navigation interception/correlation

#### Scenario: Matrix includes macOS WKWebView navigation acceptance criteria
- **WHEN** a contributor inspects the compatibility matrix for macOS (WKWebView) Embedded mode
- **THEN** it lists acceptance criteria covering link click, 302 redirect correlation, `window.location`, and cancellation (`Cancel=true`)

#### Scenario: Matrix includes macOS WKWebView minimal script/bridge acceptance criteria
- **WHEN** a contributor inspects the compatibility matrix for macOS (WKWebView) Embedded mode
- **THEN** it lists acceptance criteria covering minimal script execution and WebMessage bridge receive behavior
