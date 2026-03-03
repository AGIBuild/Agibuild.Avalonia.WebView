## Why

Fulora's bridge call tracing (`IBridgeTracer`, DevTools panel overlay) outputs to logs or an in-WebView panel. Developers cannot observe bridge traffic in their IDE. For complex hybrid apps with many bridge services, tracing calls requires switching between the IDE and the app's DevTools overlay or parsing log files. A VS Code extension that visualizes live bridge calls, latency, and errors in a sidebar panel would significantly reduce debugging friction.

**Goal alignment**: Phase 11 M11.4 (IDE Extensions). Advances E2 (Dev Tooling) by bringing bridge observability into the developer's primary workspace.

## What Changes

- Create a VS Code extension `agibuild-fulora` published to the VS Code marketplace
- Implement a **Bridge Inspector** sidebar panel that connects to a running Fulora app via a lightweight debug protocol (WebSocket) and displays:
  - Live call feed: service.method, direction (C#→JS / JS→C#), duration, status
  - Call detail view: parameters, return value, error details
  - Service registry: list of exposed services and their methods
  - Latency histogram: per-service call latency distribution
- Add a `BridgeDebugServer` component to Fulora runtime that:
  - Listens on a configurable local port (default: off, opt-in via `BridgeOptions.EnableDebugServer = true`)
  - Streams bridge tracer events as JSON over WebSocket
  - Exposes service registry metadata on connect
- Provide VS Code commands: `Fulora: Connect to App`, `Fulora: Disconnect`, `Fulora: Clear Call Log`
- Extension auto-discovers local Fulora apps via a well-known port range or mDNS

## Capabilities

### New Capabilities
- `bridge-debug-protocol`: WebSocket-based debug protocol for streaming bridge tracer events and service registry to external tools
- `vscode-bridge-inspector`: VS Code extension with sidebar panel for live bridge call visualization

### Modified Capabilities
- `bridge-tracing`: Add debug server emission mode alongside existing log/overlay modes

## Non-goals

- Rider/IntelliJ extension — VS Code first, Rider can follow the same debug protocol later
- Remote debugging across network — localhost only for security
- Breakpoint/step-through debugging — this is observability, not a debugger
- Editing bridge interfaces from the extension — read-only visualization

## Impact

- New project: `tools/vscode-extension/` (TypeScript, VS Code Extension API)
- New runtime component: `BridgeDebugServer` in `Agibuild.Fulora.Runtime`
- Modified: `BridgeOptions` (new `EnableDebugServer` property)
- Dependencies: VS Code Extension API, WebSocket (System.Net.WebSockets on C# side)
- Publish: VS Code marketplace (`agibuild-fulora`), npm for extension packaging
