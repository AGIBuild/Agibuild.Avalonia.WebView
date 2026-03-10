## Why

Recent `avalonia-ai-chat` iterations exposed the same class of issues repeatedly: window dragging in custom chrome areas, host/window transparency not matching web-side settings, top safe-area/layout drift, and UI lag caused by polling-based synchronization. These concerns are not sample-specific business logic; they are shell/runtime responsibilities that should be provided once by Fulora and reused by all apps.

This change aligns with:
- **G1** (typed host-web contract) by defining a typed shell contract.
- **G2** (first-class hybrid hosting) by making window chrome and transparency predictable.
- **G4** (contract-driven testability) by moving ad-hoc sample logic into testable runtime contracts.

Roadmap alignment:
- Extends **Phase 4 / Deliverables 4.2, 4.5, 4.6** (multi-window lifecycle, shell DX presets, hardening).
- Consolidates lessons from **Phase 10 "theme sync bridge"** into a generalized shell-window contract.
- Fits current **post-roadmap maintenance** as platform-hardening and API consistency work.

## What Changes

- Add a new capability spec: **`shell-window-chrome`**.
- Standardize a host-provided shell state contract that includes:
  - effective theme mode,
  - transparency enabled/effective state,
  - applied opacity,
  - effective transparency level,
  - chrome layout metrics (titlebar/top inset, safe insets).
- Require event-stream based state sync (snapshot + stream), replacing polling-based shell synchronization.
- Define host-owned drag region semantics for custom chrome windows:
  - drag strip behavior,
  - deterministic exclusion for interactive zones.
- Define end-to-end transparency semantics so host window and web surface are updated as one applied state.

## Capabilities

### New Capabilities

- `shell-window-chrome`: deterministic shell/window chrome + appearance synchronization contract.

### Modified Capabilities

- `webview-shell-experience`: references shell-window capability as the canonical window/chrome state source for shell-enabled templates.

## Non-goals

- Defining app-specific settings UI (tabs/buttons/copywriting).
- Embedding AI runtime flows (Ollama install/download UX) into framework core.
- Introducing platform-specific visual style presets in core APIs.
- Replacing existing app-level services immediately; migration is incremental.

## Impact

- New spec: `openspec/specs/shell-window-chrome/spec.md` (via change delta).
- Host/runtime implementations can converge on one contract instead of per-sample custom services.
- Templates and samples can consume a common shell state stream with less glue code and fewer regressions.
