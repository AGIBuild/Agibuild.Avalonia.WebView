## Purpose

Define the VS Code extension `agibuild-fulora` with Bridge Inspector sidebar panel for live bridge call visualization, service registry display, and latency metrics.

## Requirements

### Requirement: Extension provides Bridge Inspector panel

The extension SHALL provide a Bridge Inspector panel implemented as a WebView that displays live bridge call feed, call detail, service registry, and latency histogram.

#### Scenario: Panel opens on command
- **WHEN** the user runs the `Fulora: Open Bridge Inspector` (or equivalent) command
- **THEN** the extension SHALL open a WebView panel in the sidebar or editor area showing the Bridge Inspector UI

#### Scenario: Panel shows empty state when disconnected
- **WHEN** the panel is open and no connection to a Fulora app exists
- **THEN** the panel SHALL display a prompt to connect (e.g. "Connect to a Fulora app to view bridge calls")

### Requirement: Live call feed displays bridge calls

The panel SHALL display a chronological list of bridge calls with service name, method name, direction (C#→JS / JS→C#), duration, and status.

#### Scenario: Export call appears in feed
- **WHEN** a JS→C# call completes and the extension is connected
- **THEN** the feed SHALL show an entry with direction "JS→C#", service name, method name, duration, and success status

#### Scenario: Import call appears in feed
- **WHEN** a C#→JS call completes and the extension is connected
- **THEN** the feed SHALL show an entry with direction "C#→JS", service name, method name, and duration

#### Scenario: Failed call is visually distinct
- **WHEN** a bridge call fails and the extension is connected
- **THEN** the feed SHALL show the failed entry with distinct styling (e.g. error icon, red indicator)

### Requirement: Call detail view shows params and result/error

The panel SHALL allow inspection of request (params) and response (result or error) for each bridge call.

#### Scenario: Selecting a call shows detail
- **WHEN** the user selects or expands a bridge call entry in the feed
- **THEN** the panel SHALL display the request params (JSON) and result or error details for that call

### Requirement: Service registry view lists exposed services

The panel SHALL display a list of exposed bridge services and their methods.

#### Scenario: Registry updates on connect
- **WHEN** the extension connects to a Fulora app
- **THEN** the panel SHALL display the service registry (service names and method counts) received in the handshake or service-registry message

#### Scenario: Registry updates when services change
- **WHEN** a service is exposed or removed while connected
- **THEN** the panel SHALL update the registry view to reflect the change

### Requirement: Latency histogram shows per-service distribution

The panel SHALL display a latency histogram (or equivalent visualization) showing call latency distribution per service.

#### Scenario: Histogram updates with call data
- **WHEN** bridge calls complete and the extension is connected
- **THEN** the histogram SHALL update to include the elapsed times, grouped or filterable by service

### Requirement: Connect, Disconnect, Clear commands

The extension SHALL provide commands: `Fulora: Connect to App`, `Fulora: Disconnect`, `Fulora: Clear Call Log`.

#### Scenario: Connect command establishes connection
- **WHEN** the user runs `Fulora: Connect to App` and selects a target (e.g. from discovered apps or manual URL)
- **THEN** the extension SHALL open a WebSocket connection to the selected Fulora app's debug server and SHALL begin receiving events

#### Scenario: Disconnect command closes connection
- **WHEN** the user runs `Fulora: Disconnect` while connected
- **THEN** the extension SHALL close the WebSocket connection and SHALL update the panel to show disconnected state

#### Scenario: Clear command clears displayed calls
- **WHEN** the user runs `Fulora: Clear Call Log`
- **THEN** the extension SHALL clear the displayed call feed (and optionally reset the histogram) without disconnecting

### Requirement: Auto-discovery of local Fulora apps

The extension SHALL support auto-discovery of local Fulora apps (e.g. via well-known port range or mDNS) to simplify connection.

#### Scenario: Discovered apps are offered for connection
- **WHEN** the user runs Connect and local Fulora apps are discoverable
- **THEN** the extension SHALL present a list of discovered apps (e.g. by port or app name) for the user to select

#### Scenario: Manual connection when no discovery
- **WHEN** no apps are discovered or discovery is disabled
- **THEN** the extension SHALL allow the user to enter a manual WebSocket URL (e.g. `ws://localhost:9876`)
