# Web Worker Bridge — Tasks

## 1. Main Thread Relay

- [x] 1.1 Add relay registration API: `registerWorkerBridgeRelay()` or equivalent that sets up a listener for `fulora:worker:init` messages
- [x] 1.2 Implement relay handler: receive MessagePort from worker, store in worker registry (worker ID → port)
- [x] 1.3 Implement request handler: on relay request message, call `BridgeRpc.invoke(method, params)`, correlate by `id`
- [x] 1.4 Implement response posting: when host returns, post `{ jsonrpc: "2.0", id, result }` or `{ id, error }` to the correct worker port
- [x] 1.5 Support multiple workers: maintain map of port → pending requests; route responses to correct port
- [x] 1.6 Handle port close/error: reject all pending requests for that worker, clean up registry
- [x] 1.7 Integrate relay registration into host initialization (e.g., when bridge is ready, register relay)

## 2. WorkerBridgeClient

- [x] 2.1 Create `WorkerBridgeClient` class/interface in `packages/bridge/src/worker-bridge.ts`
- [x] 2.2 Implement handshake: create MessageChannel, post `fulora:worker:init` with port to main, await `fulora:worker:ready`
- [x] 2.3 Implement `ready()`: perform handshake, resolve when ready or reject on timeout
- [x] 2.4 Implement `invoke()`: post request with unique ID, return promise that resolves when response with matching ID arrives
- [x] 2.5 Implement `getService()`: return proxy that maps method calls to `invoke("ServiceName.methodName", params)`
- [x] 2.6 Implement `use()`: register middleware; apply middleware in worker context around relay round-trip
- [x] 2.7 Add request ID generation (UUID or monotonic) and response correlation
- [x] 2.8 Add configurable handshake timeout (default 5000ms)
- [x] 2.9 Export `createWorkerBridgeClient()` factory and `WorkerBridgeClient` type from `packages/bridge/src/index.ts`

## 3. Relay Protocol

- [x] 3.1 Define relay message envelope: `{ jsonrpc: "2.0", id: string, method: string, params?: object }` for requests
- [x] 3.2 Define response envelope: `{ jsonrpc: "2.0", id: string, result?: unknown }` and `{ jsonrpc: "2.0", id: string, error: { code, message, data? } }`
- [x] 3.3 Define handshake messages: `fulora:worker:init` (worker → main, with port), `fulora:worker:ready` (main → worker)
- [x] 3.4 Document protocol in design.md or a dedicated protocol doc
- [x] 3.5 Ensure params and results are JSON-serializable (structured clone compatible)

## 4. Bridge Source Generator (Worker Stubs)

- [x] 4.1 Extend bridge source generator to emit worker-compatible output (e.g., `worker-bridge.g.ts` or separate target)
- [x] 4.2 Worker stubs SHALL accept `WorkerBridgeClient` (or `BridgeClient | WorkerBridgeClient`) for service getters
- [x] 4.3 Worker stubs SHALL provide same typed methods as main-thread stubs
- [x] 4.4 Add generator option or convention to enable worker stub emission (e.g., `EmitWorkerStubs=true`)
- [x] 4.5 Ensure worker stubs are included in build output and consumable from worker entry point

## 5. @agibuild/bridge Package Updates

- [x] 5.1 Add `packages/bridge/src/worker-bridge.ts` with WorkerBridgeClient implementation
- [x] 5.2 Export `createWorkerBridgeClient`, `WorkerBridgeClient` from `packages/bridge/src/index.ts`
- [x] 5.3 Update `package.json` exports if needed (e.g., subpath `@agibuild/bridge/worker`)
- [x] 5.4 Update `dist/` build to include worker-bridge in output
- [x] 5.5 Add JSDoc/TSDoc for WorkerBridgeClient API

## 6. Host-Side Relay Integration

- [x] 6.1 Determine where relay registration is invoked (e.g., WebView message handler, bridge init callback)
- [x] 6.2 Implement main-thread script injection or handler that listens for worker init and sets up relay
- [x] 6.3 Ensure relay has access to `BridgeRpc` (e.g., `window.agWebView.rpc`)
- [x] 6.4 Document how host apps enable worker bridge (e.g., opt-in flag, automatic when bridge is used)

## 7. Integration Tests

- [x] 7.1 Create integration test: spawn Web Worker, WorkerBridgeClient.ready(), invoke bridge method, verify result
- [x] 7.2 Test error propagation: host throws → worker receives rejected promise with error message
- [x] 7.3 Test concurrent calls: worker sends 3 requests, verify all 3 resolve with correct results
- [x] 7.4 Test multiple workers: two workers each make bridge calls, verify no cross-delivery
- [x] 7.5 Test handshake timeout: relay not ready, worker.ready() rejects after timeout
- [x] 7.6 Test getService from worker: typed service proxy, method call returns correct result
- [x] 7.7 Test middleware in worker: use(loggingMiddleware), verify middleware runs on worker bridge calls
