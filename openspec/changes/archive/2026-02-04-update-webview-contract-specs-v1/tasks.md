## 1. Spec & contract alignment (public API)

- [x] 1.1 Review existing Core contracts code and identify required breaking changes (event args fields, enums, exception types)
- [x] 1.2 Update `NavigationStartingEventArgs` to include `NavigationId` and keep `RequestUri` + `Cancel` semantics
- [x] 1.3 Update/add `NavigationCompletedEventArgs` to include `NavigationId`, `RequestUri`, `Status`, and `Error` mapping rules
- [x] 1.4 Add enums: `NavigationCompletedStatus`, `WebAuthStatus`, `WebMessageDropReason`
- [x] 1.5 Add exception types: `WebViewNavigationException`, `WebViewScriptException`
- [x] 1.6 Update `WebMessageReceivedEventArgs` to include `Origin` and `ChannelId`

## 2. Core semantics implementation (v1 baseline)

- [x] 2.1 Introduce a deterministic UI-thread dispatcher abstraction used by Core to marshal async API execution and public events
- [x] 2.2 Implement API threading rules: async APIs marshal; sync APIs require UI thread and throw `InvalidOperationException` off-thread
- [x] 2.3 Implement disposal semantics: sync throws `ObjectDisposedException`; async tasks fault with `ObjectDisposedException`; ignore adapter events after disposal
- [x] 2.4 Add `NavigationId` generation for each navigation request (including Source set, GoBack/Forward/Refresh)
- [x] 2.5 Implement navigation ordering: raise `NavigationStarted` then `NavigationCompleted` exactly-once per `NavigationId`
- [x] 2.6 Implement cancel-in-started semantics: cancel prevents adapter navigation, emits `Completed(Canceled)`, and completes navigation Task successfully
- [x] 2.7 Implement Latest-wins: new navigation supersedes active one with `Completed(Superseded)` and completes the superseded Task successfully
- [x] 2.8 Implement navigation Task completion mapping: `Failure` faults with `WebViewNavigationException`; others complete successfully
- [x] 2.9 Implement `Stop()` semantics: cancels active navigation -> `Completed(Canceled)`; idle stop returns `false`
- [x] 2.10 Implement `Source` and `NavigateToStringAsync` semantics: `about:blank` behaviors as specified

## 3. WebMessage bridge baseline security & diagnostics

- [x] 3.1 Add explicit opt-in mechanism for enabling WebMessage bridge (default disabled)
- [x] 3.2 Implement policy checks: origin allowlist, protocol/version match, channel isolation per WebView instance
- [x] 3.3 Add a testable drop-diagnostics mechanism (sink or counters) emitting `WebMessageDropReason` with origin + channel
- [x] 3.4 Ensure `WebMessageReceived` is raised only after policy passes and always on UI thread

## 4. Auth broker baseline semantics

- [x] 4.1 Enforce `CallbackUri` presence and throw `ArgumentException` when missing
- [x] 4.2 Implement strict callback match rules (scheme/host/port/path; ignore query/fragment)
- [x] 4.3 Ensure default auth session is ephemeral/isolated (non-shared cookies/storage) per spec
- [x] 4.4 Return distinct `WebAuthStatus` values for Success/UserCancel/Timeout/Error

## 5. Testing harness & contract tests (deterministic)

- [x] 5.1 Extend `MockWebViewAdapter` to simulate navigation outcomes: Success/Failure/Canceled/Superseded
- [x] 5.2 Extend `MockWebViewAdapter` to simulate WebMessage inputs with origin/protocol/channel metadata
- [x] 5.3 Implement deterministic `TestDispatcher` that can assert UI thread identity and marshal deterministically (no sleeps)
- [x] 5.4 Add CT: all public events are raised on UI thread
- [x] 5.5 Add CT: `NavigationCompleted` is exactly-once per `NavigationId`
- [x] 5.6 Add CT: cancel in `NavigationStarted` prevents adapter navigation and completes as `Canceled`
- [x] 5.7 Add CT: Latest-wins supersedes active navigation with `Superseded`
- [x] 5.8 Add CT: navigation failure faults with `WebViewNavigationException`
- [x] 5.9 Add CT: script failure faults with `WebViewScriptException`
- [x] 5.10 Add CT: WebMessage bridge disabled by default
- [x] 5.11 Add CT: WebMessage drops are observable with `WebMessageDropReason`

## 6. Documentation sync (non-OpenSpec)

- [x] 6.1 Sync/align `docs/agibuild_webview_contract_semantics_v1.md` with implemented API/type names (if renamed during implementation)
- [x] 6.2 Sync/align `docs/agibuild_webview_contract_tests_v1.md` with actual CT names and coverage
- [x] 6.3 Sync/align `docs/agibuild_webview_compatibility_matrix_proposal.md` with the new OpenSpec `webview-compatibility-matrix` spec

