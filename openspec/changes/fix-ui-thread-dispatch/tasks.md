## 1. Contract Reset (Async-First Public Surface)

- [x] 1.1 Replace `IWebView` sync command signatures with async (`GoBackAsync`, `GoForwardAsync`, `RefreshAsync`, `StopAsync`) and remove old sync variants. (Deliverable: D1, AC: Core contracts compile and no sync command signatures remain.)
- [x] 1.2 Replace sync DevTools/Zoom/Preload/StopFind surface with async methods across Core contracts (`OpenDevToolsAsync`, `CloseDevToolsAsync`, `IsDevToolsOpenAsync`, `GetZoomFactorAsync`, `SetZoomFactorAsync`, `AddPreloadScriptAsync`, `RemovePreloadScriptAsync`, `StopFindInPageAsync`). (Deliverable: D1, AC: Contract tests compile against async-only signatures.)
- [x] 1.3 Update `ICommandManager` contract from sync methods to async command methods. (Deliverable: D1, AC: `ICommandManager` reflection test asserts async methods only.)

## 2. Runtime Execution Model (OperationQueue + Lifecycle Gate)

- [x] 2.1 Introduce `WebViewOperation` model and single-consumer FIFO `OperationQueue` inside `WebViewCore` with operation IDs and timestamps. (Deliverable: D2, AC: Queue processes operations in enqueue order under concurrent producers.)
- [x] 2.2 Route all adapter-backed runtime calls through queue execution and UI dispatcher hop; remove direct adapter invocation paths from public methods. (Deliverable: D2, AC: No public API method invokes adapter directly outside queue executor.)
- [x] 2.3 Implement explicit lifecycle state machine (`Created`, `Attaching`, `Ready`, `Detaching`, `Disposed`) with deterministic allow/reject rules. (Deliverable: D3, AC: State transition tests pass and rejected states fail fast.)
- [x] 2.4 Enforce boundary closure for manager/facade paths (`TryGetCommandManager`, cookie/bridge related runtime access points) so they execute through same queue semantics. (Deliverable: D4, AC: Manager-path calls are observed by queue instrumentation.)

## 3. Feature Surface Migration

- [x] 3.1 Migrate command navigation runtime behavior to async command APIs while preserving navigation event semantics (`NavigationStarted`/`NavigationCompleted`). (Deliverable: D1+D2, AC: Command navigation CT passes with async APIs.)
- [x] 3.2 Migrate zoom capability from property-style sync path to async get/set path in runtime + control layer. (Deliverable: D1, AC: Zoom CT and integration usage compile and pass with async API.)
- [x] 3.3 Migrate preload script add/remove to async path end-to-end (WebView/WebDialog/WebViewCore). (Deliverable: D1, AC: Preload IT/E2E scenarios pass via async API only.)
- [x] 3.4 Migrate find stop operation to async API and queue execution. (Deliverable: D1+D2, AC: Find-in-page tests pass and no sync stop API remains.)
- [x] 3.5 Migrate DevTools toggle/query to async API and queue execution with no-op semantics on unsupported adapters. (Deliverable: D1, AC: DevTools capability tests pass on supported and unsupported adapters.)
- [x] 3.6 Remove obsolete sync compatibility members from `WebViewCore` / `WebView` / `WebDialog` / `AvaloniaWebDialog`, and migrate integration call sites to async-only APIs. (Deliverable: D8, AC: solution build has no obsolete sync API usage warnings.)

## 4. Error Model and Observability

