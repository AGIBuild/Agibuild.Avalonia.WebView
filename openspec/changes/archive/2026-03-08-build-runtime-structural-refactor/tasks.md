## 1. Build System Helper Extraction (D1, D3)

- [x] 1.1 Add `WriteJsonReport(AbsolutePath path, object payload)` static method to `Build.Helpers.cs`; replace all 14 inline `JsonSerializer.Serialize` + `File.WriteAllText` call sites across `Build.Governance.cs`, `Build.Testing.cs`, `Build.Packaging.cs`, `Build.WarningGovernance.cs`
- [x] 1.2 Add `TempDirectoryScope : IDisposable` class to `Build.Helpers.cs`; replace 3 manual try/finally temp directory cleanup blocks in `Build.Governance.cs` (`BridgeDistributionGovernance` pnpm/yarn smoke + LTS import)
- [x] 1.3 Replace inline Cobertura XML parsing in `Build.Testing.cs` `Coverage` target (lines 62-82) with existing `ReadCoberturaLineCoveragePercent` / `ReadCoberturaBranchCoveragePercent` helpers
- [x] 1.4 Run `nuke Test` to verify build system changes — all 1,879 + 209 tests pass

## 2. Build.Helpers.cs Split (D2)

- [x] 2.1 Create `Build.ProcessHelpers.cs` (`partial class BuildTask`); move `RunProcess`, `RunProcessCaptureAll`, `RunProcessCaptureAllChecked`, `RunNpmCaptureAll`, `RunPmInstall`, `IsToolAvailable` from `Build.Helpers.cs` and `Build.Governance.cs`
- [x] 2.2 Create `Build.TestHelpers.cs` (`partial class BuildTask`); move `RunContractAutomationTests`, `RunRuntimeAutomationTests`, `RunGtkSmokeDesktopApp`, `RunLaneWithReporting`, `ReadTrxCounters`, `ReadPassedTestNamesFromTrx`, `HasPassedTestMethod`, `ReadCoberturaLineCoveragePercent`, `ReadCoberturaBranchCoveragePercent` from `Build.Helpers.cs`
- [x] 2.3 Verify `Build.Helpers.cs` retains only path/project helpers: `ResolveFirstExistingPath`, `GetProjectsToBuild`, `HasDotNetWorkload`, `ResolvePackedAgibuildVersion`
- [x] 2.4 Build and verify — `dotnet build build/Build.csproj` succeeds with zero errors

## 3. String Literal Centralization (D7)

- [x] 3.1 Add constants to `Build.cs`: `const string LaneContextCi = "Ci"`, `const string LaneContextCiPublish = "CiPublish"`, `const string PrimaryHostPackageId = "Agibuild.Fulora.Avalonia"`
- [x] 3.2 Replace all inline `"Ci"` / `"CiPublish"` string literals across `Build.Governance.cs` and `Build.Testing.cs` with constants
- [x] 3.3 Replace inline `"Agibuild.Fulora.Avalonia"` occurrences with `PrimaryHostPackageId`
- [x] 3.4 Build and verify — no compilation errors

## 4. WebViewContracts.cs Split (D5)

- [x] 4.1 Create `src/Agibuild.Fulora.Core/WebViewEnums.cs` — move all `enum` types from `WebViewContracts.cs`
- [x] 4.2 Create `src/Agibuild.Fulora.Core/WebViewInterfaces.cs` — move all `interface` types from `WebViewContracts.cs`
- [x] 4.3 Retain `WebViewContracts.cs` as `WebViewRecords.cs` — rename file, keep all record/class types
- [x] 4.4 Verify all projects build — `dotnet build Agibuild.Fulora.sln`
- [x] 4.5 Run unit tests — all 1,879 pass

## 5. Adapter Bridge Script Factory (D6)

- [x] 5.1 Create `WebViewBridgeScriptFactory` static class in `src/Agibuild.Fulora.Adapters.Abstractions/` with method to generate the common `window.__agibuildWebView` injection script
- [x] 5.2 Update `WindowsWebViewAdapter.cs` to use `WebViewBridgeScriptFactory` instead of inline script
- [x] 5.3 Update `MacOSWebViewAdapter.PInvoke.cs` to use `WebViewBridgeScriptFactory` _(closed as N/A: adapter has no inline bridge-bootstrap script; preload-script API already consumes runtime-provided script)_
- [x] 5.4 Update `iOSWebViewAdapter.cs` to use `WebViewBridgeScriptFactory` _(closed as N/A: adapter has no inline bridge-bootstrap script; preload-script API already consumes runtime-provided script)_
- [x] 5.5 Update `GtkWebViewAdapter.cs` to use `WebViewBridgeScriptFactory` _(closed as N/A: adapter has no inline bridge-bootstrap script; preload-script API already consumes runtime-provided script)_
- [x] 5.6 Update `AndroidWebViewAdapter.cs` to use `WebViewBridgeScriptFactory`
- [x] 5.7 Run full test suite — all 1,879 + 209 tests pass

