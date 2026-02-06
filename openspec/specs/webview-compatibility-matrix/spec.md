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

