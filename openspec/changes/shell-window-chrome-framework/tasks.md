## 1. Core Contracts (Agibuild.Fulora.Core)

- [ ] 1.1 Add `TransparencyLevel` enum (`None`, `Transparent`, `Blur`, `AcrylicBlur`, `Mica`) with JSON camelCase serialization.
  Acceptance: Enum compiles, serializes as camelCase string via STJ.

- [ ] 1.2 Add `IWindowChromeProvider` interface (`Platform`, `SupportsTransparency`, `ApplyWindowAppearanceAsync`, `GetTransparencyState`, `GetChromeMetrics`, `AppearanceChanged` event).
  Acceptance: Interface compiles in Core without host-framework dependencies.

- [ ] 1.3 Add `WindowAppearanceRequest` and `TransparencyEffectiveState` DTOs.
  Acceptance: DTOs compile and include all fields from design (enable, opacity, theme, effective state fields, validation message).

- [ ] 1.4 Move `IWindowShellService` from `AvaloniAiChat.Bridge` to Core with `[JsExport]` attribute.
  Acceptance: Interface in Core references Core DTOs only. Sample bridge project references Core interface instead of local copy.

- [ ] 1.5 Update `WindowShellCapabilities.EffectiveTransparencyLevel` from `string` to `TransparencyLevel` enum.
  Acceptance: Existing Core DTOs updated; all references compile.

## 2. Runtime WindowShellService (Agibuild.Fulora.Runtime/Shell)

- [ ] 2.1 Implement `WindowShellService` class with constructor taking `IWindowChromeProvider` and `IPlatformThemeProvider`.
  Acceptance: Class compiles, implements `IWindowShellService` and `IDisposable`.

- [ ] 2.2 Implement `GetWindowShellState()` that builds state from provider + current settings.
  Acceptance: Returns complete `WindowShellState` with all fields populated from provider state.

- [ ] 2.3 Implement `UpdateWindowShellSettings()` with settings validation (clamp opacity 20-95, validate theme preference).
  Acceptance: Invalid values are clamped/defaulted; provider receives validated `WindowAppearanceRequest`.

- [ ] 2.4 Implement `StreamWindowShellState()` with signature-based dedup.
  Acceptance: Equivalent states produce single emission; state changes emit new values.

- [ ] 2.5 Implement theme resolution logic (`"system"` → OS theme, `"liquid"` / `"classic"` → fixed).
  Acceptance: Theme preference correctly resolves against OS theme mode.

- [ ] 2.6 Implement transparency state machine enforcing legal state combinations.
  Acceptance: Invalid combinations (`enabled=false, effective=true`) never produced.

- [ ] 2.7 Wire OS theme change notifications from `IPlatformThemeProvider` to trigger stream emission.
  Acceptance: OS theme change triggers state rebuild and stream emission when effective state differs.

## 3. Avalonia AvaloniaWindowChromeProvider (Agibuild.Fulora.Avalonia)

- [ ] 3.1 Implement `AvaloniaWindowChromeProvider` class implementing `IWindowChromeProvider` and `IDisposable`.
  Acceptance: Class compiles in Avalonia layer with Avalonia dependencies.

- [ ] 3.2 Implement `TrackWindow(Window, WindowChromeTrackingOptions?)` and `UntrackWindow(Window)` for multi-window management.
  Acceptance: Provider tracks multiple windows; untrack removes handlers and stops tracking.

- [ ] 3.3 Implement tunnel-based drag region handling: window-level `PointerPressedEvent` handler with Y-coordinate check and interactive element exclusion.
  Acceptance: Drag starts for pointer in drag region; interactive controls are excluded; no AXAML modifications needed.

- [ ] 3.4 Implement transparency application: `TransparencyLevelHint`, `ExtendClientAreaToDecorationsHint`, `Background` on all tracked windows.
  Acceptance: Toggling transparency updates all tracked windows simultaneously.

- [ ] 3.5 Implement `GetTransparencyState()` reading actual Avalonia window transparency state.
  Acceptance: Returns effective transparency level, enabled/effective flags, and validation message.

- [ ] 3.6 Implement `GetChromeMetrics()` from Avalonia window decoration state.
  Acceptance: Returns title bar height, drag region height, safe insets from tracked window configuration.

- [ ] 3.7 Implement `AppearanceChanged` event wiring from `Application.ActualThemeVariantChanged`.
  Acceptance: OS theme change raises `AppearanceChanged` event.

- [ ] 3.8 Implement `ApplyWindowAppearanceAsync()` applying transparency + theme background to all tracked windows.
  Acceptance: Method applies to all tracked windows; new windows tracked later inherit current appearance.

## 4. Contract Tests (Agibuild.Fulora.UnitTests)

- [ ] 4.1 CT: Transparency state machine — legal combinations for Disabled, Active, Fallback states.
  Acceptance: All three state paths validated with mock provider.

- [ ] 4.2 CT: Invalid state combinations never produced.
  Acceptance: Test asserts impossible states are rejected.

- [ ] 4.3 CT: Settings validation — opacity clamping, theme preference validation.
  Acceptance: Out-of-range opacity clamped; invalid theme defaults to system.

- [ ] 4.4 CT: Stream dedup — equivalent states emit once, changed states emit.
  Acceptance: Dedup semantics validated end-to-end with mock provider.

- [ ] 4.5 CT: Theme resolution — system/liquid/classic preference paths.
  Acceptance: All three preference modes resolve correctly against mock OS theme.

- [ ] 4.6 CT: Multi-window — update applies to all tracked windows via provider.
  Acceptance: Mock provider receives apply call after settings update.

- [ ] 4.7 CT: OS theme change triggers stream emission.
  Acceptance: Mock theme provider change triggers state rebuild and stream emission.

- [ ] 4.8 CT: Update → Snapshot → Stream deterministic order.
  Acceptance: Full roundtrip validated in single test.

## 5. Sample Migration (avalonia-ai-chat)

- [ ] 5.1 Remove `IWindowShellService` from `AvaloniAiChat.Bridge` (use Core contract).
  Acceptance: Sample bridge project references `Agibuild.Fulora.Core.IWindowShellService`.

- [ ] 5.2 Replace `AppearanceService` window shell logic with framework `WindowShellService` + `AvaloniaWindowChromeProvider`.
  Acceptance: `AppearanceService` delegates to framework service or is removed; MainWindow setup uses ~5 lines.

- [ ] 5.3 Simplify `MainWindow.axaml` — remove manual DragRegion Border, PointerPressed handler, IsInteractiveChromeSource.
  Acceptance: MainWindow AXAML has no drag region overlay; framework handles drag.

- [ ] 5.4 Verify web layer (`App.tsx`) continues to work with same RPC contract.
  Acceptance: Web layer calls same `WindowShellService.*` methods; no JS changes needed.

## 6. Spec Sync & Validation

- [ ] 6.1 Run `openspec validate --all --strict` and fix any failures.
  Acceptance: Validator returns 0 failures.

- [ ] 6.2 Run full test suite (`nuke Test`) and ensure all tests pass.
  Acceptance: All unit and integration tests pass.
