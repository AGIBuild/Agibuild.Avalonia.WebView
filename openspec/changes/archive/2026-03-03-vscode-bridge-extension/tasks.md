# VS Code Bridge Inspector — Tasks

## 1. Bridge Debug Protocol Design

- [x] 1.1 Define JSON message schema for `handshake`, `service-registry`, `call-start`, `call-end`, `call-error`
- [x] 1.2 Document protocol version field and handshake payload
- [x] 1.3 Add `BridgeOptions.DebugServerPort` and `BridgeOptions.EnableDebugServer` to design/spec

## 2. BridgeDebugServer Implementation

- [x] 2.1 Create `BridgeDebugServer` class implementing `IBridgeTracer`
- [x] 2.2 Implement WebSocket server (System.Net.WebSockets or equivalent) binding to 127.0.0.1
- [x] 2.3 Wire tracer callbacks to JSON message emission
- [x] 2.4 Send handshake on client connect
- [x] 2.5 Send service-registry on connect and on OnServiceExposed/OnServiceRemoved
- [x] 2.6 Integrate BridgeDebugServer into Fulora DI when EnableDebugServer is true
- [x] 2.7 Add unit tests: message serialization, tracer delegation, connection lifecycle

## 3. VS Code Extension Scaffold

- [x] 3.1 Create `tools/vscode-extension/` project with package.json, tsconfig
- [x] 3.2 Register extension ID `agibuild-fulora` and display name
- [x] 3.3 Implement activation entry point and command registration
- [x] 3.4 Add commands: `fulora.connect`, `fulora.disconnect`, `fulora.clear`

## 4. Bridge Inspector Panel

- [x] 4.1 Create WebView panel for Bridge Inspector
- [x] 4.2 Implement call feed UI (list with service, method, direction, duration, status)
- [x] 4.3 Implement call detail view (expandable params, result, error)
- [x] 4.4 Implement service registry view
- [x] 4.5 Implement latency histogram (per-service or global)
- [x] 4.6 Add empty/disconnected state UI
- [x] 4.7 Style error entries distinctly

## 5. Connection Management

- [x] 5.1 Implement WebSocket client for debug protocol
- [x] 5.2 Parse incoming JSON messages (handshake, service-registry, call-start, call-end, call-error)
- [x] 5.3 Implement Connect flow: discovery or manual URL input, connect, receive handshake
- [x] 5.4 Implement Disconnect flow
- [x] 5.5 Implement Clear flow (clear UI state, keep connection)
- [x] 5.6 Implement auto-discovery (well-known port range or mDNS)

## 6. Tests

- [x] 6.1 Unit tests: BridgeDebugServer tracer delegation, message format
- [x] 6.2 Unit tests: Extension WebSocket client message parsing
- [x] 6.3 Integration test: Fulora app with debug server → extension connects → bridge call → event appears in panel
- [x] 6.4 Document extension usage and debug server setup
