## ADDED Requirements

### Requirement: Async-boundary behavior requires runtime evidence
For APIs with UI-thread affinity semantics, release validation SHALL include runtime evidence in addition to contract/mocked evidence.

#### Scenario: Boundary-sensitive API is changed
- **WHEN** a change modifies boundary-sensitive runtime behavior
- **THEN** release validation includes passing runtime automation evidence for off-thread invocation and lifecycle boundary handling

### Requirement: Environment option isolation remains instance-scoped under runtime paths
Runtime semantics MUST preserve instance-scoped environment options and MUST NOT implicitly mutate global environment options as a side effect of instance construction/use.

#### Scenario: Multiple instances use different environment options
- **WHEN** two runtime instances are created with different options in the same process
- **THEN** each instance preserves its own options and global environment options remain unchanged unless explicitly set by the caller
