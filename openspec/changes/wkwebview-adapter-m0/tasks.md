## 1. macOS adapter structure & lifecycle

- [x] 1.1 Ensure `Agibuild.Avalonia.WebView.Adapters.MacOS` builds on macOS and references Core + Abstractions only
- [x] 1.2 Implement `MacOSWebViewAdapter.Initialize(IWebViewAdapterHost host)` and store host/channel context
- [x] 1.3 Implement `Attach(IPlatformHandle parentHandle)` to create/attach WKWebView to the parent native view
- [x] 1.4 Implement `Detach()` to tear down WK delegates/handlers and prevent further event emission
- [x] 1.5 Add guardrails for lifecycle sequencing (Initialize once, no events after Detach)

## 2. WKWebView navigation interception (native-initiated)

- [x] 2.1 Add `WKNavigationDelegate` interception using policy decision callback(s) to gate main-frame navigations
- [x] 2.2 In the interception callback, call `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` with `IsMainFrame=true`, `RequestUri`, and non-empty `CorrelationId`
- [x] 2.3 Implement adapter-side correlation state: generate a new chain `CorrelationId` for a new main-frame chain, reuse it across redirect steps, clear it on terminal completion
- [x] 2.4 Honor host decision deterministically: allow -> proceed; deny -> cancel policy decision and complete as `Canceled`
- [x] 2.5 Track and reuse the host-issued `NavigationId` for all adapter `NavigationCompleted` reporting for the chain
- [x] 2.6 Enforce exactly-once completion per `NavigationId` (guard against duplicate WK terminal callbacks)

## 3. Navigation completion mapping

- [x] 3.1 Map successful completion to `NavigationCompletedStatus.Success` and raise adapter `NavigationCompleted`
- [x] 3.2 Map failures to `NavigationCompletedStatus.Failure` and include the underlying error where available
- [x] 3.3 Ensure denied/canceled steps complete as `NavigationCompletedStatus.Canceled` using the host-issued `NavigationId`
- [x] 3.4 Validate redirect chain emits multiple `NavigationStarted` (runtime) but only one `NavigationCompleted` for the reused `NavigationId`

## 4. Minimal scripting & WebMessage receive path (M0)

- [x] 4.1 Implement `InvokeScriptAsync` using WK JavaScript evaluation and map errors to the existing contract expectations
- [x] 4.2 Register a `WKUserContentController` script message handler for the WebMessage bridge receive path
- [x] 4.3 Wire received messages into adapter `WebMessageReceived` event using Core event args
- [x] 4.4 Ensure message handler registration/unregistration aligns with Attach/Detach to avoid leaks

## 5. macOS Integration Test (IT) smoke suite

- [x] 5.1 Add macOS-only test gating (run condition) for WKWebView smoke tests
- [x] 5.2 Add loopback HTTP server fixture for deterministic pages and a deterministic 302 redirect chain
- [x] 5.3 IT: link click navigation -> observe started/completed correlation
- [x] 5.4 IT: 302 redirect chain -> verify same correlation chain and single completion for reused `NavigationId`
- [x] 5.5 IT: `window.location` script-driven navigation -> observe interception and successful completion
- [x] 5.6 IT: cancellation (`Cancel=true`) -> deny native step and complete as `Canceled`
- [x] 5.7 IT: minimal script + WebMessage receive -> observe script result (if applicable) and `WebMessageReceived`

## 6. Compatibility matrix update (documentation wiring)

- [x] 6.1 Update compatibility matrix content to record macOS/WKWebView M0 acceptance criteria (CT + IT mapping)
- [x] 6.2 Ensure the matrix explicitly mentions the IT smoke scenarios added for macOS/WKWebView

## 7. Verification

- [x] 7.1 Run unit tests on macOS to ensure CT suite remains green
- [x] 7.2 Run macOS IT smoke suite locally and confirm deterministic pass for the required scenarios
