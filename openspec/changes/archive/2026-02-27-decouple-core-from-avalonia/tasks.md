## 1. Contract Boundary Refactor (D1, D3)

- [x] 1.1 Introduce `INativeHandle` in Core and replace public `IPlatformHandle` usages in `IWebDialog`, `INativeWebViewHandleProvider`, typed handle interfaces, and related event args (AC: `Agibuild.Fulora.Core` builds without Avalonia reference).
- [x] 1.2 Update `IWebViewAdapter.Attach` and related abstraction contracts to consume `INativeHandle` only (AC: `Agibuild.Fulora.Adapters.Abstractions` compiles with updated signatures, no dual-path API).
- [x] 1.3 Refactor runtime (`WebViewCore`, `WebDialog`, auth window owner contracts) to new handle contracts without behavior fallback branches (AC: runtime unit tests for navigation/auth/handle retrieval compile and pass).

## 2. Host Layer Isolation (D2, D4)

- [x] 2.1 Move Avalonia dispatcher implementation out of runtime into host layer and rewire DI registrations to contract-only dependency in runtime (AC: runtime project has no `Avalonia` package reference).
- [x] 2.2 Isolate Avalonia control/dialog/app-builder integration into host-specific assembly/package boundary (AC: Avalonia-specific types are absent from Core/Runtime/Adapters.Abstractions source and public API).
- [x] 2.3 Update package metadata and transitive dependency graph so only host package depends on Avalonia (AC: package restore graph confirms Core/Runtime/Adapters.Abstractions are Avalonia-free).

## 3. Adapter and Template Alignment (D3, D4)

- [x] 3.1 Update all platform adapters (Windows/macOS/iOS/GTK/Android) to accept `INativeHandle` and continue exposing typed native handles (AC: adapter projects build and integration attach semantics remain unchanged).
- [x] 3.2 Update template desktop host dependency wiring to explicit Avalonia host layer reference (AC: generated template project resolves host integration explicitly and builds).

## 4. Verification and Governance (D5)

- [x] 4.1 Add/adjust CTs for contract signatures and reflection assertions ensuring no Avalonia types leak from core contracts (AC: updated CT suite passes).
- [x] 4.2 Add/adjust unit/integration tests for runtime+adapter lifecycle and native handle retrieval semantics after boundary change (AC: related unit and integration test groups pass).
- [x] 4.3 Add dependency governance checks preventing reintroduction of Avalonia references in Core/Runtime/Adapters.Abstractions (AC: CI/local verification fails when forbidden dependency is introduced).
- [x] 4.4 Run full validation baseline: `nuke Test`, `nuke Coverage`, `openspec validate --all --strict` (AC: all commands pass and results are recorded in change evidence notes).
