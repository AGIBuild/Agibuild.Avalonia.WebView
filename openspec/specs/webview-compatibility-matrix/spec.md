## Requirements

### Requirement: Compatibility matrix exists and is versioned
The repository SHALL maintain a versioned WebView compatibility matrix that defines:
- supported platforms (Windows, macOS/iOS, Android, Linux)
- supported modes (Embedded, Dialog, Auth)
- capability coverage by support level (Baseline vs Extended vs Not Supported)
- acceptance criteria per capability (CT and/or IT requirements)

#### Scenario: Matrix document is present
- **WHEN** a contributor inspects the repository documentation
- **THEN** a compatibility matrix can be found and includes platforms, modes, support levels, and acceptance criteria

### Requirement: Baseline capabilities are falsifiable
For each Baseline capability listed in the matrix, the matrix SHALL identify at least one deterministic Contract Test (CT) scenario that validates the baseline semantics.

#### Scenario: Each baseline capability maps to CT
- **WHEN** a capability is marked as Baseline in the matrix
- **THEN** the matrix includes CT acceptance criteria for that capability

### Requirement: Extended capabilities document platform differences
For each capability marked as Extended with platform differences, the repository SHALL document the difference using a standard "platform difference" entry including:
- affected platforms/modes
- user-visible behavior
- security implications (if any)
- test impact (CT conditionalization or IT substitution)

#### Scenario: Platform differences are documented
- **WHEN** a capability is marked as Extended with a platform warning
- **THEN** a platform difference entry exists with behavior, security implications, and test impact

### Requirement: Linux embedded mode is explicitly out of baseline scope
The matrix SHALL explicitly state that Linux Embedded mode is not part of Baseline support and that Linux support is provided via Dialog mode only.

#### Scenario: Linux embedded is not promised
- **WHEN** the matrix is reviewed for Linux support
- **THEN** it does not claim Baseline support for Embedded mode on Linux

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

