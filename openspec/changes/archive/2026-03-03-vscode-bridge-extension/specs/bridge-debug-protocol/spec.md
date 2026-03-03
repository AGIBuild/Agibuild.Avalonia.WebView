## Purpose

Define the WebSocket-based bridge debug protocol and `BridgeDebugServer` component for streaming bridge tracer events to external tools (e.g. VS Code extension).

## Requirements

### Requirement: BridgeDebugServer implements IBridgeTracer

`BridgeDebugServer` SHALL implement `IBridgeTracer` and SHALL forward all tracer callbacks to connected WebSocket clients as JSON messages.

#### Scenario: Export call events are streamed
- **WHEN** a JS→C# method invocation occurs and a client is connected
- **THEN** the server SHALL send `call-start` (direction: export) followed by `call-end` or `call-error` with service name, method name, params, result/error, and elapsed time

#### Scenario: Import call events are streamed
- **WHEN** a C#→JS method invocation occurs and a client is connected
- **THEN** the server SHALL send `call-start` (direction: import) followed by `call-end` with service name, method name, params, and elapsed time

#### Scenario: Service lifecycle events are streamed
- **WHEN** a service is exposed or removed and a client is connected
- **THEN** the server SHALL send `service-registry` (or equivalent) reflecting the current registry state

### Requirement: Server sends handshake on client connect

The server SHALL send a `handshake` message to each client upon WebSocket connection, including protocol version and app metadata.

#### Scenario: Client receives handshake after connect
- **WHEN** a client connects to the debug server WebSocket endpoint
- **THEN** the server SHALL send a `handshake` message with protocol version (e.g. `"version": "1"`) and optional app name/identifier

### Requirement: Server sends service registry on connect and on changes

The server SHALL send the current service registry (list of exposed services and their methods) when a client connects, and SHALL send updates when services are exposed or removed.

#### Scenario: New client receives full registry
- **WHEN** a client connects and services are already exposed
- **THEN** the server SHALL send a `service-registry` message with all exposed services and their method counts

#### Scenario: Registry update on service exposed
- **WHEN** a new service is exposed while a client is connected
- **THEN** the server SHALL send a `service-registry` (or delta) message reflecting the new service

#### Scenario: Registry update on service removed
- **WHEN** a service is removed while a client is connected
- **THEN** the server SHALL send a `service-registry` (or delta) message reflecting the removal

### Requirement: Debug server is opt-in and configurable

The debug server SHALL be disabled by default and SHALL be enabled via `BridgeOptions.EnableDebugServer = true`. The port SHALL be configurable (e.g. `BridgeOptions.DebugServerPort`).

#### Scenario: Server does not start when disabled
- **WHEN** `EnableDebugServer` is false (default)
- **THEN** no WebSocket server SHALL be started and no tracer SHALL be registered for debug streaming

#### Scenario: Server binds to configured port
- **WHEN** `EnableDebugServer` is true and a port is configured
- **THEN** the server SHALL listen on `ws://127.0.0.1:{port}`

### Requirement: Server binds to localhost only

The server SHALL bind to `127.0.0.1` only. It SHALL NOT accept connections from non-localhost addresses.

#### Scenario: Localhost-only binding
- **WHEN** the debug server is started
- **THEN** it SHALL bind to `127.0.0.1` and SHALL NOT be reachable from other machines on the network

### Requirement: Protocol messages are JSON

All protocol messages SHALL be valid JSON objects with a `type` field identifying the message kind.

#### Scenario: Call-start message structure
- **WHEN** an export or import call starts
- **THEN** the server SHALL send a JSON object with `type: "call-start"`, `serviceName`, `methodName`, `direction` (export/import), `paramsJson`, and `timestamp`

#### Scenario: Call-end message structure
- **WHEN** an export or import call completes successfully
- **THEN** the server SHALL send a JSON object with `type: "call-end"`, `serviceName`, `methodName`, `direction`, `elapsedMs`, `resultType` (export only), and `timestamp`

#### Scenario: Call-error message structure
- **WHEN** an export call fails
- **THEN** the server SHALL send a JSON object with `type: "call-error"`, `serviceName`, `methodName`, `elapsedMs`, `error` (message and optional stack), and `timestamp`
