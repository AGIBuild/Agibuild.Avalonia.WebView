## Context

Post-Phase 12, the codebase has grown to 1,879 unit tests + 209 integration tests across 97%+ coverage. Build system is 3,900+ lines across 9 partial files. Runtime core (`WebViewShellExperience`, `WebViewCore`, `WebViewContracts`) and platform adapters carry accumulated structural debt from rapid phased delivery. No behavioral changes are needed — only internal restructuring to improve navigability and reduce maintenance surface.

## Goals / Non-Goals

**Goals:**
- Reduce per-file cognitive load where it improves maintainability without introducing threshold-driven churn
- Eliminate code duplication (build JSON serialization, Cobertura parsing, adapter bridge scripts)
- Single source of truth for magic strings (lane contexts, package IDs)
- Replace brittle governance source-string checks with syntax-level contract assertions
- Preserve all existing public APIs, namespaces, and test behavior
- All 1,879 + 209 tests pass with zero changes to assertions

**Non-Goals:**
- Splitting `Build.Governance.cs` (deferred; requires governance schema alignment)
- Changing public API surface, namespaces, or assembly boundaries
- Performance optimization or behavioral changes
- Refactoring the Bridge Source Generator

## Decisions

### D1: Build helper extraction strategy

**Choice:** Extract `WriteJsonReport(AbsolutePath, object)` as a static method in `Build.Helpers.cs`; introduce `TempDirectoryScope : IDisposable` for temp directory lifecycle.

**Rationale:** 14 identical `JsonSerializer.Serialize` + `File.WriteAllText` call sites. A single helper reduces lines and ensures consistent `WriteIndented = true` option. `TempDirectoryScope` replaces 3 manual try/finally cleanup blocks in `Build.Governance.cs`.

**Alternative considered:** Generic `GovernanceReportBuilder` — rejected as over-engineering for what is essentially one-line delegation.

### D2: Build.Helpers.cs split into ProcessHelpers + TestHelpers

**Choice:** Split by concern:
- `Build.ProcessHelpers.cs`: `RunProcess`, `RunProcessCaptureAll`, `RunProcessCaptureAllChecked`, `RunNpmCaptureAll`
- `Build.TestHelpers.cs`: `RunContractAutomationTests`, `RunRuntimeAutomationTests`, `ReadTrxCounters`, `ReadPassedTestNamesFromTrx`, `ReadCoberturaLineCoveragePercent`, `ReadCoberturaBranchCoveragePercent`, `RunLaneWithReporting`

**Rationale:** Current file mixes unrelated concerns. Split makes it obvious where to find/add helpers. Both remain `partial class BuildTask`.

### D3: Build.Testing.Coverage inline parsing → helper reuse

**Choice:** Replace the 20-line inline XDocument Cobertura parsing with existing `ReadCoberturaLineCoveragePercent` / `ReadCoberturaBranchCoveragePercent` from helpers.

**Rationale:** Direct dedup. The helpers already exist and are used by `ReleaseCloseoutSnapshot`.

### D4: WebViewShellExperience decomposition approach

**Choice:** Extract into focused collaborator classes within the same `Shell/` directory, injected via constructor:
- `WebViewNewWindowHandler` — new-window strategy selection and execution
- `WebViewManagedWindowManager` — managed window lifecycle, tracking, teardown
- `WebViewHostCapabilityExecutor` — capability policy execution dispatch

`WebViewShellExperience` becomes a thin coordinator delegating to these collaborators.

**Rationale:** Preserves the single public entry point (`WebViewShellExperience`) while distributing responsibilities. Internal classes keep the change non-breaking.

**Alternative considered:** Splitting into completely independent services registered in DI — rejected because shell experience is tightly coupled to WebView lifecycle and doesn't benefit from independent DI registration.

### D5: WebViewContracts.cs split strategy

**Choice:** Split by type category in the same `Agibuild.Fulora.Core` namespace:
- `WebViewEnums.cs` — all enums
- `WebViewInterfaces.cs` — all interfaces (`IWebViewAdapter`, `IWebView`, etc.)
- `WebViewRecords.cs` — all record/class types (request/response, options, event args)

