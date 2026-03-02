## Purpose

Define remote configuration and feature flag support for Agibuild.Fulora, with IConfigProvider interface, local JSON fallback, optional remote HTTP endpoint, and bridge-integrated JS access.

## Requirements

### Requirement: IConfigProvider interface
Runtime SHALL provide an `IConfigProvider` interface with methods to read configuration values and sections.

#### Scenario: GetValue returns typed value
- **WHEN** C# or JS calls `GetValue<T>(key)` (or equivalent) with a valid key
- **THEN** the provider SHALL return the value for that key, or default if not found
- **AND** the value SHALL be deserialized to the requested type (string, bool, int, etc.)

#### Scenario: GetSection returns nested config
- **WHEN** C# or JS calls `GetSection(key)` with a key denoting a section (e.g., "Features")
- **THEN** the provider SHALL return a JSON object or dictionary of the nested values
- **AND** the result SHALL be merge of local and remote (remote overrides local when available)

### Requirement: Local JSON fallback
A built-in provider SHALL support local JSON file as fallback when remote is unavailable or on error.

#### Scenario: Local JSON loaded on startup
- **WHEN** the config provider is initialized with a local JSON path
- **THEN** the provider SHALL load and parse the JSON file
- **AND** GetValue/GetSection SHALL return values from the local file when remote has not been fetched or has failed

#### Scenario: Fallback on remote failure
- **WHEN** the remote endpoint is configured and RefreshAsync fails (network error, timeout, non-2xx)
- **THEN** the provider SHALL retain the last successfully fetched remote config, or fall back to local
- **AND** the app SHALL continue to function with available config

### Requirement: Optional remote HTTP endpoint
A built-in provider SHALL support an optional remote HTTP endpoint for config fetch.

#### Scenario: Remote config overrides local
- **WHEN** RefreshAsync successfully fetches from the remote endpoint
- **THEN** the remote JSON SHALL be merged with local config (remote overrides local for matching keys)
- **AND** subsequent GetValue/GetSection SHALL return the merged values

#### Scenario: Remote endpoint format
- **WHEN** the remote endpoint is called
- **THEN** it SHALL return a JSON object (e.g., `{ "featureX": true, "apiUrl": "https://..." }`)
- **AND** the provider SHALL support configurable URL, headers, and timeout

### Requirement: Bridge-integrated config
The bridge SHALL expose config to JS so frontend code can read values without C# round-trips.

#### Scenario: JS can read config values
- **WHEN** the bridge is initialized
- **THEN** `window.agWebView.config` (or equivalent) SHALL expose `getValue(key)` and `getSection(key)`
- **AND** JS calls SHALL return the same values as C# GetValue/GetSection

#### Scenario: Config is read-only from JS
- **WHEN** JS attempts to modify config (e.g., set a value)
- **THEN** the API SHALL NOT support write operations from JS
- **AND** config updates SHALL occur only via C# (provider refresh, etc.)

### Requirement: Refresh API
The provider SHALL support async refresh to fetch latest config from remote.

#### Scenario: RefreshAsync fetches from remote
- **WHEN** C# calls `RefreshAsync()` on the provider
- **THEN** the provider SHALL attempt to fetch from the configured remote endpoint
- **AND** on success, SHALL update the merged config; on failure, SHALL retain previous state
