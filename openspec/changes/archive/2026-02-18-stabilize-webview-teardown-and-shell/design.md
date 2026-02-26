## Context

- The Windows adapter (`Agibuild.Fulora.Adapters.Windows`) initializes WebView2 asynchronously on Attach and performs best-effort teardown on Detach, attempting to marshal COM and Win32 cleanup back onto the captured UI `SynchronizationContext`.
- The NuGet package smoke lane runs a real Avalonia app and fails the build if Chromium teardown errors appear (notably `Failed to unregister class Chrome_WidgetWin_0` / `window_impl.cc:124`), which is treated as a lifecycle regression.
- This change targets Phase 3 quality/readiness work (ROADMAP Phase 3.3 “Performance & Quality” and Phase 3.5 “GA Release”), and aligns with **G4 (Contract-Driven Testability)** and **E2 (Dev Tooling)** by making lifecycle correctness observable and enforceable in automation.

## Goals / Non-Goals

**Goals:**
- Provide deterministic, automation-verifiable teardown for Windows WebView2:
  - Detach is safe from any caller thread (no deadlocks, bounded waits).
  - No WebView2/Chromium teardown error strings emitted during normal shutdown.
  - No leaked window subclassing (WndProc restore) or event handlers after teardown.
- Provide an opt-in “shell experience” layer that helps hosts implement desktop-grade behavior without expanding Core contracts:
  - consistent defaults/policies around `NewWindowRequested`, downloads, permissions, and DevTools toggling.
- Add tests (CT + IT) that catch lifecycle regressions before packaging.

**Non-Goals:**
- Bundled browser engine / Node.js runtime / full bundled-browser parity.
- OS-wide app shell features (tray/global shortcuts/auto-update) inside this repo’s runtime libraries.
- Breaking changes to Core contracts unless strictly required by specs.

## Decisions

### 1) Treat teardown determinism as a first-class, spec-tested behavior (Windows-specific capability)

- **Decision:** Create a Windows-focused capability spec (`webview2-teardown-stability`) with explicit acceptance criteria derived from the smoke lane guardrail.
- **Rationale:** The failure mode is observable in automation and impacts GA readiness; a dedicated spec provides a stable contract and prevents regressions.
- **Alternatives considered:**
  - Only “fix the bug” in the adapter without a spec → fast but regressions likely, weak alignment with contract-first philosophy.
  - Fold requirements into `webview-contract-semantics-v1` → risks conflating cross-platform baseline semantics with a platform-specific WebView2 issue.

### 2) Make teardown a single, serialized lifecycle path with bounded cross-thread coordination

- **Decision:** Centralize Detach into a single teardown path that:
  - cancels initialization and prevents pending operations from executing after Detach,
  - performs Win32 window-proc restoration before closing controller,
  - unsubscribes all WebView2 events deterministically,
  - releases COM objects consistently on the initialization/UI thread,
  - never blocks the UI thread, and only uses bounded waiting when Detach is called off-thread.
- **Rationale:** WebView2/Win32 lifetimes are sensitive to ordering and thread affinity; a single serialized teardown path reduces races.
- **Alternatives considered:**
  - “Best effort” teardown with multiple exit paths and partial ordering → simpler but more race-prone.
  - Fully synchronous cross-thread `Send`/blocking teardown → increases deadlock risk during app shutdown.

### 3) Provide shell experience via opt-in runtime policies (no Core expansion)

- **Decision:** Introduce an opt-in runtime “shell experience” component (`webview-shell-experience`) that hosts can enable to apply consistent policies:
  - `NewWindowRequested`: choose a strategy (navigate-in-place, open external browser, open a `WebDialog`, or delegate to host callback).
  - Downloads/permissions: standardize default behavior and ensure event ordering/threading matches existing semantics.
  - DevTools: surface a predictable UX path (toggle/open/close) without coupling to a specific UI toolkit.
- **Rationale:** Desktop-grade behaviors are highly app-specific; the library should provide composable policies and helpers rather than hard-coded UI.
- **Alternatives considered:**
  - Add more fields to Core event args to emulate bundled-browser’s `window.open` feature set → high surface area and cross-platform inconsistency risk.
  - Keep everything “host-only” with no helpers → forces each app to reinvent common patterns and reduces adoption.

### Testing strategy

- **Contract tests (CT):**
  - Add deterministic CTs for “no events after detach/dispose” and for shell policy behavior using MockAdapter/dispatcher.
  - Ensure new policies are testable without a real browser (G4).
- **Integration tests (IT):**
  - Add/extend Windows automation that repeatedly creates/attaches/detaches a WebView and asserts:
    - the process exits cleanly without the known Chromium teardown markers,
    - no hangs (bounded timeouts),
    - smoke lane remains stable across retries.

## Risks / Trade-offs

- **[WebView2 emits non-deterministic teardown logs depending on runtime version] →** Pin acceptance criteria to the specific marker strings used by CI guardrails; validate across supported WebView2 runtime versions in Windows CI.
- **[Bounded waits may hide rare teardown stalls] →** Treat timeouts as test failures in automation; capture diagnostics (structured logs) to debug root causes.
- **[Shell policies risk becoming UI-framework-specific] →** Keep the component policy-driven and UI-agnostic; only expose callbacks/strategies and leave UI rendering to the host.