**Rationale:** The 966-line file mixes unrelated type categories. Splitting by category makes it navigable without changing any namespace or assembly.

### D6: Adapter bridge script deduplication

**Choice:** Add `WebViewBridgeScriptFactory` as a static class in `Agibuild.Fulora.Adapters.Abstractions` that produces the common `window.__agibuildWebView` injection script. Apply it to adapters that inline bootstrap script injection (`Windows`, `Android`).

**Rationale:** Inline bootstrap duplication was present in `Windows`/`Android` adapter paths. `MacOS`/`iOS`/`Gtk` use runtime preload-script flows and do not contain equivalent inline bootstrap copy points, so they are explicitly out of scope for D6.

### D7: String literal centralization

**Choice:** Add constants in `Build.cs` (build system) and `WebViewConstants.cs` (runtime):
- Build: `LaneContextCi = "Ci"`, `LaneContextCiPublish = "CiPublish"`, `PrimaryHostPackageId = "Agibuild.Fulora.Avalonia"`
- Runtime: no new constants needed (adapter scripts handled by D6)

**Rationale:** 25+ occurrences of lane context strings. Centralized constants prevent typo-based drift.

### D8: Large test file split convention

**Choice:** Split by domain/component under test. New file naming: `{Component}{TestCategory}Tests.cs`.

| Source file | Split into |
|------------|-----------|
| `CoverageGapTests.cs` (1,870) | `WebMessageCoverageTests.cs`, `CookieCoverageTests.cs`, `WebDialogCoverageTests.cs`, `WebViewCoreCoverageTests.cs` |
| `ShellExperienceBranchCoverageTests.cs` (1,483) | `ShellNewWindowCoverageTests.cs`, `ShellDevToolsCoverageTests.cs`, `ShellCommandCoverageTests.cs`, `ShellSessionCoverageTests.cs` |
| `RuntimeCoverageTests.cs` (1,277) | `BridgeProxyCoverageTests.cs`, `SpaHostingCoverageTests.cs`, `RpcServiceCoverageTests.cs` |

**Rationale:** Domain-specific files are faster to navigate and produce clearer test failure attribution.

### D9: WebViewCore long method extraction

**Choice:** Extract private helper methods within `WebViewCore.cs`:
- `OnNativeNavigationStartingOnUiThread` → extract `HandleNavigationRedirect`, `HandleNavigationSupersession`
- `StartNavigationRequestCoreAsync` → extract `AwaitNavigationCompletion`

**Rationale:** Keeps methods under 50 lines. No class split needed — the methods are cohesive within `WebViewCore`.

### D10: Governance contract assertion hardening

**Choice:** Introduce Roslyn syntax assertion helpers in governance tests and migrate high-risk checks (target declarations, `DependsOn` graph contracts, key assignments, invariant string literals, command/report wiring) away from raw substring/regex assertions.

**Rationale:** Source-string matching is brittle to harmless formatting/reordering changes. Syntax-level assertions keep governance intent machine-checkable while reducing false failures.

## Risks / Trade-offs

- **[Risk] File move breaks git blame** → Mitigation: use `git mv` where possible; accept that some history fragmentation is unavoidable for structural improvement
- **[Risk] Partial class proliferation in build** → Mitigation: limit to 2 new build files (ProcessHelpers + TestHelpers), not per-target splits
- **[Risk] Test file split introduces import/fixture duplication** → Mitigation: shared test fixtures remain in existing helper files (`MockWebViewAdapter`, `TestDispatcher`)
- **[Risk] WebViewShellExperience decomposition breaks internal state sharing** → Mitigation: collaborator classes receive shared state via constructor injection; no new public surface
- **[Trade-off] More files to navigate** → Accepted: smaller, focused files > fewer large files for long-term maintainability

## Testing Strategy

- **Zero test logic changes** — all refactoring is structural (file splits, method extractions, dedup)
- **Validation**: all 1,879 unit + 209 integration tests must pass unchanged
- **Coverage gate**: line ≥ 96%, branch ≥ 90% (existing thresholds)
- **Build system**: `nuke Test`, `nuke Coverage` must succeed