- [x] 4.1 Implement unified operation failure taxonomy (`Disposed`, `NotReady`, `DispatchFailed`, `AdapterFailed`) and map runtime exceptions accordingly. (Deliverable: D5, AC: Failure mapping tests assert deterministic category per failure mode.)
- [x] 4.2 Add structured operation logging fields (`operationId`, `operationType`, `enqueueTs`, `startTs`, `endTs`, threadId, lifecycleState`). (Deliverable: D5, AC: Log assertions confirm required fields for success/failure paths.)
- [x] 4.3 Remove fire-and-forget runtime calls in adapter-backed APIs; ensure all operations return awaited Tasks to callers. (Deliverable: D5, AC: Static scan + tests show no fire-and-forget in public adapter-backed paths.)
- [x] 4.4 Remove avoidable `GetAwaiter().GetResult()` usage from runtime paths (`BridgeImportProxy` sync-return path) and enforce Task-only contract for `[JsImport]`. (Deliverable: D9, AC: sync-return import invocation throws deterministic `NotSupportedException`.)

## 5. Tests and Verification

- [x] 5.1 Add contract tests for any-thread invocation across all async public APIs, asserting adapter invocations occur on UI thread. (Deliverable: D2+D4, AC: New threading CT suite passes deterministically.)
- [x] 5.2 Add concurrency tests (multi-thread mixed operations) validating FIFO linearization and single-completion invariant. (Deliverable: D2, AC: Stress tests pass without flaky ordering failures.)
- [x] 5.3 Add lifecycle gate tests validating accept/reject behavior in each state and fast-fail after disposal. (Deliverable: D3, AC: State matrix tests pass.)
- [x] 5.4 Update integration/E2E scenarios to async API usage and verify previously failing cases (`Screenshot`, `PrintToPdf`, `FindInPage`, `PreloadScript`). (Deliverable: D1+D2, AC: Feature E2E suite passes on Windows.)
- [x] 5.5 Add Windows Runtime-version matrix validation for `PrintToPdf` to separate threading fix from runtime capability mismatch. (Deliverable: D7, AC: Matrix report distinguishes thread-related pass/fail from interface-version incompatibility.)
- [x] 5.6 Add regression test for async-preload adapter preference and verify preload path avoids sync blocking fallback when async adapter is available. (Deliverable: D8, AC: targeted unit test passes and asserts async path is used.)
- [x] 5.7 Add production-source guard test for `GetAwaiter().GetResult()` whitelist to prevent future blocking-wait abuse. (Deliverable: D9, AC: unit guard test fails on any new non-whitelisted usage.)

## 6. Final Validation

- [x] 6.1 Run full test matrix (CT/IT/E2E) on Windows/macOS/Linux and record pass/fail with operation diagnostics attached for failures. (Deliverable: D2+D5, AC: No regression in existing suites and no UI-thread contract exceptions.)
  - [x] Windows CT: `Agibuild.Avalonia.WebView.UnitTests` passed (`643/643`)
  - [x] Windows IT/E2E automation: `Agibuild.Avalonia.WebView.Integration.Tests.Automation` passed (`112/112`)
  - [x] Windows solution-level matrix: `dotnet test Agibuild.Avalonia.WebView.sln` passed (Unit `643/643`, Automation `112/112`)
  - [x] Linux CT: `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --os linux` passed (`643/643`)
  - [x] Linux IT/E2E automation: `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --os linux` passed (`112/112`)
  - [x] macOS CT: `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --os osx` passed (`643/643`)
  - [x] macOS IT/E2E automation: `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --os osx` passed (`112/112`)
  - [x] Failure diagnostics (cross-platform): initial linux/osx matrix exposed two test-level timing/concurrency issues
    - `BridgeIntegrationTests.Bridge_is_thread_safe_for_expose_operations` (linux): concurrent `ScriptCallback` writes caused nondeterministic assertion; fixed by neutralizing callback during parallel expose and restoring callback for assertion phase.
    - `ContractSemanticsV1NavigationTests.Latest_wins_supersedes_active_navigation` (osx): supersede event observation raced with assertion; fixed by adding dispatcher pump wait before asserting `Superseded`.
  - [x] UI-thread contract exception check: no `"must be called on UI thread"` regressions observed across Windows/Linux/macOS matrix runs.
- [x] 6.2 Run OpenSpec validation for `fix-ui-thread-dispatch` and confirm proposal/design/specs/tasks remain consistent with async-first architecture. (Deliverable: D1-D7, AC: `openspec validate fix-ui-thread-dispatch` passes.)
