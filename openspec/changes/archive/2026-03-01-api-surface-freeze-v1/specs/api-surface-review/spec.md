## MODIFIED Requirements

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
