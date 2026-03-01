## 1. IWebView interface promotion

- [x] 1.1 Promote ZoomFactor, ZoomFactorChanged, FindInPageAsync, StopFindInPage, AddPreloadScript, RemovePreloadScript, and ContextMenuRequested to the IWebView interface in WebViewContracts.cs (Deliverable: api-surface-review; Acceptance: members exist on IWebView with correct signatures matching WebViewCore).
- [x] 1.2 Verify build succeeds and all existing tests pass after interface promotion (Deliverable: api-surface-review; Acceptance: dotnet build + nuke Test pass with zero new failures).

## 2. API inventory regeneration

- [x] 2.1 Regenerate API_SURFACE_INVENTORY.release.txt from a Release build covering all public assemblies (Deliverable: api-surface-review; Acceptance: inventory includes Phase 8 types like DeepLinkActivationEnvelope, SpaAssetHotUpdateService).
- [x] 2.2 Audit Phase 8 public type naming against .NET conventions (PascalCase, I* prefix, *EventArgs suffix) and fix any violations (Deliverable: api-surface-review; Acceptance: zero naming convention violations in Phase 8 types).

## 3. Experimental attribute resolution

- [x] 3.1 Document AGWV001 (ICookieManager) decision: keep experimental with justification (platform gaps) in API_SURFACE_REVIEW.md (Deliverable: api-surface-review; Acceptance: AGWV001 has explicit status and rationale in review doc).
- [x] 3.2 Document AGWV005 (EnvironmentRequestedEventArgs) decision: keep experimental with justification (placeholder) in API_SURFACE_REVIEW.md (Deliverable: api-surface-review; Acceptance: AGWV005 has explicit status and rationale in review doc).

## 4. API review document update

- [x] 4.1 Update API_SURFACE_REVIEW.md with 1.0 freeze timestamp, resolved action items, Phase 8 additions audit, and Experimental resolution (Deliverable: api-surface-review; Acceptance: document has 1.0 freeze date and all action items resolved or deferred with justification).

## 5. ROADMAP and validation

- [x] 5.1 Update ROADMAP.md M9.1 → Done, M9.2 → Done (Deliverable: api-surface-review; Acceptance: ROADMAP milestones reflect completion).
- [x] 5.2 Run nuke Test and verify all targets pass (Deliverable: api-surface-review; Acceptance: build succeeds, all tests green).
