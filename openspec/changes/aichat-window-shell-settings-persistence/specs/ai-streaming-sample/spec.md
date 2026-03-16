## ADDED Requirements

### Requirement: AI chat sample appearance settings SHALL persist across app restarts
The desktop AI chat sample SHALL persist host window appearance settings so user-applied transparency/theme preferences remain stable after relaunch.

#### Scenario: User saves appearance settings
- **WHEN** web UI calls `updateWindowShellSettings` and host successfully applies settings
- **THEN** the sample host SHALL persist the applied/normalized `WindowShellSettings` to local storage

#### Scenario: App starts with persisted settings
- **WHEN** desktop sample starts and persisted settings file exists
- **THEN** host SHALL load the persisted settings and apply them before SPA bootstrap
- **AND** initial shell appearance state returned to web SHALL reflect persisted values

#### Scenario: Persisted settings are invalid
- **WHEN** persisted settings file is missing, unreadable, or invalid JSON
- **THEN** sample SHALL fall back to default settings without startup failure
