## Why

Phase 9 (GA Release Readiness) requires a frozen API surface before committing to semver 1.0.0. The last API audit was on 2026-02-17, before Phase 8 added deep-link registration, SPA hot update, and Bridge V2 capabilities. The public surface needs re-inventory, consistency validation, and explicit freeze/deprecation decisions for all remaining `[Experimental]` markers. This is M9.2 in the ROADMAP.

## What Changes

- Regenerate `API_SURFACE_INVENTORY.release.txt` with current public types
- Audit Phase 8 additions (DeepLink, SPA hot update, Bridge V2 generated signatures) for naming consistency
- Resolve remaining `[Experimental]` markers: graduate `AGWV001` (ICookieManager) or keep, keep `AGWV005` (EnvironmentRequestedEventArgs)
- Promote feature members to `IWebView` interface: ZoomFactor, FindInPage, PreloadScript, ContextMenuRequested
- Update `API_SURFACE_REVIEW.md` with 1.0 freeze status and timestamp
- Update ROADMAP M9.1 → Done, M9.2 → Done

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `api-surface-review`: Add 1.0 freeze inventory requirement with Phase 8 coverage and Experimental resolution

## Non-goals

- Adding new runtime features or APIs
- npm package publication (M9.3)
- Performance benchmarking (M9.4)

## Impact

- `src/Agibuild.Fulora.Core/WebViewContracts.cs` — IWebView interface members promoted; Experimental attributes resolved
- `docs/API_SURFACE_REVIEW.md` — Updated with 1.0 freeze audit
- `docs/API_SURFACE_INVENTORY.release.txt` — Regenerated
- `openspec/ROADMAP.md` — M9.1 → Done, M9.2 → Done
