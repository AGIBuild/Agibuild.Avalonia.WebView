## Requirements

### Requirement: Baseline semantics are versioned and testable
The system SHALL define a Baseline behavioral contract named "WebView Contract Semantics v1".
The v1 semantics SHALL be testable via deterministic Contract Tests (CT) without platform dependencies.

#### Scenario: Semantics are CT-testable
- **WHEN** the CT suite is executed against a mock adapter and deterministic dispatcher
- **THEN** the v1 semantics assertions pass without relying on platform WebView behavior

### Requirement: Event threading guarantees
All public WebView events SHALL be raised on the UI-thread context.
If adapter-originated events are produced on a non-UI thread, the system SHALL marshal them to the UI thread before raising public events.

#### Scenario: Events are always on UI thread
- **WHEN** the adapter raises a navigation event on a non-UI thread
- **THEN** the corresponding public event is observed on the UI thread

### Requirement: API threading model
The system SHALL enforce the following threading rules:
- Async APIs (`NavigateAsync`, `NavigateToStringAsync`, `InvokeScriptAsync`) SHALL be callable from any thread and SHALL marshal execution to the UI thread.
- Sync APIs (`GoBack`, `GoForward`, `Refresh`, `Stop`) SHALL require UI-thread invocation and SHALL throw `InvalidOperationException` when called from non-UI threads.

#### Scenario: Async APIs marshal to UI thread
- **WHEN** `NavigateAsync` is invoked from a non-UI thread
- **THEN** the adapter navigation invocation occurs on the UI thread

#### Scenario: Sync APIs reject non-UI thread calls
- **WHEN** `Stop()` is called from a non-UI thread
- **THEN** the call throws `InvalidOperationException`

### Requirement: Disposed behavior is consistent
After disposal, the WebView SHALL reject API calls consistently:
- Sync APIs SHALL throw `ObjectDisposedException`.
- Async APIs SHALL return Tasks faulted with `ObjectDisposedException` without hanging.
After disposal, the system SHALL NOT raise any further public events.

#### Scenario: Disposed async APIs fail fast
- **WHEN** `InvokeScriptAsync` is called after disposal
- **THEN** the returned Task faults with `ObjectDisposedException`

#### Scenario: No events after disposal
- **WHEN** the adapter attempts to raise a message event after disposal
- **THEN** the public `WebMessageReceived` event is not raised

### Requirement: Navigation correlation and exactly-once completion
Each navigation request SHALL be correlated by a unique `NavigationId`.
For each `NavigationId`, the system SHALL raise `NavigationCompleted` exactly once.

#### Scenario: Completed is exactly-once per NavigationId
- **WHEN** a navigation is started and then completes successfully
- **THEN** exactly one `NavigationCompleted` event is raised for that navigation's `NavigationId`

### Requirement: Navigation cancel semantics
The `NavigationStarted` event SHALL allow cancellation.
If `Cancel=true` is set during `NavigationStarted`, the system SHALL:
- NOT invoke the adapter navigation for that request
- raise `NavigationCompleted` with status `Canceled`
- complete the returned navigation Task successfully (not faulted)

#### Scenario: Cancel prevents adapter navigation
- **WHEN** a handler sets `Cancel=true` in `NavigationStarted`
- **THEN** the adapter does not receive a navigation invocation and `NavigationCompleted` is raised with status `Canceled`

### Requirement: Native-initiated navigations are controllable (full-control baseline)
The v1 Baseline semantics SHALL cover navigations initiated by web content (native-initiated navigations), including:
- link clicks
- script-driven navigations (e.g. `window.location`)
- server redirects

For each native-initiated navigation, the system SHALL:
- consult the adapter-host callback before allowing the native navigation to proceed
- raise the public `NavigationStarted` event for the corresponding `NavigationId`
- allow cancellation via `NavigationStarted.Cancel`

If a handler cancels a native-initiated navigation, the system SHALL:
- deny the navigation via the adapter-host callback decision
- raise `NavigationCompleted` with status `Canceled` for the corresponding `NavigationId`

#### Requirement: Redirect correlation is deterministic
Native-initiated navigations SHALL be correlated deterministically across redirects using an adapter-provided `CorrelationId`:
- For a given `CorrelationId`, the system SHALL assign exactly one `NavigationId`.
- For each redirect step observed via the adapter-host callback with the same `CorrelationId`, the system SHALL:
  - reuse the same `NavigationId`
  - raise `NavigationStarted` again with that same `NavigationId` and the new `RequestUri` (to allow per-step cancellation)
- `NavigationCompleted` SHALL be raised exactly once for that `NavigationId`.
- If a handler cancels any redirect step (sets `Cancel=true`), the system SHALL deny that step and complete the entire correlated navigation with status `Canceled`.

The adapter SHALL NOT reuse a `CorrelationId` concurrently for a different main-frame navigation chain.

#### Scenario: Native-initiated navigation can be canceled
- **WHEN** web content triggers a navigation and a handler sets `Cancel=true` in `NavigationStarted`
- **THEN** the native navigation is denied and `NavigationCompleted` is raised with status `Canceled`

