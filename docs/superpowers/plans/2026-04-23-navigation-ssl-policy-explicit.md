# Navigation SSL Policy Explicit — Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Status:** Accepted. Implementation tracked by this plan.
**Goal:** Make SSL/server-certificate error handling in every platform WebView adapter an **explicit, auditable, structurally reported rejection path**, instead of relying on framework-default "cancel on invalid cert" behavior. Public behavior is unchanged (still reject). All SSL failures gain a uniform `WebViewSslException` carrying host + certificate summary fields.
**Architecture:** Introduce a single internal strategy interface `INavigationSecurityHooks`. All platform adapters call into this one hook when their native layer observes a certificate error. The only production implementation (`DefaultNavigationSecurityHooks`) returns `Reject`. v2.0 may promote the interface to public and add `Proceed` with a per-domain pinning policy — out of scope for this change.
**Tech Stack:** .NET 10, C# 12, Android (`Android.Webkit.WebViewClient.OnReceivedSslError`), WKWebView (`webView:didReceiveAuthenticationChallenge:` via Objective-C shim), WebView2 (`CoreWebView2.ServerCertificateErrorDetected`), WebKitGTK (`load-failed-with-tls-errors`).

---

## Scope Guardrails

- Do not introduce any `Proceed` / "trust-on-first-use" path in v1. The only `NavigationSecurityDecision` value is `Reject`.
- Do not make `INavigationSecurityHooks` public in v1. That decision is reserved for v2.0 (see `docs/superpowers/plans/2026-04-23-fulora-v2-public-api-breakage.md`).
- Do not migrate Apple-side URL error mapping (`map_nsurl_error_to_category`) away from the shim. It is a bona fide second trigger path (for main-frame failures that WebKit handles before the delegate is consulted) and must also route through the same managed hook.
- Do not remove the existing `NavigationCompleted → NavigationErrorCategory.Ssl` path. Enrich it; do not replace it.
- Do not alter any public API member in this change. Only additive internal surface.
- Do not add per-domain allowlist / pinning configuration. Defer to v2.

## Release Policy

- Target release line: `1.6.x`.
- Lands in a single feature branch, one proposal commit (this plan already present) plus implementation commits. No capability registry changes required — SSL rejection is a latent invariant, not a capability. Update `docs/framework-capabilities.json` only if we decide to expose an observable capability id (proposed: none).
- `release-gate` CI job must stay green. `governance` and `docs` test projects must stay green.

## Rejected Alternatives

| Alternative | Rejection reason |
|---|---|
| Inline `handler.Cancel()` + log line in each of the four adapters | Four copies of a cross-cutting security decision. Fails the "one service owns one concern" rule. Future per-domain pinning would require editing four files. |
| Ship the hook as `public` in 1.6.x and let hosts inject their own policy | Public API breakage. The hook surface is also a security-critical extension point that must be validated against per-domain pinning, mTLS, and audit requirements — none of which are designed yet. Ship internal in 1.x, promote in v2. |
| Swallow SSL errors silently (status quo on Android / Apple) | Relies on framework default behavior. Any framework change or accidental override silently erodes the security boundary. Not acceptable for an architectural invariant. |
| Replace the whole `NavigationCompleted` error channel with a dedicated `SecurityViolation` event | A separate channel fragments the failure model. Host code already handles `NavigationCompleted(Failure, WebViewSslException)`. Enrich `WebViewSslException`, do not fork. |
| Rely solely on WebView2's `NavigationCompleted.WebErrorStatus` for Windows | Already in place and does reject; but it cannot carry certificate subject/issuer. Subscribing to `ServerCertificateErrorDetected` is the only way to surface structured cert detail. |

## Architectural Invariants

