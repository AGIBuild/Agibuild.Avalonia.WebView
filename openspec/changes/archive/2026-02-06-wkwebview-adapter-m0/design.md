## Context

This change implements the first native macOS adapter using WKWebView (`Agibuild.Fulora.Adapters.MacOS`) to validate the existing v1 contract semantics against a real platform WebView.

The key behavioral constraint is “full-control” native navigation handling:
- WKWebView navigations initiated by web content (link click, `window.location`, redirects) MUST be intercepted before proceeding.
- The adapter MUST consult `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` and honor the allow/deny decision.
- Redirect steps MUST reuse the same `CorrelationId` so the runtime can deterministically reuse one `NavigationId` across the redirect chain.
- `NavigationCompleted` MUST be reported using the host-issued `NavigationId` (and reported exactly-once for that id).

Additionally, we need a minimal end-to-end macOS integration-test (IT) smoke loop to ensure WK behavior matches the contract semantics for:
link-click, 302 redirect, script-driven navigation, cancellation, and minimal script/message-bridge behavior.

Constraints / assumptions:
- This is an M0 adapter: prioritize correctness of correlation/cancellation and basic feature closure over completeness.
- Existing contract surfaces in Core + Adapter Abstractions are treated as the source of truth; this work should be implement-only unless a contract gap is discovered during implementation.
- The macOS adapter should remain modular and replaceable (adapter-as-plugin): no macOS-specific behavior leaks into `Agibuild.Fulora.Core`.

## Goals / Non-Goals

**Goals:**
- Implement `MacOSWebViewAdapter` backed by WKWebView that:
  - intercepts native-initiated main-frame navigations using WK navigation interception hooks
  - calls `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` from the interception callback
  - uses the returned decision:
    - allow -> proceed
    - deny -> cancel and complete the correlated navigation as `Canceled`
  - reuses a stable `CorrelationId` across redirect steps within one logical navigation chain
  - reports `NavigationCompleted` with the host-issued `NavigationId` exactly-once
- Provide macOS IT smoke coverage for:
  - click a link
  - 302 redirect (same correlation chain)
  - `window.location`
  - cancellation via `Cancel=true`
  - minimal `InvokeScriptAsync` + WebMessage bridge receive path

**Non-Goals:**
- Implement every extended capability (devtools, advanced resource interception, multiple-window, full cookie manager, etc.).
- Provide a cross-platform HTTP test server abstraction beyond what the smoke tests require.
- Solve all WK edge cases (subframes, SPA history API, custom schemes) unless required by the existing v1 semantics.

## Decisions

### 1) Navigation interception point in WKWebView

**Decision:** Use `WKNavigationDelegate` policy decision callbacks as the primary interception mechanism for native-initiated navigation, gating main-frame navigations by invoking `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` and completing the WK policy handler with Allow/Cancel.

**Rationale:**
- Policy decision callbacks are the earliest point to deterministically allow/deny a navigation step.
- The adapter needs to deny before the platform commits the navigation in order to fulfill “full-control baseline”.

**Alternatives considered:**
- Intercept only after provisional navigation starts (`didStartProvisionalNavigation`) and attempt to stop: rejected because it is not strictly “before allowing to proceed” and can be timing-sensitive.
- Use `WKURLSchemeHandler` and force all traffic through a custom scheme: rejected as over-scoped for M0 and intrusive to typical HTTP/HTTPS navigation.

### 2) CorrelationId strategy for redirects (deterministic and stable)

**Decision:** Maintain a small per-instance “main-frame navigation chain” state in the macOS adapter:
- When a new main-frame native navigation chain begins, generate a new non-empty `CorrelationId`.
- Reuse that same `CorrelationId` for subsequent main-frame policy callbacks that occur while the chain is still active (including redirect steps observed by WK).
- End the chain and clear the correlation state on the first terminal completion signal (finish/fail/cancel) for that chain.

**Rationale:**
- The v1 semantics rely on `CorrelationId` to correlate redirects without platform-specific redirect shapes.
- WKWebView’s redirect signaling can vary; the adapter owning a single “chain correlation” per active main-frame navigation provides a deterministic rule that can be tested.

