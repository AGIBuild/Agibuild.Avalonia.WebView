# Web Worker Bridge — Design

## Context

Fulora's bridge is currently accessible only from the main thread. Web Workers cannot call `window.agWebView.rpc` because they lack access to the host-injected RPC. Heavy computation (data processing, crypto, image manipulation) run in workers blocks the main thread if workers must post results back for bridge calls. Extending bridge access to Web Workers via a main-thread relay enables computation offload while preserving type safety and the existing bridge protocol. Goal: Phase 12 M12.3.

**Existing contracts**: `BridgeClient`, `BridgeRpc`, `createBridgeClient`, `@agibuild/bridge` npm package, bridge source generator (emits typed stubs for main-thread consumption).

## Goals / Non-Goals

### Goals

- Enable bridge calls from Web Workers via main-thread relay
- Preserve the same typed API surface as main-thread `BridgeClient` (getService, invoke, ready)
- Use MessagePort for Worker ↔ Main communication (structured clone, no serialization overhead for transferables)
- Request/response correlation via unique request IDs
- Reuse existing JSON-RPC protocol semantics for relay messages
- Worker initialization handshake to establish MessagePort and verify relay readiness
- Propagate errors from Host C# through Main to Worker with full stack/context

### Non-Goals

- SharedWorker support in v1
- ServiceWorker bridge
- Direct Worker → Host bypass (must relay through main thread; host RPC is main-thread bound)

## Decisions

### D1: MessagePort-based relay

**Decision**: Use `MessagePort` for Worker ↔ Main communication. The main thread creates a `MessageChannel`, passes one port to the worker via `postMessage`, and retains the other for relay logic. Worker sends bridge requests on its port; main receives, forwards to `BridgeRpc.invoke`, and posts responses back.

**Rationale**: MessagePort is the standard mechanism for worker communication. Structured clone handles JSON-serializable params and results. Transferables (ArrayBuffer, etc.) can be passed without copy when needed. No need for SharedArrayBuffer or Atomics for this use case.

### D2: Request/response correlation via ID

**Decision**: Each relay request SHALL carry a unique string ID (e.g., UUID or monotonic counter). The main-thread relay SHALL store a pending promise map keyed by ID. When the Host C# response arrives, the relay SHALL resolve the corresponding promise and post the response (including ID) back to the worker. The worker SHALL match responses to requests by ID.

**Rationale**: Bridge calls are async; multiple concurrent calls from a worker must be correlated correctly. ID-based correlation is simple and avoids reordering bugs. Same pattern as JSON-RPC 2.0 `id` field.

### D3: Same JSON-RPC protocol for relay messages

**Decision**: Relay messages SHALL use a JSON-RPC 2.0–like envelope: `{ jsonrpc: "2.0", id: string, method: string, params?: object }` for requests; `{ jsonrpc: "2.0", id: string, result?: unknown, error?: { code: number, message: string, data?: unknown } }` for responses. The main-thread relay SHALL translate Worker request → `BridgeRpc.invoke(method, params)` → Host C# → translate response → post to Worker.

**Rationale**: Aligns with existing bridge protocol semantics. Method names (e.g., `AppService.GetData`) and params structure remain unchanged. Worker sees the same logical API as main thread.

### D4: Worker initialization handshake

**Decision**: The main thread SHALL register a relay handler that listens for a `"fulora:worker:init"` message. The worker SHALL post this message with a `MessagePort` (from a `MessageChannel` it creates). The main thread SHALL receive the port, register it as the worker's relay target, and post `"fulora:worker:ready"` back. WorkerBridgeClient SHALL `await` this handshake before allowing any bridge calls.

**Rationale**: Ensures the relay is ready before the worker sends bridge requests. The worker owns the MessageChannel; it passes one port to main, keeping the other for its own use. This avoids race conditions where the worker sends a request before the main thread has set up the relay.

### D5: Error propagation

**Decision**: Errors from Host C# (exceptions, bridge rejections) SHALL be serialized and sent to the worker as `{ jsonrpc: "2.0", id, error: { code, message, data } }`. The worker SHALL reject the corresponding promise with a `BridgeError` (or equivalent) that preserves the message and optional `data`. Unhandled relay errors (e.g., port closed) SHALL reject all pending requests with a generic error.

**Rationale**: Workers need actionable error information for debugging. Preserving `message` and `data` allows JS to surface structured errors. Port closure (e.g., worker terminated) should fail all pending calls cleanly.

### D6: WorkerBridgeClient API parity with BridgeClient

**Decision**: `WorkerBridgeClient` SHALL expose `ready()`, `invoke()`, `getService()`, and `use()`. The `ready()` SHALL wait for the handshake. `invoke()` and `getService()` SHALL post requests via the MessagePort and return promises that resolve when the response arrives. `use()` SHALL support middleware; middleware runs in the worker context before/after the relay round-trip.

**Rationale**: Developers expect the same API whether on main or worker. Middleware (e.g., logging, retry) in the worker is useful for worker-specific concerns.

## Risks / Trade-offs

### R1: MessagePort lifecycle

**Risk**: If the worker is terminated or the port is closed, pending requests will hang or fail. The main thread may not detect worker termination immediately.

**Mitigation**: Document that worker termination invalidates the bridge. Consider adding a `port.onclose` or `port.onerror` handler to reject pending requests. Use `AbortController` for long-running calls if needed.

### R2: Relay registration scope

**Risk**: Multiple workers could register with the same main thread. The relay must support multiple worker ports and route responses to the correct worker.

**Mitigation**: Maintain a map of worker ID → MessagePort. Each worker gets a unique ID at handshake. Responses include the worker ID (or the port is implicitly the routing key—each port has its own pending map).

### R3: Main-thread relay overhead

**Risk**: Every worker bridge call adds a main-thread hop. For high-frequency calls, this could add latency.

**Mitigation**: Acceptable for the typical use case (offload heavy computation; occasional bridge calls for storage, notifications). Document that worker bridge is for compute-heavy workloads, not high-frequency UI updates.
