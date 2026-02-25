## ADDED Requirements

### Requirement: Compatibility declarations SHALL distinguish unsupported from undeclared
Platform compatibility declarations SHALL explicitly mark unsupported platform scope with deterministic token (`n/a`) instead of omitting platform keys.

#### Scenario: Unsupported mobile scope is explicitly declared
- **WHEN** compatibility/governance artifacts are generated for shell capabilities
- **THEN** iOS/Android scope is explicitly represented as `n/a` where executable evidence is not yet available
