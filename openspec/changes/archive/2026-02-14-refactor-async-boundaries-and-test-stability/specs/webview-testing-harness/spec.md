## MODIFIED Requirements

### Requirement: Deterministic UI-thread dispatcher for contract tests
The test harness SHALL provide a deterministic dispatcher abstraction for tests that:
- can identify the current thread as the UI thread for assertions
- can marshal work to the UI thread deterministically without timing sleeps
- exposes condition-based waiting primitives (`WaitUntil`, `Run`, `Run<T>`) for async contract validation
- centralizes shared threading helpers to avoid per-test duplicated `RunOffThread` / `PumpUntil` variants

#### Scenario: Off-thread calls are marshaled deterministically
- **WHEN** a contract test invokes an async WebView API from a non-UI thread
- **THEN** the harness can deterministically observe adapter invocations and public events on the UI thread

#### Scenario: No sleep-based synchronization in migrated suites
- **WHEN** unit or automation tests wait for asynchronous state changes
- **THEN** they use condition-driven helper primitives instead of fixed `Thread.Sleep(...)` delays
