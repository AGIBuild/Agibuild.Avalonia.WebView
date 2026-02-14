## MODIFIED Requirements

### Requirement: API threading model
The system SHALL enforce the following threading rules:
- All public WebView APIs SHALL be callable from any thread.
- Adapter invocation SHALL be marshaled by runtime internals through a deterministic operation pipeline onto the UI thread.
- Public API calls SHALL NOT require caller-side UI-thread affinity checks.

#### Scenario: Any-thread invocation succeeds without UI-thread contract violation
- **WHEN** `StopAsync()` is called from a non-UI thread
- **THEN** the call does not throw a UI-thread contract exception and completes via the runtime operation pipeline

### Requirement: Stop semantics
`StopAsync()` SHALL cancel the active navigation if one exists and cause it to complete with status `Canceled`.
If no navigation is active, `StopAsync()` SHALL complete with result `false`.

#### Scenario: StopAsync cancels active navigation
- **WHEN** `StopAsync()` is called while a navigation is active
- **THEN** the active navigation completes with status `Canceled`

#### Scenario: StopAsync returns false when idle
- **WHEN** `StopAsync()` is called while no navigation is active
- **THEN** it completes with result `false`

### Requirement: Command navigation semantics
The command navigation APIs (`GoBackAsync`, `GoForwardAsync`, `RefreshAsync`) SHALL be treated as navigation requests and MUST:
- start a new navigation operation with a new `NavigationId` when accepted by the adapter
- raise `NavigationStarted` and `NavigationCompleted` for that `NavigationId`
- be cancelable via `NavigationStarted.Cancel` before invoking the adapter command

#### Scenario: Accepted command raises Started and completes
- **WHEN** `GoBackAsync()` is accepted and the navigation completes successfully
- **THEN** `NavigationStarted` and `NavigationCompleted` are raised for the same `NavigationId` with status `Success`

#### Scenario: Canceled command does not invoke adapter
- **WHEN** a handler cancels the `NavigationStarted` for a command navigation
- **THEN** the adapter command is not invoked and the navigation completes with status `Canceled`

### Requirement: Source and NavigateToString semantics
`Source` SHALL represent the last requested navigation target:
- After `NavigateAsync(uri)` (or `SetSourceAsync(uri)` if provided by the host surface), `Source` SHALL equal `uri`.
- After `NavigateToStringAsync(html)`, `Source` SHALL equal `about:blank`.
- After `NavigateToStringAsync(html, baseUrl)` with non-null `baseUrl`, `Source` SHALL equal `baseUrl`.
- After `NavigateToStringAsync(html, null)`, `Source` SHALL equal `about:blank`.

`NavigateToStringAsync(html)` SHALL raise `NavigationStarted` with `RequestUri=about:blank`.
`NavigateToStringAsync(html, baseUrl)` with non-null `baseUrl` SHALL raise `NavigationStarted` with `RequestUri=baseUrl`.

#### Scenario: NavigateToString sets Source to about:blank
- **WHEN** `NavigateToStringAsync("<html>...</html>")` completes successfully
- **THEN** `Source` equals `about:blank` and the started request URI was `about:blank`

#### Scenario: NavigateToString with baseUrl sets Source to baseUrl
- **WHEN** `NavigateToStringAsync("<html>...</html>", new Uri("https://example.com/"))` completes successfully
- **THEN** `Source` equals `https://example.com/` and the started request URI was `https://example.com/`

## ADDED Requirements

### Requirement: Operation execution is linearized
Adapter-backed operations submitted by public API calls SHALL be linearized by runtime operation queue order.

For any two operations `A` and `B` where `A` is enqueued before `B`, adapter invocation for `A` SHALL begin no later than adapter invocation for `B`.

#### Scenario: FIFO operation order is preserved
- **WHEN** two operations are issued concurrently from different threads
- **THEN** their adapter invocations occur in queue order
