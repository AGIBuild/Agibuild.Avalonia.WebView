## Context

The project is at v0.1.21-preview with all core features complete (G1–G4, E1–E3). Phase 8 added significant public API surface (deep-link registration, SPA hot update, Bridge V2 capabilities). The last API inventory was generated 2026-02-17 — before these additions. Two `[Experimental]` markers remain: `AGWV001` (ICookieManager) and `AGWV005` (EnvironmentRequestedEventArgs).

The existing `API_SURFACE_REVIEW.md` identified 5 action items for 1.0, of which one is already resolved (AGWV004 graduated).

## Goals / Non-Goals

**Goals:**
- Complete the 1.0 API freeze by regenerating the inventory and resolving all open action items
- Promote commonly-used features to the `IWebView` interface for consistency
- Decide on remaining `[Experimental]` markers
- Produce a timestamped freeze record

**Non-Goals:**
- Adding new public APIs beyond interface promotions
- Removing any existing public APIs (would be breaking)
- Publishing packages (M9.3)

## Decisions

### D1: IWebView interface promotion strategy

**Choice**: Promote ZoomFactor, FindInPage, PreloadScript, and ContextMenuRequested to `IWebView` as additive (non-breaking) changes.

**Rationale**: These are commonly-used features that consumers expect on the abstraction. Adding members to an interface is technically breaking for implementers, but all implementations are internal to the framework. External consumers only consume `IWebView`, not implement it.

### D2: Experimental attribute resolution

**Choice**:
- `AGWV001` (ICookieManager): **Keep** — cookie management still has platform gaps (Android returns null). Graduating it implies full cross-platform support.
- `AGWV005` (EnvironmentRequestedEventArgs): **Keep** — still a placeholder with no concrete implementation.

**Rationale**: Experimental markers are a signal to consumers that the API may change. Only graduate when the contract is stable across all platforms.

### D3: EnableSpaHosting on WebView control

**Choice**: Defer to post-1.0. The current pattern (accessed via `WebViewCore`) works and adding it to the Avalonia control requires design decisions about property vs method and lifecycle timing.

**Rationale**: Low consumer demand, and the workaround via WebViewEnvironmentOptions is adequate.

### D4: API inventory regeneration

**Choice**: Use the existing nuke `Pack` target which runs the API surface generator, then copy the output.

**Rationale**: The inventory generator already exists and produces consistent output.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| IWebView promotion breaks third-party implementers | All implementations are internal; no external adapter implementations exist |
| Keeping AGWV001 Experimental may confuse adopters | Document in API review that cookie management is platform-dependent |
| Phase 8 APIs may have naming inconsistencies | Explicit audit step in tasks |

## Testing Strategy

- **CT**: Existing contract tests cover all IWebView members; no new tests needed for additive interface changes
- **Governance**: `nuke ReleaseOrchestrationGovernance` must pass after changes
- **Validation**: `openspec validate --all --strict` must pass
