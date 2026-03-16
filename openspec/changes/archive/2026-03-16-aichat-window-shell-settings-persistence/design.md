## Context

AI chat sample uses `WindowShellService` for theme/transparency/chrome settings, but the service state is process-memory only. Web UI saves settings via bridge call, then receives in-process state, but there is no durable persistence boundary. Users perceive this as "saved then reverted" across relaunches, and troubleshooting titlebar issues is harder without stable persisted state.

This design is sample-scoped and consistent with architecture principles: explicit contracts, deterministic host-owned state, and testable behavior.

## Goals / Non-Goals

**Goals:**
- Persist sample appearance settings to local disk in desktop host.
- Load persisted settings at startup before web shell initialization.
- Persist normalized applied settings after every successful update call.

**Non-Goals:**
- No global persistence behavior added to framework-level `WindowShellService`.
- No schema/version migration complexity beyond simple JSON compatibility.
- No persistence for unrelated AI model download/runtime diagnostics state.

## Decisions

### Decision 1: Persistence stays in sample adapter layer
- **Choice**: Add a sample-local settings store + bridge adapter wrapper.
- **Why**: Keeps framework core generic while solving sample UX issue deterministically.
- **Alternatives considered**:
  - Put persistence into `WindowShellService`: rejected (broad framework behavior change).
  - Persist in web localStorage only: rejected (host is source of truth for applied window appearance).

### Decision 2: Apply persisted state before SPA bootstrap
- **Choice**: Load from disk and call `UpdateWindowShellSettings` before exposing bridge services.
- **Why**: Prevents first-frame mismatch between default and user-configured appearance.
- **Alternatives considered**:
  - Lazy apply after web mount: rejected due to visible flicker and race.

### Decision 3: Save normalized applied settings, not raw draft
- **Choice**: Persist `updated.Settings` returned by `WindowShellService`.
- **Why**: Keeps disk state aligned with clamped/normalized canonical values.
- **Alternatives considered**:
  - Save incoming payload directly: rejected (may preserve invalid/un-normalized values).

## Risks / Trade-offs

- **[Corrupt settings file]** parse failure could break load path → **Mitigation**: fail-safe fallback to defaults.
- **[Path portability]** app data path differs by OS → **Mitigation**: use `Environment.SpecialFolder.ApplicationData`.
- **[Sample drift]** future refactors bypass adapter persistence wrapper → **Mitigation**: add regression test assertions on wiring.

## Migration Plan

1. Add sample-local JSON settings store class in desktop project.
2. Wire startup load/apply before bootstrap.
3. Update bridge adapter to persist post-update settings.
4. Add regression assertions and run unit tests.

## Open Questions

- Should future sample docs mention exact local file path per OS, or keep it implementation detail?
