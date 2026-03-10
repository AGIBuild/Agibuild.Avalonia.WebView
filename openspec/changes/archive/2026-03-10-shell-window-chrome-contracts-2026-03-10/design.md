## Context

Current sample implementations duplicate shell/window behavior in app code:
- host-window transparency and web transparency are coordinated manually,
- drag regions are implemented per-window with fragile layering assumptions,
- shell metrics (top inset/titlebar safe area) are inferred in CSS,
- shell state sync often degrades to polling.

This creates regressions when custom chrome, platform behavior, or adapter timing changes.

Roadmap linkage:
- Phase 4 (4.2/4.5/4.6): shell lifecycle, shell template DX, hardening.
- Phase 10: theme sync bridge proved that typed shell state events are viable.
- Post-roadmap maintenance: unify these patterns into one stable contract.

## Goals / Non-Goals

**Goals**
- Provide a single typed shell-window contract for appearance + chrome metrics.
- Make shell synchronization event-driven (snapshot + stream), not polling-driven.
- Define deterministic drag-region behavior under custom chrome windows.
- Ensure transparency is applied as one host-validated state across window + web surface.
- Keep behavior mock-testable under contract tests.

**Non-Goals**
- Prescribing app UI/IA for settings pages.
- Adding AI/provider-specific workflow APIs.
- Guaranteeing identical visual effects across OS compositor implementations.

## Decisions

### D1: Introduce `shell-window-chrome` as a dedicated capability contract

Define a typed runtime contract that exposes:
- `GetWindowShellState()` snapshot
- `StreamWindowShellState()` updates
- `UpdateWindowShellSettings(...)` host-applied update path

The returned state represents **applied/effective** values (not merely requested values).

### D2: Host owns drag-region semantics

Drag behavior is defined by host shell policy, not by web overlay assumptions.
- Host provides drag strip/safe inset metrics.
- Host defines interactive exclusion zones precedence over drag initiation.
- Pointer handling remains deterministic regardless of WebView z-order quirks.

### D3: Unify transparency semantics into one applied state machine

`enableTransparency` and opacity are resolved by host into an applied state that includes:
- `isTransparencyEnabled`
- `isTransparencyEffective`
- `effectiveTransparencyLevel`
- applied alpha/opacity value

Web UI consumes this state; it does not independently infer host transparency.

### D4: Event-first sync contract

State synchronization uses stream/event channels with explicit deduplication semantics.
Polling is allowed only as recovery fallback in error states, not as primary path.

## Risks / Trade-offs

- **Platform divergence risk**: compositor differences can still produce perceptual variance.  
  *Mitigation*: contract exposes effective state + diagnostics to avoid hidden mismatch.
- **Contract surface growth**: new shell contract adds API footprint.  
  *Mitigation*: scope to shell/window concerns only; no business-domain methods.
- **Migration complexity**: samples currently use custom services.  
  *Mitigation*: provide incremental migration path (adapter wrappers over existing services).

## Testing Strategy

- **CT (MockAdapter/Runtime)**:
  - deterministic `Get + Stream + Update` semantics,
  - state deduplication and ordering,
  - drag-region precedence and exclusion behavior.
- **IT (platform adapters)**:
  - transparency on/off + opacity application reflects in effective state,
  - custom chrome drag strip works under top safe-area conditions,
  - no polling required for normal state propagation.
- **Template/Sample validation**:
  - shell-enabled sample consumes stream state without periodic polling timers.
