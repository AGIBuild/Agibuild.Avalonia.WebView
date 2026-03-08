## Why

Post-Phase 12 codebase has accumulated structural debt: build helper duplication (14 identical JSON serialization sites, inline Cobertura parsing duplicating existing helpers), God classes (`WebViewShellExperience` at 1,620 lines with ~55 methods, `WebViewContracts.cs` at 966 lines mixing enums/interfaces/records), duplicated bridge bootstrap injection in inline-script adapters, magic string literals for lane contexts and package IDs, and fragile governance tests that rely on raw source-string matching. This refactoring reduces maintenance surface, improves navigability, and establishes cleaner module boundaries with more stable contract-driven governance checks. Aligns with **E2 (Developer Experience)** and **G4 (Contract-Driven Testability)**.

## What Changes

- Extract `WriteJsonReport` and `TempDirectoryScope` build helpers; replace 14 inline JSON serialization sites
- Replace inline Cobertura parsing in `Build.Testing.Coverage` with existing `ReadCoberturaLineCoveragePercent`/`ReadCoberturaBranchCoveragePercent`
- Split `Build.Helpers.cs` into `Build.ProcessHelpers.cs` and `Build.TestHelpers.cs`
- Split `WebViewShellExperience.cs` into `WebViewNewWindowHandler`, `WebViewManagedWindowManager`, `WebViewHostCapabilityExecutor`
- Split `WebViewContracts.cs` into `WebViewEnums.cs`, `WebViewInterfaces.cs`, `WebViewRecords.cs`
- Extract `WebViewBridgeScriptFactory` in `Adapters.Abstractions` to deduplicate inline bridge bootstrap injection (Windows/Android)
- Centralize string literals (`"Ci"`, `"CiPublish"`, `"Agibuild.Fulora.Avalonia"`) as constants
- Split large test files (`CoverageGapTests.cs` 1,870 lines, `ShellExperienceBranchCoverageTests.cs` 1,483 lines, `RuntimeCoverageTests.cs` 1,277 lines) by domain
- Extract long methods in `WebViewCore.cs` (`OnNativeNavigationStartingOnUiThread` ~75 lines, `StartNavigationRequestCoreAsync` ~55 lines)
- Upgrade governance contract tests from fragile source-string matching to Roslyn syntax assertions for build target graph and evidence schema contracts

## Capabilities

### New Capabilities

_(none — this is a structural refactoring with no new behavioral capabilities)_

### Modified Capabilities

_(no spec-level requirement changes — all modifications are implementation-internal restructuring)_

## Impact

- **build/**: `Build.Helpers.cs` splits into 2 files; `Build.Testing.cs` removes ~20 lines of inline coverage parsing
- **src/Agibuild.Fulora.Runtime/**: `WebViewShellExperience.cs` splits into 3-4 files; `WebViewCore.cs` gains extracted helper methods
- **src/Agibuild.Fulora.Core/**: `WebViewContracts.cs` splits into 3 files
- **src/Agibuild.Fulora.Adapters.Abstractions/**: New `WebViewBridgeScriptFactory` class
- **src/Agibuild.Fulora.Adapters.Windows/** + **src/Agibuild.Fulora.Adapters.Android/**: consume shared bridge script factory for inline bootstrap script
- **src/Agibuild.Fulora.Adapters.MacOS/** + **src/Agibuild.Fulora.Adapters.iOS/** + **src/Agibuild.Fulora.Adapters.Gtk/**: no inline bridge bootstrap script path to deduplicate (unchanged by design)
- **tests/Agibuild.Fulora.UnitTests/**: governance contract assertions migrated to Roslyn syntax-based invariants
- **tests/**: 3 large test files split into ~10 smaller domain-specific files
- **No API changes** — all public types/methods/namespaces remain stable
- **No dependency changes**

## Non-goals

- Splitting `Build.Governance.cs` (deferred — requires separate governance schema alignment)
- Changing public API surface or namespaces
- Adding new features or behavioral changes
- Refactoring the Bridge Source Generator (`BridgeHostEmitter.cs` at 437 lines is within tolerance)
