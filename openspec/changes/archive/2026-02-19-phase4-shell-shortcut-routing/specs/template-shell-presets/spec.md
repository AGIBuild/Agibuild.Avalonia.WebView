## ADDED Requirements

### Requirement: App-shell preset wires shortcut routing with deterministic lifecycle cleanup
When app-shell preset is selected, generated desktop host SHALL wire a reusable shortcut router to window key input and detach it during preset disposal.

#### Scenario: App-shell preset enables default shell shortcuts
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop shell preset code initializes shortcut routing and handles mapped shortcuts through router execution

#### Scenario: App-shell preset detaches shortcut handler on disposal
- **WHEN** generated desktop host unloads and shell preset is disposed
- **THEN** key input handler for shortcut routing is detached deterministically
