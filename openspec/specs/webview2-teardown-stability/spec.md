## Purpose
Define Windows-specific acceptance criteria for deterministic WebView2 teardown behavior suitable for automation and CI quality gates.

## Requirements

### Requirement: WebView2 teardown is deterministic and automation-verifiable
The Windows WebView2 adapter teardown (Detach / shutdown) SHALL be deterministic and verifiable by automation.

#### Scenario: No Chromium teardown marker output in NuGet smoke
- **WHEN** the NuGet package smoke test runs the app to completion and exits normally
- **THEN** process output contains none of the following marker strings:
  - `Failed to unregister class Chrome_WidgetWin_0`
  - `ui\\gfx\\win\\window_impl.cc:124`

### Requirement: Detach is safe from any caller thread and bounded
`IWebViewAdapter.Detach()` for the Windows adapter SHALL be callable from any thread and SHALL complete without deadlocking.

#### Scenario: Detach called off-UI thread completes promptly
- **WHEN** `Detach()` is called from a non-UI thread while the UI thread is responsive
- **THEN** `Detach()` completes within a bounded time and does not hang indefinitely

#### Scenario: Detach during UI shutdown does not deadlock
- **WHEN** `Detach()` is called while the UI thread is exiting or no longer processing messages
- **THEN** `Detach()` returns without deadlocking and releases best-effort resources

### Requirement: No adapter events after detach
After the Windows adapter begins teardown, it SHALL NOT raise any further adapter events.

#### Scenario: Events are suppressed after detach
- **WHEN** the platform emits navigation, message, resource, download, permission, or new-window signals after teardown begins
- **THEN** the adapter does not raise the corresponding .NET events

### Requirement: Window subclassing is restored on teardown
If the Windows adapter subclasses the parent window procedure to track resize or other lifecycle signals, it SHALL restore the original window procedure during teardown.

#### Scenario: Parent WndProc is restored
- **WHEN** the adapter has attached and later detaches
- **THEN** the parent window procedure is restored to its original value

### Requirement: Pending operations are canceled on teardown
If Attach begins asynchronous initialization and queues operations until readiness, teardown SHALL prevent queued operations from executing after teardown begins and SHALL unblock any waits on readiness.

#### Scenario: Queued operations do not execute after detach
- **WHEN** operations are queued before WebView2 is ready and `Detach()` is called
- **THEN** queued operations are not executed and readiness waits are canceled or faulted deterministically

### Requirement: Teardown unsubscribes native event handlers before releasing COM objects
The Windows adapter SHALL unsubscribe all native event handlers before releasing COM objects or closing the controller.

#### Scenario: WebView2 events are detached before COM release
- **WHEN** teardown starts
- **THEN** the adapter detaches WebView2 event handlers prior to controller close and COM object release

