# Web Worker Bridge — Spec

## Purpose

Define BDD-style requirements for bridge call support from Web Workers via main-thread relay. Covers WorkerBridgeClient API, MessagePort relay, request/response correlation, handshake, error propagation, and multiple worker support. Enables computation offload to workers while maintaining type-safe bridge access.

## Requirements

### Requirement: WorkerBridgeClient exposes same API as main-thread BridgeClient

`WorkerBridgeClient` SHALL expose `ready()`, `invoke()`, `getService()`, and `use()` with semantics equivalent to the main-thread `BridgeClient`.

#### Scenario: WorkerBridgeClient.ready waits for handshake
- **GIVEN** a Web Worker that will use the bridge
- **WHEN** the worker creates a `WorkerBridgeClient` and calls `await client.ready()`
- **THEN** the client SHALL establish a MessagePort connection to the main thread
- **AND** SHALL complete the handshake (`fulora:worker:init` / `fulora:worker:ready`)
- **AND** SHALL resolve the promise when the relay is ready
- **AND** SHALL reject if the handshake fails or times out

#### Scenario: WorkerBridgeClient.invoke calls bridge via relay
- **GIVEN** a `WorkerBridgeClient` that has completed `ready()`
- **WHEN** the worker calls `await client.invoke("AppService.GetData", { id: 1 })`
- **THEN** the client SHALL post a relay request to the main thread via MessagePort
- **AND** the main-thread relay SHALL call `BridgeRpc.invoke("AppService.GetData", { id: 1 })`
- **AND** the Host C# SHALL process the call and return a result
- **AND** the result SHALL be posted back to the worker
- **AND** the worker's promise SHALL resolve with the result

#### Scenario: WorkerBridgeClient.getService returns typed proxy
- **GIVEN** a `WorkerBridgeClient` that has completed `ready()`
- **WHEN** the worker calls `client.getService<IAppService>("AppService")` and invokes a method
- **THEN** the proxy SHALL behave like main-thread `getService`: method calls SHALL map to `invoke("ServiceName.methodName", params)`
- **AND** the call SHALL be relayed through the main thread to the host
- **AND** the returned promise SHALL resolve with the method result

#### Scenario: WorkerBridgeClient.use registers middleware
- **GIVEN** a `WorkerBridgeClient`
- **WHEN** the worker calls `client.use(middleware)` with a `BridgeMiddleware`
- **THEN** the middleware SHALL be applied to bridge calls made from the worker
- **AND** middleware SHALL run in the worker context (before/after relay round-trip)
- **AND** SHALL follow the same middleware contract as main-thread `BridgeClient.use`

---

### Requirement: Main-thread relay forwards Worker requests to BridgeRpc

The main thread SHALL register a relay that receives Worker requests via MessagePort and forwards them to `BridgeRpc.invoke`.

#### Scenario: Relay receives request and invokes bridge
- **GIVEN** the main thread has registered the worker bridge relay
- **WHEN** the worker posts a relay request `{ jsonrpc: "2.0", id: "req-1", method: "DbService.Query", params: { sql: "SELECT 1" } }`
- **THEN** the relay SHALL call `rpc.invoke("DbService.Query", { sql: "SELECT 1" })`
- **AND** SHALL await the result
- **AND** SHALL post a response `{ jsonrpc: "2.0", id: "req-1", result: [...] }` back to the worker

#### Scenario: Relay correlates responses to requests by ID
- **GIVEN** the worker has sent multiple concurrent requests (req-1, req-2, req-3)
- **WHEN** responses arrive from the host in arbitrary order
- **THEN** the relay SHALL match each response to the correct request by `id`
- **AND** SHALL post each response to the worker with the matching `id`
- **AND** the worker SHALL resolve the correct promise for each response

#### Scenario: Relay supports multiple workers
- **GIVEN** two Web Workers (A and B) have each completed the handshake
- **WHEN** Worker A sends a request and Worker B sends a request
- **THEN** the relay SHALL route each request to the host independently
- **AND** SHALL route each response back to the correct worker (A or B)
- **AND** responses SHALL NOT be cross-delivered

---

### Requirement: Error handling propagates host errors to worker

Errors from the host (C# exceptions, bridge rejections) SHALL be propagated to the worker with actionable information.

#### Scenario: Host exception returns error response
- **GIVEN** a bridge call that throws an exception on the host (e.g., database error)
- **WHEN** the relay receives the error
- **THEN** the relay SHALL post `{ jsonrpc: "2.0", id, error: { code, message, data? } }` to the worker
- **AND** the worker SHALL reject the corresponding promise
- **AND** the rejected error SHALL include the `message` and optionally `data` for debugging

#### Scenario: Bridge not available returns clear error
- **GIVEN** the main thread relay is registered but `BridgeRpc` is not yet available (e.g., host not ready)
- **WHEN** the worker sends a bridge request
- **THEN** the relay SHALL post an error response with a message indicating bridge unavailability
- **AND** the worker's promise SHALL reject with that error

#### Scenario: Port closed rejects pending requests
- **GIVEN** the worker has pending bridge requests
- **WHEN** the MessagePort is closed (e.g., worker terminated)
- **THEN** all pending requests SHALL be rejected with an appropriate error
- **AND** the relay SHALL clean up pending state for that worker

---

### Requirement: Worker initialization handshake establishes relay

The worker and main thread SHALL perform a handshake to establish the MessagePort and verify relay readiness.

#### Scenario: Handshake completes successfully
- **GIVEN** the main thread has set up the worker bridge relay listener
- **WHEN** the worker posts `{ type: "fulora:worker:init", port: MessagePort }` (transferring the port)
- **THEN** the main thread SHALL receive the port and register it for relay
- **AND** SHALL post `{ type: "fulora:worker:ready" }` (or equivalent) back to the worker
- **AND** the worker SHALL consider the handshake complete and allow bridge calls

#### Scenario: Handshake timeout fails gracefully
- **GIVEN** the main thread relay is not ready (e.g., bridge not initialized)
- **WHEN** the worker sends the init message and waits for ready
- **THEN** if the ready message does not arrive within a configurable timeout (e.g., 5s)
- **AND** the worker SHALL reject `ready()` with a timeout error
- **AND** SHALL NOT allow bridge calls until handshake succeeds

---

### Requirement: Bridge source generator emits worker-compatible stubs

The bridge source generator SHALL emit worker-compatible TypeScript stubs alongside main-thread stubs.

#### Scenario: Generated worker stubs use WorkerBridgeClient
- **GIVEN** a C# project with `[JsExport]` interfaces
- **WHEN** the bridge source generator runs
- **THEN** it SHALL emit worker-compatible stubs (e.g., `worker-bridge.d.ts`, `worker-bridge.ts` or equivalent)
- **AND** the stubs SHALL accept a `WorkerBridgeClient` (or factory) instead of `BridgeClient`
- **AND** the stubs SHALL provide the same typed service methods as main-thread stubs

#### Scenario: Worker can use generated types with WorkerBridgeClient
- **GIVEN** generated worker stubs for `IAppService`
- **WHEN** the worker calls `getAppService(workerBridgeClient).getData({ id: 1 })`
- **THEN** the call SHALL be typed (params and return type)
- **AND** SHALL behave identically to main-thread usage except for the relay hop
