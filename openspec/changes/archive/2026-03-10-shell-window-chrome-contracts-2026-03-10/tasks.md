## 1. Shell Window Contract Surface (Phase 4 / Deliverable 4.2)

- [x] 1.1 Define typed DTOs for shell window state/settings (effective transparency flags, opacity, top inset/safe insets).  
  Acceptance: DTOs compile in core/bridge assemblies, and include all fields required by `shell-window-chrome` spec scenarios.
- [x] 1.2 Define service contract (`GetWindowShellState`, `UpdateWindowShellSettings`, `StreamWindowShellState`) and expose through typed bridge.  
  Acceptance: contract is callable from JS via generated types and compiles without reflection-based runtime binding.

## 2. Runtime State Machine & Dedup (Phase 4 / Deliverable 4.6)

- [x] 2.1 Implement runtime/applied-state builder that resolves requested settings to effective host state (`enabled`, `effective`, `level`, applied opacity).  
  Acceptance: equivalent inputs produce stable signatures and effective-state outputs.
- [x] 2.2 Implement stream dedup logic so unchanged effective signatures do not emit duplicate events.  
  Acceptance: repeated equivalent host notifications emit one event only.

## 3. Host-Owned Drag Region Semantics (Phase 4 / Deliverable 4.2)

- [x] 3.1 Implement deterministic drag initiation policy for host drag strip/top region.  
  Acceptance: pointer down in drag-eligible area starts move-drag on supported platforms.
- [x] 3.2 Implement interactive exclusion precedence (buttons/inputs in chrome area do not trigger drag).  
  Acceptance: pointer in exclusion area routes to control interaction and does not initiate drag.

## 4. Transparency End-to-End Application (Phase 4 / Deliverable 4.6)

- [x] 4.1 Wire transparency update path so host window composition and web-consumed shell state are updated from one applied state source.  
  Acceptance: toggling transparency/opacity updates effective-state stream and visible host effect consistently.
- [x] 4.2 Add deterministic fallback diagnostics when transparency is requested but not effective on current compositor/runtime.  
  Acceptance: state contains stable validation/fallback message for unsupported/effective-none outcomes.

## 5. Web Consumption Path (Phase 4 / Deliverable 4.5)

- [x] 5.1 Replace polling-based shell state sync in sample/template path with stream-first subscription.  
  Acceptance: no periodic timer is required for normal shell state sync.
- [x] 5.2 Consume top inset/safe inset metrics in layout root so title/chrome overlap is avoided under custom chrome.  
  Acceptance: top controls remain visible and not clipped when window uses extended client area.

## 6. Contract Tests (Goal G4, Phase 4 / Deliverable 4.6)

- [x] 6.1 Add CT for `Update -> Snapshot -> Stream` deterministic order and field completeness.  
  Acceptance: tests pass with mock adapter/runtime only (no real browser requirement).
- [x] 6.2 Add CT for stream dedup and signature stability.  
  Acceptance: repeated equal states do not generate extra emissions.
- [x] 6.3 Add CT for drag region precedence/exclusion behavior.  
  Acceptance: drag-eligible and exclusion inputs are validated with deterministic outcomes.

## 7. Integration Validation (Phase 4 / Deliverable 4.6)

- [x] 7.1 Add/extend IT for macOS custom chrome drag behavior and top safe-area layout.  
  Acceptance: IT validates drag works in configured strip and interactive exclusions remain clickable.
- [x] 7.2 Add/extend IT for transparency effective-state mapping.  
  Acceptance: IT validates enabled/effective/level consistency with host-reported compositor result.

## 8. Final Governance Validation (Goal G4)

- [x] 8.1 Run `openspec validate --all --strict` and keep change spec-valid.  
  Acceptance: validator returns 0 failures.
- [x] 8.2 Run targeted build/tests for touched runtime + sample paths.  
  Acceptance: build passes and no newly introduced test failures on targeted suites.
