## Purpose
Define baseline rules for pre-1.0 API surface review, focusing on consistency, naming quality, and actionable remediation.
## Requirements
### Requirement: Public type inventory is complete
The API review SHALL include an inventory of all public types and their exposed members.

#### Scenario: Reviewing current public surface
- **WHEN** a release review is executed
- **THEN** all public types and member signatures are listed in the review output

### Requirement: IWebView consistency is assessed
The API review SHALL identify gaps between `IWebView` contracts and concrete implementations.

#### Scenario: Comparing contract and implementation
- **WHEN** the review checks `IWebView` and implementations
- **THEN** missing members, signature mismatches, and semantic drift are recorded

### Requirement: Naming conventions are validated
The API review SHALL validate .NET naming conventions for public APIs.

#### Scenario: Detecting naming issues
- **WHEN** public symbols are analyzed
- **THEN** violations are captured with concrete rename suggestions

### Requirement: Action items are prioritized
The API review SHALL produce prioritized action items with breaking-change impact classification.

#### Scenario: Producing remediation plan
- **WHEN** issues are identified
- **THEN** each item includes priority, owner, and breaking/non-breaking classification

### Requirement: Async-boundary API audit is explicit and actionable
API surface review SHALL include explicit async-boundary audit items for runtime and control APIs where sync/async ambiguity exists.

#### Scenario: Audit captures native-handle boundary
- **WHEN** API surface review runs for the current release train
- **THEN** it records migration status for native handle access toward async-first contracts

### Requirement: Public event subscription lifecycle is audited
API surface review SHALL verify that control-level event subscriptions behave deterministically before and after core attach.

#### Scenario: Review captures pre-attach subscription semantics
- **WHEN** event forwarding APIs are audited
- **THEN** the report includes pass/fail outcomes for pre-attach subscribe/unsubscribe behavior

### Requirement: Blocking-wait exceptions are documented with owner and rationale
API surface review SHALL maintain an allowlist record for production blocking waits, including owning component and audited justification.

#### Scenario: New blocking wait is proposed
- **WHEN** a new `GetAwaiter().GetResult()` call is introduced in production source
- **THEN** review fails unless allowlist entry and rationale are added in the same change

### Requirement: Async and global-coupling closure is tracked in API review
API surface review SHALL track remaining sync wrappers and global mutable coupling points related to async-boundary behavior, with explicit closure status per item.

#### Scenario: Release API audit is executed
- **WHEN** API review runs for a release candidate
- **THEN** each boundary-coupling item is marked as closed, accepted risk, or scheduled removal with owner and target milestone

### Requirement: API review maps public boundaries to executable evidence
API surface review MUST include traceability from boundary-sensitive public APIs to executable contract/runtime tests.

#### Scenario: Boundary API has no runtime evidence mapping
- **WHEN** audit checks API-to-test traceability
- **THEN** review fails until at least one runtime evidence path is linked for that API boundary

