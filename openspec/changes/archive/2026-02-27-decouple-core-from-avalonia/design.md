## Context

Current contracts in `Core`/`Runtime`/`Adapters.Abstractions` expose Avalonia types (`Avalonia.Platform.IPlatformHandle`, Avalonia dispatcher binding). This violates the architecture principle in `docs/agibuild_webview_design_doc.md` that runtime semantics should be contract-driven and platform-isolated.

Project alignment:
- Goals: strengthens **G4** (contract-first testability) and supports long-term dual-path adoption defined in `PROJECT.md`.
- Roadmap: this is not a direct Phase 6 (`M6.1-M6.3`) governance deliverable, but is a prerequisite-quality refactor to keep core contracts phase-neutral and prevent framework-coupled semantic drift.

Stakeholders:
- Runtime/API maintainers (contract stability)
- Adapter maintainers (attach/handle semantics)
- Template and package consumers (dependency graph clarity)

## Goals / Non-Goals

**Goals:**
- Remove Avalonia type dependencies from `Agibuild.Fulora.Core`, `Agibuild.Fulora.Runtime`, and `Agibuild.Fulora.Adapters.Abstractions`.
- Keep `WebViewCore` as the single semantic owner, with host-specific lifecycle/dispatcher wiring moved to host layer.
- Preserve existing platform adapter capabilities and behavioral semantics (navigation, bridge, features), while changing boundary types.
- Enforce this boundary through tests and package dependency checks.

**Non-Goals:**
- Introduce a new non-Avalonia host implementation in this change.
- Keep legacy Avalonia-typed API paths in parallel.
- Re-scope platform feature behavior (PDF, cookies, permission, shell capabilities).

## Decisions

### D1. Host-neutral handle contract
- **Decision:** Introduce framework-neutral handle abstractions in core contracts:
  - `INativeHandle` (e.g., `nint Handle`, `string HandleDescriptor`)
  - `INativeHandle<TTag>` optional typed extension for platform-specific tags
- Replace all core/runtime public uses of `IPlatformHandle` with `INativeHandle`.
- **Rationale:** removes compile-time dependency on Avalonia from runtime semantic layers.
- **Alternatives considered:**
  1. Keep `IPlatformHandle` in core, only hide implementation in runtime (rejected: dependency leak remains).
  2. Use raw `nint` everywhere (rejected: loses descriptor/type semantics and contracts readability).

### D2. Host boundary split
- **Decision:** Move Avalonia-specific implementations to host package/layer (`Agibuild.Fulora.Avalonia`):
  - `WebView` control
  - `AvaloniaWebDialog`/factory
  - `AppBuilderExtensions`
  - Avalonia `IWebViewDispatcher` implementation
- **Rationale:** keeps host lifecycle concerns separate from runtime semantics.
- **Alternatives considered:**
  1. Keep same assembly with internal namespace split (rejected: package dependency still coupled).
  2. Full multi-host plugin system now (rejected: larger scope than required).

### D3. Adapter boundary update without dual-path fallback
- **Decision:** Change adapter SPI to consume `INativeHandle` only (no old/new dual signatures).
- **Rationale:** aligns with no-legacy-compatibility policy and avoids long-term maintenance split.

### D4. Packaging policy
- **Decision:** remove `Avalonia` package references from:
  - `Agibuild.Fulora.Core`
  - `Agibuild.Fulora.Runtime`
  - `Agibuild.Fulora.Adapters.Abstractions`
- Keep Avalonia dependency only in host-specific package.
- **Rationale:** NuGet dependency graph must reflect architectural boundary, not just source layout.

### D5. Testing strategy (CT/IT/Automation)
- Contract tests: verify no framework types are exposed by core contract APIs.
- Unit tests: adapt mocks to `INativeHandle`; preserve navigation/bridge semantics.
- Integration tests: validate adapter attach/detach and typed native-handle retrieval.
- Automation governance: add dependency-surface assertion to prevent re-introducing Avalonia in core/runtime projects.

## Risks / Trade-offs

- **[Breaking API surface]** → Mitigation: explicit major-version change note + spec-level breaking requirements.
- **[Wide cross-project touch]** → Mitigation: staged implementation (contracts → runtime/adapters → host layer → template/tests).
- **[Packaging regression]** → Mitigation: CI check for transitive dependency surface and package smoke tests.
- **[Semantic regression during boundary swap]** → Mitigation: keep runtime behavior assertions identical; only boundary type changes.

## Migration Plan

1. Introduce new host-neutral contracts and update core interfaces.
2. Update runtime and adapter abstractions to new handle/dispatcher boundary.
3. Move Avalonia concrete host types to dedicated host layer/package and rewire DI/template usage.
4. Update tests (CT/IT/automation) to new contracts and dependency invariants.
5. Validate with `nuke Test`, `nuke Coverage`, `openspec validate --all --strict`.

Rollback strategy:
- Revert the change set at package version boundary (single release unit rollback).
- No runtime dual-path fallback is introduced.

## Open Questions

- Should the host package name remain under `Agibuild.Fulora` namespace or become explicit (`.Host.Avalonia`)?
- Should typed platform handles stay in `Core` or move to a dedicated interop contracts assembly in a follow-up?
- Do we enforce a strict API analyzer rule to block any future framework namespace references in core/runtime?
