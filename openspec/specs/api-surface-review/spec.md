## Purpose
Define baseline rules for pre-1.0 API surface review, focusing on consistency, naming quality, and actionable remediation.
## Requirements
### Requirement: API surface review outputs are stored in a canonical, reviewable location
Each API surface review execution SHALL produce a human-reviewable report and SHALL store it at a canonical path within the repository so it can be reviewed by pull request diff.

#### Scenario: Review output is discoverable
- **WHEN** a contributor performs a pre-1.0 API surface review for the release train
- **THEN** the report SHALL be updated in `docs/API_SURFACE_REVIEW.md` (or an explicitly referenced canonical successor file)

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
API surface review MUST include traceability pointers from boundary-sensitive public APIs to executable contract/runtime tests, or explicitly record the gap as an actionable item.

#### Scenario: Boundary API has no runtime evidence mapping
- **WHEN** audit checks API-to-test traceability
- **THEN** review fails until at least one runtime evidence path is linked for that API boundary OR a tracked action item is recorded to add such evidence

### Requirement: 1.0 API freeze inventory is complete and versioned
API surface review for the 1.0 release SHALL produce a complete inventory of all public types and members, stored in a canonical, diff-reviewable location, with explicit freeze/deprecation status per item. The inventory SHALL include all Phase 8 additions (deep-link registration, SPA hot update, Bridge V2 generated signatures).

#### Scenario: 1.0 freeze inventory is generated
- **WHEN** pre-release API review runs for the 1.0 release train
- **THEN** `docs/API_SURFACE_REVIEW.md` is updated with a timestamped 1.0 freeze inventory listing all public types and their members
- **AND** `docs/API_SURFACE_INVENTORY.release.txt` is regenerated from a Release build

#### Scenario: Phase 8 additions are included in freeze inventory
- **WHEN** the 1.0 freeze inventory is generated
- **THEN** it SHALL include public types from deep-link registration (`DeepLinkActivationEnvelope`, `IDeepLinkRegistrationService`, etc.), SPA hot update (`SpaAssetHotUpdateService`), and Bridge V2 capability changes

#### Scenario: Experimental API is explicitly resolved
- **WHEN** a public API carries `[Experimental]` at freeze time
- **THEN** the review records one of: (a) graduated (attribute removed), (b) kept experimental with justification, or (c) marked `[Obsolete]` with migration guidance

#### Scenario: IWebView interface gap is resolved
- **WHEN** the 1.0 freeze audit checks IWebView consistency
- **THEN** commonly-used feature members (ZoomFactor, FindInPage, PreloadScript, ContextMenuRequested) SHALL be present on `IWebView` or explicitly documented as deferred with justification

### Requirement: Naming convention compliance is enforced for 1.0 surface
All public types, methods, and properties in the 1.0 release SHALL comply with .NET naming guidelines. Non-compliant names introduced during preview SHALL be renamed or marked obsolete.

#### Scenario: Convention violations are listed with remediation
- **WHEN** naming audit runs against the 1.0 public surface
- **THEN** each violation includes the current name, suggested rename, and breaking-change classification

