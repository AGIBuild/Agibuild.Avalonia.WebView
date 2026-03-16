## Context

The `avalonia-ai-chat` sample combines host-window chrome metrics from C# with a React shell. Today the sample allows two independent fallback semantics: desktop drag region is configured at `28` DIP while web fallback titlebar height uses `44`, and title text is not enforced as a cross-layer contract between `MainWindow` and web header. This creates first-render layout drift before bridge state arrives and inconsistent user-facing title semantics.

Stakeholders:
- Sample consumers using AI chat as a reference architecture
- Maintainers of shell-window-chrome integration contracts
- QA/governance tests validating sample behavior

This is a focused post-Phase-11 DX hardening change aligned with `PROJECT.md` goals **G2** and **G4**, and with roadmap quality maintenance after ecosystem delivery.

## Goals / Non-Goals

**Goals:**
- Define a deterministic sample-level contract for title text and titlebar metrics across host and web layers.
- Remove fallback mismatch (`44` vs `28`) by aligning fallback value with host drag region.
- Add regression checks so future refactors cannot silently reintroduce drift.

**Non-Goals:**
- No change to bridge transport or shell-window-chrome framework internals.
- No platform-specific redesign of native window decorations.
- No runtime feature expansion beyond consistency correction.

## Decisions

### Decision 1: Single title semantics across host and web
- **Choice**: Keep one canonical sample title string and ensure host title and web header title represent the same product identity.
- **Why**: Avoids split branding and removes ambiguity for users and docs.
- **Alternatives considered**:
  - Keep independent strings: rejected due to inevitable drift.
  - Remove host title entirely: rejected because native shell still exposes window title in OS-level UI.

### Decision 2: Fallback titlebar height equals host drag region
- **Choice**: Use `28` as web fallback titlebar height to match desktop `DragRegionHeight = 28`.
- **Why**: Bridge-not-ready windows still need predictable layout; fallback must match host geometry contract.
- **Alternatives considered**:
  - Keep `44`: rejected as it violates host metric contract and causes top-area misalignment.
  - Dynamic CSS-only computation without host value: rejected because host remains source of truth.

### Decision 3: Keep bridge metrics as primary, fallback as bootstrap-only
- **Choice**: Continue consuming `appearance.chromeMetrics.titleBarHeight` when available; fallback applies only before state hydration.
- **Why**: Preserves architecture from `shell-window-chrome` while fixing bootstrap behavior.
- **Alternatives considered**:
  - Hardcode fixed titlebar forever: rejected because host metrics can vary by platform/future settings.

### Decision 4: Add regression verification in unit tests
- **Choice**: Extend unit tests that already validate AI chat shell metric wiring to assert fallback consistency markers.
- **Why**: Supports **G4** by making the consistency contract testable without real browser runtime.
- **Alternatives considered**:
  - Rely on manual sample run: rejected as nondeterministic and easy to miss in CI.

## Risks / Trade-offs

- **[Hardcoded value coupling]** `28` is sample-specific and may need updates if host defaults change → **Mitigation**: keep fallback and host drag region values co-located in tests and spec constraints.
- **[Perceived visual delta on existing screenshots]** top spacing may change in bridge-not-ready frame → **Mitigation**: acceptable because new layout matches true host drag geometry.
- **[Spec drift across sample docs]** hidden references may still describe old behavior → **Mitigation**: scope includes spec delta and tests as canonical guardrails.

## Migration Plan

1. Add OpenSpec proposal/design/spec/tasks for `fix-aichat-titlebar-consistency`.
2. Update sample host title and web fallback height sources.
3. Add/update unit tests validating titlebar bootstrap contract markers.
4. Run targeted `.NET` unit tests and web build for the sample.
5. If unexpected regression appears, rollback this change set as a unit.

## Open Questions

- Should the title string be centralized into a shared constant in a later refactor, or is per-layer literal acceptable for sample-scale governance?