#### Scenario: Redirect steps reuse NavigationId
- **WHEN** a native-initiated navigation triggers a redirect and the adapter-host callback is invoked multiple times with the same `CorrelationId`
- **THEN** each `NavigationStarted` uses the same `NavigationId` and the final `NavigationCompleted` is raised exactly once for that `NavigationId`

### Requirement: Latest-wins navigation concurrency
If a new navigation request begins while a previous navigation is still active, the system SHALL use a Latest-wins rule:
- the previous navigation SHALL complete with status `Superseded`
- the new navigation proceeds normally
The Task returned by the superseded navigation SHALL complete successfully (not faulted).

#### Scenario: Second navigation supersedes the first
- **WHEN** `NavigateAsync(uri1)` is active and `NavigateAsync(uri2)` is invoked
- **THEN** `uri1` completes with status `Superseded` and `uri2` completes independently

### Requirement: Navigation Task completion mapping
The Tasks returned by `NavigateAsync` and `NavigateToStringAsync` SHALL complete only after the corresponding `NavigationCompleted` is raised.
Task completion mapping SHALL be:
- `Success` -> Task completes successfully
- `Failure` -> Task faults with `WebViewNavigationException`
- `Canceled` -> Task completes successfully
- `Superseded` -> Task completes successfully

#### Scenario: Failure faults navigation Task
- **WHEN** a navigation completes with status `Failure`
- **THEN** the returned Task faults with `WebViewNavigationException`

### Requirement: Stop semantics
`Stop()` SHALL cancel the active navigation if one exists and cause it to complete with status `Canceled`.
If no navigation is active, `Stop()` SHALL return `false`.

#### Scenario: Stop cancels active navigation
- **WHEN** `Stop()` is called while a navigation is active
- **THEN** the active navigation completes with status `Canceled`

#### Scenario: Stop returns false when idle
- **WHEN** `Stop()` is called while no navigation is active
- **THEN** it returns `false`

### Requirement: Command navigation semantics
The command navigation APIs (`GoBack`, `GoForward`, `Refresh`) SHALL be treated as navigation requests and MUST:
- start a new navigation operation with a new `NavigationId` when accepted by the adapter
- raise `NavigationStarted` and `NavigationCompleted` for that `NavigationId`
- be cancelable via `NavigationStarted.Cancel` before invoking the adapter command

#### Scenario: Accepted command raises Started and completes
- **WHEN** `GoBack()` is accepted and the navigation completes successfully
- **THEN** `NavigationStarted` and `NavigationCompleted` are raised for the same `NavigationId` with status `Success`

#### Scenario: Canceled command does not invoke adapter
- **WHEN** a handler cancels the `NavigationStarted` for a command navigation
- **THEN** the adapter command is not invoked and the navigation completes with status `Canceled`

### Requirement: Source and NavigateToString semantics
`Source` SHALL represent the last requested navigation target:
- After `NavigateAsync(uri)` (or setting `Source=uri`), `Source` SHALL equal `uri`.
- After `NavigateToStringAsync(html)`, `Source` SHALL equal `about:blank`.
Setting `Source=null` SHALL throw `ArgumentNullException`.
`NavigateToStringAsync(html)` SHALL raise `NavigationStarted` with `RequestUri=about:blank`.

#### Scenario: NavigateToString sets Source to about:blank
- **WHEN** `NavigateToStringAsync("<html>...</html>")` completes successfully
- **THEN** `Source` equals `about:blank` and the started request URI was `about:blank`

### Requirement: Script invocation result and failure mapping
`InvokeScriptAsync` SHALL return a `string?` result.
On script execution failure, the returned Task SHALL fault with `WebViewScriptException`.

#### Scenario: Script failure maps to WebViewScriptException
- **WHEN** the adapter reports a script execution failure
- **THEN** `InvokeScriptAsync` faults with `WebViewScriptException`

### Requirement: WebMessage bridge baseline security
WebMessage bridging SHALL be disabled by default.
When enabled, the system SHALL enforce policy checks before raising `WebMessageReceived`:
- origin allowlist
- protocol/version match
- per-instance channel isolation
Messages failing policy checks SHALL be dropped and SHALL produce a testable diagnostics signal with a drop reason.

#### Scenario: Bridge disabled by default
- **WHEN** the adapter raises a web message without the bridge being enabled
- **THEN** `WebMessageReceived` is not raised

#### Scenario: Policy drops non-allowlisted origin
- **WHEN** a web message is raised from a non-allowlisted origin
- **THEN** the message is dropped and a diagnostics signal is emitted with reason `OriginNotAllowed`

### Requirement: Auth broker baseline semantics
Authentication via `IWebAuthBroker` SHALL require a non-null CallbackUri in options.
Callback matching SHALL be strict on scheme/host/port/absolute-path and SHALL ignore query/fragment differences.
The default session for authentication SHALL be ephemeral/isolated (non-shared cookies/storage).
Authentication results SHALL distinguish at least: `Success`, `UserCancel`, `Timeout`, `Error`.

#### Scenario: Missing CallbackUri is rejected
- **WHEN** `AuthenticateAsync` is invoked with missing CallbackUri
- **THEN** it throws `ArgumentException`

#### Scenario: Strict callback match produces success
- **WHEN** navigation reaches a callback URI that matches scheme/host/port/path
- **THEN** the auth result is `Success` and includes the final callback URI

