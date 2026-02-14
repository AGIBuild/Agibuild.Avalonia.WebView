## MODIFIED Requirements

### Requirement: API threading model
The system SHALL enforce the following threading rules:
- Async APIs (`NavigateAsync`, `NavigateToStringAsync`, `InvokeScriptAsync`) SHALL be callable from any thread and SHALL marshal execution to the UI thread.
- Runtime capability APIs (`TryGetWebViewHandleAsync`, cookie/command accessors, bridge operations) SHALL be callable from any thread and SHALL marshal adapter-bound execution to the UI thread.
- Synchronous compatibility APIs, if retained, SHALL be narrow wrappers that do not become the default application path.

#### Scenario: Async APIs marshal to UI thread
- **WHEN** `NavigateAsync` is invoked from a non-UI thread
- **THEN** the adapter navigation invocation occurs on the UI thread

#### Scenario: Async handle access marshals to UI thread
- **WHEN** `TryGetWebViewHandleAsync` is invoked from a non-UI thread
- **THEN** handle retrieval is executed on the UI thread and completes without deadlock

## ADDED Requirements

### Requirement: Pre-attach subscription semantics are deterministic
For public events exposed by the control layer, subscriptions made before core attach SHALL be preserved and bound once attach completes.  
Unsubscription before attach SHALL remove pending handlers and SHALL NOT bind them later.

#### Scenario: Subscribe before attach still receives event
- **WHEN** a consumer subscribes to `ContextMenuRequested` before `_core` is created
- **THEN** the handler is bound during attach and receives the first matching event

#### Scenario: Unsubscribe before attach is honored
- **WHEN** a consumer subscribes and unsubscribes before `_core` is created
- **THEN** no handler is bound after attach