- **Every SSL failure on every platform produces a `NavigationCompleted(Failure, WebViewSslException)` event.** No exceptions. No "silent reject".
- **Every platform adapter routes the decision through `INavigationSecurityHooks`.** The adapter does not decide on its own.
- **The only `NavigationSecurityDecision` in v1 is `Reject`.** `DefaultNavigationSecurityHooks` returns `Reject` unconditionally. No environment variable, no debug flag, no per-build override.
- **`WebViewSslException` carries the host, error summary, and — where the platform makes them available — certificate subject, issuer, and validity window.** Adapters populate what they can; Core never rejects a context for missing optional fields. Platform-by-platform availability of certificate metadata is a *capability*, not an invariant: Windows (WebView2) and GTK (WebKitGTK) typically expose subject/issuer/validity via the native event payload; Android (deliberately aligned with Avalonia.Controls.WebView's `OnReceivedError` routing — see Task 2 amendment) and Apple (until Task 3 lands in v2) leave them `null`.
- **The hook is `internal`.** Promotion is a v2.0 decision, paired with per-domain pinning policy design.

---

## Task 1 — Core Security Contracts

**Rationale.** A single place that owns the decision + the data shape. All adapters and tests target this contract.

**Files to add:**

- `src/Agibuild.Fulora.Core/Security/NavigationSecurityDecision.cs`
  - `internal enum NavigationSecurityDecision { Reject = 0 }`
- `src/Agibuild.Fulora.Core/Security/ServerCertificateErrorContext.cs`
  - `internal sealed record ServerCertificateErrorContext(Uri RequestUri, string Host, string ErrorSummary, int PlatformRawCode, string? CertificateSubject, string? CertificateIssuer, DateTimeOffset? ValidFrom, DateTimeOffset? ValidTo)`
- `src/Agibuild.Fulora.Core/Security/INavigationSecurityHooks.cs`
  - `internal interface INavigationSecurityHooks { NavigationSecurityDecision OnServerCertificateError(ServerCertificateErrorContext context); }`
- `src/Agibuild.Fulora.Core/Security/DefaultNavigationSecurityHooks.cs`
  - `internal sealed class DefaultNavigationSecurityHooks : INavigationSecurityHooks` with `static INavigationSecurityHooks Instance { get; }`; always returns `Reject`. `ArgumentNullException.ThrowIfNull` on the context.

**Files to modify:**

- `src/Agibuild.Fulora.Core/Errors/WebViewSslException.cs`
  - Add optional fields: `Host`, `CertificateSubject`, `CertificateIssuer`, `ValidFrom`, `ValidTo`, `ErrorSummary`.
  - Add new ctor: `WebViewSslException(ServerCertificateErrorContext ctx, Guid navigationId)`.
  - Preserve the existing ctor (`string message, Guid navId, Uri uri`). Its message continues to be emitted when adapters cannot build a context (should never happen in this change, but the legacy ctor stays as a safety net).

**Acceptance:**

- Added types are `internal` and carry XML docs.
- `WebViewSslException` existing ctor signatures are unchanged (no public breakage).
- New unit tests in `tests/Agibuild.Fulora.UnitTests`:
  - `DefaultNavigationSecurityHooksTests` — 3 facts: returns `Reject`, throws on null ctx, is safe to call concurrently.
  - `WebViewSslExceptionTests` — extended field round-trip via new ctor.

- [x] Task 1a — add the four new files in `Core/Security/`.
- [x] Task 1b — extend `WebViewSslException`.
- [x] Task 1c — unit tests for `DefaultNavigationSecurityHooks` + extended exception.

---

## Task 2 — Android Adapter Explicit Hook Routing

> **Amendment (2026-04-25):** The original wording required `override OnReceivedSslError` to extract leaf-certificate metadata from `SslError.Certificate`. The landed implementation routes SSL errors through `OnReceivedError` instead, intentionally aligned with `Avalonia.Controls.WebView` (which itself relies on `OnReceivedError` for SSL diagnostics on API 23+). The Android `WebResourceError` payload does **not** expose the leaf certificate, so `CertificateSubject` / `Issuer` / `ValidFrom` / `ValidTo` are deliberately left `null` on Android. This is consistent with the new top-level invariant: certificate metadata is a per-platform *capability*, not a contract guarantee. The hook is still invoked, the decision is still auditable, and the Android-side behavior remains "always reject".
>
> **Factory-wiring amendment.** The original "factory passes the hook through" wording is incorrect: `WebViewAdapterFactory` discovers adapters via reflection through `WebViewAdapterRegistry` and only instantiates the parameter-less ctor. Production wiring is therefore: parameter-less ctor → `DefaultNavigationSecurityHooks.Instance`; an `internal` ctor accepts an explicit hook for tests. **The same pattern applies to every platform adapter in Tasks 4 and 5.** No `WebViewAdapterFactory` changes are required for any task in this plan.

**Rationale.** `AdapterWebViewClient` must route the SSL error surface through `INavigationSecurityHooks` so the policy decision is explicit, auditable, and testable. Whether the hook is invoked from `OnReceivedSslError` or from `OnReceivedError` (after detecting an SSL `ClientError` value) is a platform-routing choice; both reach the same hook with the same `ServerCertificateErrorContext`.

**Files to modify:**

- `src/Agibuild.Fulora.Platforms/Android/AndroidWebViewAdapter.cs`
  - Constructor: parameter-less ctor delegates to an `internal` ctor that accepts `INavigationSecurityHooks`, defaulting to `DefaultNavigationSecurityHooks.Instance`.
  - `OnReceivedError` recognizes SSL `ClientError` values (`FailedSslHandshake`, `Authentication`) and dispatches to `CreateSslExceptionViaHook`, which:
    1. Builds `ServerCertificateErrorContext` (URL, host from the request URI, summary = `errorCode.ToString()`, raw code = `(int)errorCode`; cert subject/issuer/validity left `null` because Android's `WebResourceError` does not expose them).
    2. Invokes `_securityHooks.OnServerCertificateError(ctx)`.
    3. Returns a `WebViewSslException(ctx, navId)` that the existing `OnReceivedError` → `OnPageFinished` pipeline raises as `NavigationCompleted(Failure, WebViewSslException)`.
  - The Android system already cancels the navigation when `OnReceivedError` fires for an SSL category; no explicit `handler?.Cancel()` call is required (in contrast to `OnReceivedSslError`'s API contract).

**Acceptance:**

- Manual trigger path covered by the cross-platform `AdapterSslRejectionContract` (Task 6) when adapter integration lanes pick it up; Facts 1-3 must pass on Android.
- Fact 4 (`exposes_certificate_subject_and_issuer_when_available`) is *not* asserted on Android in 1.6.x — the contract guards it with the platform's optional capability marker (see Task 6 amendment).
- Code path is reachable under unit test via `MockWebViewAdapter.TriggerServerCertificateError` (see Task 6) by passing `subject = null, issuer = null` to mirror Android's behavior.

- [x] Task 2a — route SSL `ClientError` values through `INavigationSecurityHooks` in `OnReceivedError`.
- [x] Task 2b — internal-ctor seam for `AndroidWebViewAdapter`; parameter-less ctor wires `DefaultNavigationSecurityHooks.Instance` (no factory change).

---

## Task 3 — Apple (iOS + macOS) Explicit Authentication Challenge — **CANCELLED (Branch B)**

> **Branch B decision (2026-04-25):** Implementing the new
> `didReceiveAuthenticationChallenge:` delegate in the *existing* Objective-C
> shim (`WkWebViewShim.mm` / `WkWebViewShim.iOS.mm`) is intentionally **not**
> done in the 1.6.x line. The Apple SSL hook is folded into the v2 plan
> `docs/superpowers/plans/2026-04-25-fulora-v2-apple-shim-modernization.md`
> Phase 4, where it lands together with the C# runtime-delegate replacement of
> the shim. Touching the legacy `.mm` files only to add one delegate would
> incur an Objective-C build round-trip that the v2 plan is about to delete
> anyway, and would force the same code to be re-written twice. The status quo
> on Apple in 1.6.x remains: `WKWebView` rejects invalid certificates via the
> default `NSURLSession` server-trust path; `WebViewSslException` is still
> emitted via `map_nsurl_error_to_category` from `didFailProvisionalNavigation:`.
> Sub-tasks 3a / 3b / 3c / 3d are intentionally left unchecked and are tracked
> in the v2 plan instead.

**Rationale (retained for context).** WKWebView delegates `didReceiveAuthenticationChallenge:` for server-trust evaluation. It is currently unimplemented; WKWebView falls back to `NSURLSession`-level default handling. Implementing this delegate explicitly, and routing every `NSURLAuthenticationMethodServerTrust` challenge through the hook, makes the policy decision auditable.

**Files to modify:**

- `src/Agibuild.Fulora.Platforms/MacOS/Native/WkWebViewShim.mm`
  - Add a C ABI callback slot to the existing `shim_callbacks` struct: `on_server_certificate_error(user_data, url_utf8, host_utf8, summary_utf8, raw_code, subject_utf8_or_null, issuer_utf8_or_null, valid_from_unix_seconds_or_zero, valid_to_unix_seconds_or_zero)`.
  - Implement `- (void)webView:(WKWebView*)webView didReceiveAuthenticationChallenge:(NSURLAuthenticationChallenge*)challenge completionHandler:(...)`:
    - If `[challenge.protectionSpace.authenticationMethod isEqualToString:NSURLAuthenticationMethodServerTrust]`, extract `SecTrustRef`, pull the leaf cert (`SecTrustGetCertificateAtIndex(trust, 0)`), read subject / issuer via `SecCertificateCopySubjectSummary` / `SecCertificateCopyValues`, invoke `on_server_certificate_error`, then `completionHandler(NSURLSessionAuthChallengeRejectProtectionSpace, nil)` to refuse the challenge.
    - Otherwise, `completionHandler(NSURLSessionAuthChallengePerformDefaultHandling, nil)`.
  - `map_nsurl_error_to_category` path stays — it is the fallback when WKWebView fails the navigation before the delegate is consulted (e.g., low-level TLS handshake aborts). Route those failures through the same new callback as well, by invoking `on_server_certificate_error` from the SSL branch of `didFailProvisionalNavigation:` (with whatever fields we can extract from `NSError.userInfo[@"NSErrorPeerCertificateChainKey"]` — best-effort, may be nil on some OS versions).
- `src/Agibuild.Fulora.Adapters.iOS/Native/WkWebViewShim.iOS.mm`
  - Mirror the macOS changes. Identical logic.
- `src/Agibuild.Fulora.Platforms/MacOS/MacOSWebViewAdapter.cs` (and `MacOSWebViewAdapter.PInvoke.cs`) + `src/Agibuild.Fulora.Adapters.iOS/iOSWebViewAdapter.cs`:
  - Register a managed callback for `on_server_certificate_error` that builds `ServerCertificateErrorContext`, calls `_securityHooks.OnServerCertificateError(ctx)`, and raises `NavigationCompleted(Failure, WebViewSslException(ctx, navId))`.
  - Accept `INavigationSecurityHooks` in the ctor, default `DefaultNavigationSecurityHooks.Instance`.
- `src/Agibuild.Fulora.Runtime/WebViewAdapterFactory.cs`
  - Pass the hook through for `MacOSWebViewAdapter` / `iOSWebViewAdapter` the same way as Android.

**Acceptance:**

- Managed callback signature is `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]` for AOT compatibility (matches existing shim callbacks in these adapters).
- Unit test (via `MockWebViewAdapter.TriggerServerCertificateError` where the adapter exercises the same hook path) asserts rejection + structured exception.

- [~] Task 3a — extend shim C ABI (`shim_callbacks` struct + signature). **CANCELLED — moved to v2 Phase 4.**
- [~] Task 3b — implement `didReceiveAuthenticationChallenge:` in both Objective-C shims. **CANCELLED — moved to v2 Phase 4.**
- [~] Task 3c — wire managed callback + hook into `MacOSWebViewAdapter` / `iOSWebViewAdapter`. **CANCELLED — moved to v2 Phase 4.**
- [~] Task 3d — route the existing `map_nsurl_error_to_category` SSL branch through the same new callback. **CANCELLED — moved to v2 Phase 4.**

---

## Task 4 — Windows (WebView2) Explicit Subscription

> **Amendment (2026-04-25):**
>
> **C1 — Cancel, not AlwaysDeny:** WebView2 exposes `CoreWebView2ServerCertificateErrorAction.Cancel` for rejecting the certificate error. The plan’s Step 3 wording (`e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysDeny`) was incorrect — `AlwaysDeny` is not a public SDK value; the implementation sets `Cancel`, which is the semantic rejection action.
>
> **C2 — No `Host` on the event args:** `CoreWebView2ServerCertificateErrorDetectedEventArgs` provides `RequestUri` (string) and `ErrorStatus`, not `Host`. The host is derived with `new Uri(e.RequestUri).Host` (or equivalent parsing).
>
> **C3 — `ValidFrom` / `ValidTo` are `DateTime` on `CoreWebView2Certificate`:** WebView2 does not document `DateTime.Kind`. The implementation normalizes with `DateTime.SpecifyKind(dt, DateTimeKind.Utc)` before building `DateTimeOffset` values for `ServerCertificateErrorContext`, so validity windows are unambiguous UTC rather than relying on default local/UTC conversion — treat this as the canonical pattern for any future cert-time interop.
>
> **C4 — Event ordering:** Microsoft Learn documents `NavigationCompleted` **before** `ServerCertificateErrorDetected`, not after. The plan’s Step 4 (“`OnNavigationCompleted` then reads it”) implied the opposite. The implementation therefore uses a **deferred** model: for SSL-categorized completions, `OnNavigationCompleted` records state keyed by request URI in `_deferredSslNavigationByUri`; `OnServerCertificateErrorDetected` then dequeues, builds `ServerCertificateErrorContext`, invokes the hook, and completes the navigation-terminal path. `QueueDeferredSslFallbackFlush` covers navigations where `ServerCertificateErrorDetected` never fires, synthesizing a minimal context.
>
> *Implementation steps below ("Files to modify") are partially superseded by C1–C4. The C1/C2/C3/C4 corrections take precedence over any conflicting wording in those bullets.*

**Rationale.** `CoreWebView2.ServerCertificateErrorDetected` is the only WebView2 event that carries certificate detail. The existing `NavigationCompleted.WebErrorStatus → NavigationErrorCategory.Ssl` path only knows the category; it cannot populate host / subject / issuer / validity.

**Files to modify:**

- `src/Agibuild.Fulora.Platforms/Windows/WindowsWebViewAdapter.cs`
  - Inject `INavigationSecurityHooks` (ctor default `DefaultNavigationSecurityHooks.Instance`).
  - Subscribe `_webView.CoreWebView2.ServerCertificateErrorDetected += OnServerCertificateErrorDetected;` inside the existing event hookup block.
  - `private void OnServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)`:
    1. Build `ServerCertificateErrorContext` from `e.RequestUri`, `e.Host`, `e.ErrorStatus.ToString()`, `(int)e.ErrorStatus`, and the fields of `e.ServerCertificate` (`Subject`, `Issuer`, `ValidFrom`, `ValidTo`).
    2. `var decision = _securityHooks.OnServerCertificateError(ctx);`
    3. `e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysDeny;` — skips the built-in WebView2 cert error UI and fails the navigation cleanly.
    4. Record the context keyed by navigation id (internal dictionary); the existing `OnNavigationCompleted` path then reads it and constructs `WebViewSslException(ctx, navId)` instead of the plain message built from `MapWebErrorStatus`.
  - `MapWebErrorStatus` stays as a fallback for navigations where `ServerCertificateErrorDetected` does not fire (e.g., revocation errors surfaced only via `NavigationCompleted`): when a SSL-category error arrives without a stored context, synthesize a minimal `ServerCertificateErrorContext(requestUri, requestUri.Host, status.ToString(), (int)status, null, null, null, null)` and call the hook for observability parity.
  - **No `WebViewAdapterFactory` change required.** Wire the hook the same way `AndroidWebViewAdapter` does: parameter-less ctor → `DefaultNavigationSecurityHooks.Instance`; `internal WindowsWebViewAdapter(INavigationSecurityHooks hook)` for tests.

**Acceptance:**

- `ServerCertificateErrorDetected` subscription covered by a unit test that wraps the event through a `CoreWebView2` test double. If that double is infeasible, a thin integration test in the Windows integration lane is acceptable.
- `WebViewSslException` emitted on Windows now carries `Subject` + `Issuer` whenever WebView2 provides them.

- [x] Task 4a — subscribe `ServerCertificateErrorDetected` + handler.
- [x] Task 4b — thread context through to `OnNavigationCompleted` for structured `WebViewSslException`.
- [x] Task 4c — fallback path for SSL errors without a captured context.

---

## Task 5 — GTK Shim Enrich Certificate Fields

> **Amendment (2026-04-25):** Subject and issuer are emitted as X.509 distinguished-name strings (from `g_tls_certificate_get_subject_name` / `g_tls_certificate_get_issuer_name`), not PEM blobs. The `subject_pem_or_null` / `issuer_pem_or_null` names in the original plan text are descriptive only; the C ABI parameters are `subject_dn_utf8_or_null` / `issuer_dn_utf8_or_null`.

**Rationale.** The GTK path already routes TLS failures through a managed callback but only passes the error bitmask. Subject/issuer are available on `GTlsCertificate`; adding them to the callback signature brings GTK into parity with the other platforms.

**Files to modify:**

- `src/Agibuild.Fulora.Platforms/Gtk/Native/WebKitGtkShim.c`
  - Extend `on_navigation_completed` C callback signature: add optional `host`, `summary`, `subject_pem_or_null`, `issuer_pem_or_null`, `valid_from_unix_or_zero`, `valid_to_unix_or_zero` arguments. All existing non-SSL callers pass null/zero.
  - In `on_load_failed_tls`, extract:
    - host from `failing_uri` (parsed via `g_uri_parse`)
    - summary from `g_tls_certificate_flags_to_string(errors)` (or manual switch if not available)
    - subject/issuer via `g_tls_certificate_get_subject_name` / `g_tls_certificate_get_issuer_name` (GLib 2.70+; fall back to nulls if unavailable)
    - validity via `g_tls_certificate_get_not_valid_before` / `_after`
- `src/Agibuild.Fulora.Platforms/Gtk/GtkWebViewAdapter.cs` (and any PInvoke declaration)
  - Update the managed callback signature to match.
  - When receiving an SSL category, build `ServerCertificateErrorContext` and route through `INavigationSecurityHooks` + `WebViewSslException(ctx, navId)`.
  - Constructor: parameter-less ctor → `DefaultNavigationSecurityHooks.Instance`; `internal GtkWebViewAdapter(INavigationSecurityHooks hook)` for tests. **No `WebViewAdapterFactory` change required** (same rationale as Tasks 2 & 4).

**Acceptance:**

- Existing non-SSL callers continue to work (pass null/zero for new fields). Existing GTK integration tests still pass.
- When an SSL failure is reproduced, the emitted `WebViewSslException` carries host + summary; subject/issuer/validity are populated when the GLib version supports them.

- [x] Task 5a — extend `on_navigation_completed` C signature + managed PInvoke.
- [x] Task 5b — enrich `on_load_failed_tls` with cert detail extraction.
- [x] Task 5c — route through `INavigationSecurityHooks` in `GtkWebViewAdapter`.

---

## Task 6 — Cross-Platform Contract Fixture + Mock Trigger

**Rationale.** Mirror `AdapterCookieContract` / `MockCookieAdapterContractTests` (landed in `24c4079`). One reusable contract, one mock runner today, real-platform lanes wire it in as integration tests adopt platform CIs.

**Files to add:**

- `tests/Agibuild.Fulora.UnitTests/AdapterSslRejectionContract.cs`
  - `internal static class AdapterSslRejectionContract` with the same factory-delegate shape as `AdapterCookieContract`. Each fact is a `public static async Task` taking the trigger-factory delegate.
  - Facts:
    1. `ServerCertificateError_raises_NavigationCompleted_with_Failure_status(TriggerFactory factory)` — asserts a `NavigationCompleted` event fires with `Status == Failure`.
    2. `ServerCertificateError_exception_is_WebViewSslException_with_host_and_summary(TriggerFactory factory)` — asserts the event payload is `WebViewSslException` carrying non-empty `Host` and `ErrorSummary`.
    3. `ServerCertificateError_always_cancels_navigation(TriggerFactory factory)` — asserts the same `NavigationId` does not subsequently raise a `Success` completion.
    4. **`ServerCertificateError_propagates_certificate_metadata_when_supplied(TriggerFactory factory)`** — *opt-in capability fact*: when the trigger factory provides non-null `subject`/`issuer`/`validity`, the resulting `WebViewSslException` carries them through verbatim. Adapters whose native event payload does not expose certificate metadata (Android, Apple-1.6.x) skip this fact via `Skip = ...` on their per-platform contract test class. This is consistent with the top-level invariant amendment in *Architectural Invariants*. **`MockSslRejectionContractTests` always runs Fact 4** because the mock trigger supplies full metadata; platform-specific contract fixtures opt out per adapter with `Skip` where the native surface is metadata-poor.
- `tests/Agibuild.Fulora.UnitTests/MockSslRejectionContractTests.cs`
  - `public sealed class MockSslRejectionContractTests` calling each contract fact with a factory that constructs a `MockWebViewAdapter` and calls its new `TriggerServerCertificateError` method.

**Files to modify:**

- `tests/Agibuild.Fulora.Testing/MockWebViewAdapter.cs`
  - Add a `public void TriggerServerCertificateError(Uri uri, string? subject = null, string? issuer = null, DateTimeOffset? validFrom = null, DateTimeOffset? validTo = null, string? errorSummary = null, int platformRawCode = 0)` method that builds `ServerCertificateErrorContext`, invokes the configured `INavigationSecurityHooks`, and raises `NavigationCompleted(Failure, WebViewSslException(ctx, navId))`. The method must use the adapter's existing `LastNavigationId` / `LastNavigationUri` bookkeeping the same way the other `Raise*` helpers do, so the contract's "navigation does not advance past the failed URI" fact (Fact 3) is observable.
  - Mock accepts an optional `INavigationSecurityHooks` via a new `internal` ctor overload, defaulting to `DefaultNavigationSecurityHooks.Instance`. The existing parameter-less factory (`MockWebViewAdapter.Create()`) keeps its current behavior — it still produces a mock with the default hook. A new factory `MockWebViewAdapter.CreateWithSecurityHook(INavigationSecurityHooks hook)` lets contract tests inject a recording hook to assert "the hook was invoked exactly once with the expected `ServerCertificateErrorContext`".

**Acceptance:**

- Mock runs the full contract green.
- Contract is ready for platform adapter lanes to adopt as integration tests run against real self-signed Kestrel endpoints (separate enablement task, not in this plan).

- [x] Task 6a — `MockWebViewAdapter.TriggerServerCertificateError`.
- [x] Task 6b — `AdapterSslRejectionContract`.
- [x] Task 6c — `MockSslRejectionContractTests`.

---

## Task 7 — Documentation and Evidence

**Files to modify:**

- `docs/API_SURFACE_REVIEW.md`
  - Add section "1.6 Navigation SSL Policy (Additive)" documenting the new internal contracts, the explicit-rejection invariant, and the promotion plan for v2.
- `docs/CHANGELOG.md` (or `CHANGELOG.md` — confirm during impl)
  - Under `## [1.6.7]` (or the version this change ships in): "Security: SSL/server-certificate failures on every platform now route through an explicit `INavigationSecurityHooks` decision and emit `WebViewSslException` with host + certificate summary. Behavior unchanged; rejection is still the only outcome."
- `docs/platform-status.md`
  - Update the "Security gate status" row to reference the explicit rejection path as evidence.

**Acceptance:**

- `docs/API_SURFACE_INVENTORY.release.txt` is unchanged (no public API delta). If it changes, that is a bug in Task 1.

- [x] Task 7a — update `API_SURFACE_REVIEW.md`.
- [x] Task 7b — CHANGELOG entry + platform-status.md line.

---

## Verification Checklist (agent must tick ALL before commit)

- [ ] `./build.sh LocalPreflight` green (all test projects pass, format clean).
- [ ] `docs/API_SURFACE_INVENTORY.release.txt` unchanged (no public API delta).
- [ ] Coverage ≥ 93 % for `Agibuild.Fulora.Core` and `Agibuild.Fulora.Runtime`.
- [ ] `AdapterSslRejectionContract` + `MockSslRejectionContractTests` green locally.
- [ ] Existing cookie contract + cookie integration tests still green (regression guard).
- [ ] No new `catch { }` without `/* reason */`; Slopwatch remains green.
- [ ] `./build.sh UpdateVersion` run before the final commit so the version bumps in the same sequence used by previous release-line commits.

## Success Criteria

- Every platform's WebView adapter explicitly `override`s / subscribes to the SSL error surface of its native layer. No implicit default-behavior dependency remains.
- A single internal strategy (`INavigationSecurityHooks`) owns the decision.
- A single data carrier (`ServerCertificateErrorContext`) owns the payload shape.
- Every SSL failure reaches consumers as `NavigationCompleted(Failure, WebViewSslException{Host, ErrorSummary, …})`.
- CI is green.
