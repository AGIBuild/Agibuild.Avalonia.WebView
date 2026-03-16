## Purpose

Define requirements for the canonical Fulora bootstrap initialization API surface, ensuring a single deterministic entrypoint and removal of legacy aliases.

## Requirements

### Requirement: Bootstrap initialization API SHALL expose `UseFulora` as the single canonical entrypoint
The framework SHALL provide bootstrap initialization extension methods named `UseFulora` for both Avalonia `AppBuilder` and DI `IServiceProvider` startup paths, and SHALL NOT keep parallel legacy alias names that provide the same behavior.

#### Scenario: Avalonia startup uses canonical API
- **WHEN** a desktop host initializes Fulora from `AppBuilder`
- **THEN** startup code uses `UseFulora(...)` as the initialization entrypoint
- **AND** initialization behavior remains equivalent to prior startup semantics

#### Scenario: DI startup uses canonical API
- **WHEN** a host initializes Fulora from a built `IServiceProvider`
- **THEN** startup code uses `UseFulora()` as the initialization entrypoint
- **AND** `WebViewEnvironment` initialization still resolves `ILoggerFactory` from DI

### Requirement: Legacy bootstrap alias removal SHALL be explicit and migratable
The framework SHALL remove the legacy bootstrap alias `UseAgibuildWebView` from supported public startup APIs and SHALL define migration guidance that maps every removed call site to `UseFulora`.

#### Scenario: Legacy alias is absent from supported public startup APIs
- **WHEN** the public startup extension surface is inspected
- **THEN** `UseAgibuildWebView` is not present as a supported extension method
- **AND** `UseFulora` remains available for all previously supported startup contexts

#### Scenario: Migration path is deterministic
- **WHEN** consumers migrate from legacy startup calls
- **THEN** replacing `UseAgibuildWebView(...)` with `UseFulora(...)` preserves initialization intent without additional bootstrap steps