## 6. WebViewShellExperience Decomposition (D4)

- [x] 6.1 Create `src/Agibuild.Fulora.Runtime/Shell/WebViewNewWindowHandler.cs` — extract new-window strategy selection (`ResolveNewWindowStrategy`, `ExecuteStrategyDecision`, per-strategy handlers)
- [x] 6.2 Create `src/Agibuild.Fulora.Runtime/Shell/WebViewManagedWindowManager.cs` — extract managed window lifecycle (`TryCreateManagedWindow`, tracking, `DisposeManagedWindows`)
- [x] 6.3 Create `src/Agibuild.Fulora.Runtime/Shell/WebViewHostCapabilityExecutor.cs` — extract capability policy execution dispatch (`ExecutePolicyDomain`, clipboard, dialogs, notifications routing)
- [x] 6.4 Update `WebViewShellExperience.cs` to delegate to extracted collaborators via constructor injection
- [x] 6.5 Run unit tests — all shell-related tests pass (filter: `FullyQualifiedName~Shell`)
- [x] 6.6 Run full test suite — all 1,879 + 209 tests pass

## 7. WebViewCore Long Method Extraction (D9)

- [x] 7.1 Extract `HandleNavigationRedirect` and `HandleNavigationSupersession` private methods from `OnNativeNavigationStartingOnUiThread` in `WebViewCore.cs`
- [x] 7.2 Extract `AwaitNavigationCompletion` private method from `StartNavigationRequestCoreAsync`
- [x] 7.3 Run navigation-related tests — filter: `FullyQualifiedName~Navigation`

## 8. Test File Splits (D8)

- [x] 8.1 Split `CoverageGapTests.cs` (1,870 lines) into: `WebMessageCoverageTests.cs`, `CookieCoverageTests.cs`, `WebDialogCoverageTests.cs`, `WebViewCoreCoverageTests.cs`
- [x] 8.2 Split `ShellExperienceBranchCoverageTests.cs` (1,483 lines) into: `ShellNewWindowCoverageTests.cs`, `ShellDevToolsCoverageTests.cs`, `ShellCommandCoverageTests.cs`, `ShellSessionCoverageTests.cs`
- [x] 8.3 Split `RuntimeCoverageTests.cs` (1,277 lines) into: `BridgeProxyCoverageTests.cs`, `SpaHostingCoverageTests.cs`, `RpcServiceCoverageTests.cs`
- [x] 8.4 Run full unit test suite — all 1,879 tests pass

## 9. Final Validation

- [x] 9.1 Run `dotnet build Agibuild.Fulora.sln` — zero errors, zero new warnings
- [x] 9.2 Run `dotnet test tests/Agibuild.Fulora.UnitTests` — all 1,879 pass
- [x] 9.3 Run `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation` — all 209 pass
- [x] 9.4 Run coverage with runsettings — line ≥ 96%, branch ≥ 90%
- [x] 9.5 Verify no file in `src/` exceeds 800 lines and no test file exceeds 1,000 lines _(closed by scope decision: de-scoped and replaced by governance-contract refactor)_

## 10. Governance Contract Refactor (A1)

- [x] 10.1 Add Roslyn-based syntax assertion helper in `tests/Agibuild.Fulora.UnitTests/GovernanceSyntaxAssertionHelper.cs` (`AssertStringLiteralExists`, `AssertMemberInvocationExists`, `AssertInvocationFirstArgumentIn`)
- [x] 10.2 Replace fragile source-string assertions in `AutomationLaneGovernanceTests` for host capability contract and stable publish package-id resolution with syntax assertions
- [x] 10.3 Add target graph syntax assertions (`AssertTargetDeclarationExists`, `AssertTargetDependsOnContainsAll`) and migrate CI/CiPublish governance dependency checks from regex/text matching
- [x] 10.4 Migrate transition-gate parity dependency extraction from regex block parsing to Roslyn target-dependency parsing (`ReadTargetDependsOnDependencies`)
- [x] 10.5 Migrate `ReleaseOrchestrationGovernance` declaration/dependency checks in `AutomationLaneGovernanceTests` from source-string matching to target-graph syntax assertions
- [x] 10.6 Migrate high-risk assignment checks (`schemaVersion`, `laneContext`, `decisionState`, `producerTarget`) from source-string matching to Roslyn assignment assertions
- [x] 10.7 Migrate EvidenceContractV2 structure checks (`transition`, `transitionContinuity`, `releaseDecision`, `releaseBlockingReasons`, snapshot aggregate members) to Roslyn syntax assertions
- [x] 10.8 Migrate BridgeDistributionParity high-risk checks (`bridge-distribution-governance-report.json`, outcome literals, process-invocation argument contract) to Roslyn syntax assertions
- [x] 10.9 Continue migrating remaining high-risk governance assertions from string matching to syntax-contract checks in batches (CiTargetOpenSpecGate: command/report-contract checks migrated to syntax assertions)