**Alternatives considered:**
- Derive correlation from URL patterns (e.g., base URL): rejected because it is not unique and breaks on repeated navigations.
- Use a GUID per policy callback and attempt to stitch redirects later: rejected because it violates the spec requirement to reuse a stable `CorrelationId` across redirect steps.

### 3) Mapping host decision to `NavigationCompleted` and cancel semantics

**Decision:** Treat a denied host decision (`IsAllowed == false`) as a canceled navigation and report `NavigationCompleted` using the host-issued `NavigationId` with status `Canceled`.

**Rationale:**
- The contract semantics define cancellation as the outcome when a handler sets `Cancel=true`.
- For a denied step there may be no WK navigation object to produce a later failure callback; completing immediately avoids hangs and preserves determinism.

**Alternatives considered:**
- Do not emit completion on deny and rely on runtime to synthesize completion: rejected for M0 because the user requirement explicitly calls out using the host-issued `NavigationId` to report `NavigationCompleted` from the adapter path.

### 4) Exactly-once completion and state isolation

**Decision:** Track completion with a per-chain “completion reported” guard to ensure `NavigationCompleted` is emitted exactly once for the active `NavigationId`, regardless of which WK delegate callback delivers the terminal signal.

**Rationale:**
- WKWebView can fire multiple failure-related callbacks in some conditions; guarding prevents double completion which would violate v1 semantics.

### 5) Minimal WebMessage bridge implementation on macOS

**Decision:** Implement receive-only WebMessage bridging for M0 using `WKUserContentController` + script message handlers.

**Rationale:**
- The M0 scope needs a minimal closed loop to prove the wiring: web -> native message delivery and policy gating.
- Receive-only is sufficient to validate policy drop and delivery threading without inventing additional outbound APIs.

**Alternatives considered:**
- Full duplex bridge with outbound “post message” semantics: deferred to a later milestone because it expands surface area and test complexity.

### 6) IT smoke test strategy (real WK, deterministic pages)

**Decision:** Add macOS-only integration smoke tests that:
- run only on macOS
- use an in-process loopback HTTP server to serve deterministic pages and a deterministic 302 redirect chain
- exercise the app-facing `IWebView` surface (navigation events and script/message behaviors), not adapter internals

**Rationale:**
- A real 302 redirect requires HTTP semantics; deterministic pages are easiest via a loopback server.
- Smoke tests should validate the end-to-end behavior of WK + adapter + runtime wiring.

**Alternatives considered:**
- Use `data:`/`about:` pages only: rejected because it cannot model a real 302 redirect chain.
- Rely only on CT (mock adapter): rejected because we specifically need platform behavior validation for WK.

## Risks / Trade-offs

- **[WK redirect callback shape variability]** → Mitigation: define correlation reuse based on an “active main-frame chain” state rather than relying on a specific redirect callback; cover with the 302 smoke scenario.
- **[Async policy decision must call completion handler promptly]** → Mitigation: ensure the adapter always completes the policy handler exactly once; on deny, complete immediately after host decision; avoid blocking the WK callback thread.
- **[Potential deadlock if host decision awaits UI thread while running on UI thread]** → Mitigation: host decision should be designed to be UI-thread-safe; adapter code should avoid re-entering UI-thread marshaling from within the WK callback.
- **[Message handler lifetime / leaks]** → Mitigation: ensure handlers are registered on attach and unregistered on detach/dispose; treat `Detach()` as a hard stop for event emission.
- **[M0 feature gaps (subframes, window.open, advanced resource intercept)]** → Mitigation: explicitly out of scope; keep the adapter structure extensible and isolate WK-specific code behind the macOS adapter project.

## Migration Plan

- No migration steps. This is additive implementation plus tests.
- Rollback: revert adapter implementation changes and macOS smoke tests if they prove unstable in CI.

## Open Questions

- Which WK delegate callback(s) provide the most reliable terminal signal mapping for `Failure` vs `Canceled` vs `Superseded` in the current runtime model, and do we need additional adapter-side heuristics to preserve semantics under rapid navigation churn?
- Do we need outbound WebMessage support in M0 to satisfy any existing higher-level runtime behaviors, or is receive-only sufficient for the requested smoke loop?
