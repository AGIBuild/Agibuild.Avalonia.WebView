## ADDED Requirements

### Requirement: Dispatcher contracts are platform-agnostic
The system SHALL define dispatcher contract types in the Core assembly that are platform-agnostic and do not reference platform adapter projects.

#### Scenario: Dispatcher contracts compile without platform dependencies
- **WHEN** a project references the dispatcher contract types from the Core assembly
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebViewDispatcher provides UI-thread identity check
The system SHALL define an `IWebViewDispatcher` interface that can determine whether the current execution context is the UI thread.

#### Scenario: UI thread identity can be checked
- **WHEN** a consumer calls the dispatcher UI-thread identity check
- **THEN** it returns `true` on the UI thread and `false` on a non-UI thread

### Requirement: IWebViewDispatcher provides deterministic marshaling to UI thread
The `IWebViewDispatcher` interface SHALL provide async marshaling methods that allow deterministic execution of work on the UI thread.

#### Scenario: Work can be marshaled to UI thread
- **WHEN** a consumer requests to invoke work via the dispatcher from a non-UI thread
- **THEN** the work is executed on the UI thread and the returned Task completes

