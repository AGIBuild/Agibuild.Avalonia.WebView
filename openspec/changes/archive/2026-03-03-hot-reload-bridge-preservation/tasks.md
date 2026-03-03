# Hot Reload Bridge Preservation — Tasks

## 1. HMR Detection

- [x] 1.1 Create HMR adapter abstraction: `detectHmrApi()` returns Vite or webpack hot API or null
- [x] 1.2 Implement Vite detection: check `import.meta.hot`
- [x] 1.3 Implement webpack detection: check `module.hot`
- [x] 1.4 Add `PreserveStateOnReload` to `createBridgeClient` options; default from `import.meta.env?.DEV` or `process.env.NODE_ENV`
- [x] 1.5 When PreserveStateOnReload is false, skip all HMR registration

## 2. State Serialization

- [x] 2.1 Define state shape: `{ pendingCalls: [{ id, method, params, timestamp }], subscriptions: [{ service, event }] }`
- [x] 2.2 Implement `serializeBridgeState()`: collect pending calls from internal queue (need to track in-flight calls)
- [x] 2.3 Implement `serializeSubscriptions()`: collect subscription metadata from event registry
- [x] 2.4 Add pending call tracking to bridge client: wrap `invoke` to record in-flight calls with id, method, params
- [x] 2.5 On invoke resolve/reject, remove from pending set
- [x] 2.6 Implement `writeStateToSessionStorage(state)`: JSON.stringify and write to key `agibuild.bridge.hmr.state`
- [x] 2.6 Register dispose callback: call serialize + writeStateToSessionStorage when HMR dispose fires

## 3. State Restoration

- [x] 3.1 Implement `readStateFromSessionStorage()`: read and parse JSON from key
- [x] 3.2 Implement `clearStateFromSessionStorage()`: remove key after restore
- [x] 3.3 On bridge client init (after HMR apply): check for stored state
- [x] 3.4 If state exists: re-invoke each pending call via `rpc.invoke(method, params)`
- [x] 3.5 Provide `onBridgeRestored` callback or `bridgeRestored` event with `{ pendingCallPromises, subscriptions }` so app can re-bind results and re-subscribe
- [x] 3.6 Clear sessionStorage after restore
- [x] 3.7 Add TTL check: if state timestamp is older than e.g. 60 seconds, skip restore and clear

## 4. Event Subscription Restore

- [x] 4.1 Ensure subscription metadata (service, event) is stored (no callbacks)
- [x] 4.2 On restore, include subscriptions in `onBridgeRestored` payload
- [x] 4.3 Document that app must re-call `service.onEvent(callback)` in response to restore signal
- [x] 4.4 (Optional) Provide helper `useBridgeRestoreEffect` or similar for React apps to re-subscribe on restore

## 5. RuntimeBridgeService Reconnect Handling

- [x] 5.1 Verify RuntimeBridgeService does not clear registrations on WebView navigation or script reload (it should not; services are process-level)
- [x] 5.2 If any reconnect handshake is needed (e.g., client sends "restored" message), add minimal support
- [x] 5.3 Document that host requires no changes; services persist automatically

## 6. Integration & Configuration

- [x] 6.1 Wire PreserveStateOnReload into createBridgeClient
- [x] 6.2 Add `onBridgeRestored?: (ctx: { pendingCallPromises, subscriptions }) => void` to options
- [x] 6.3 Export types for restore context
- [x] 6.4 Update @agibuild/bridge README with HMR preservation usage and caveats

## 7. Tests

- [x] 7.1 Unit tests: serializeBridgeState produces valid JSON with pendingCalls and subscriptions
- [x] 7.2 Unit tests: readStateFromSessionStorage + clear round-trip
- [x] 7.3 Unit tests: PreserveStateOnReload false skips HMR registration
- [x] 7.4 Unit tests: Mock HMR dispose triggers serialization
- [x] 7.5 Unit tests: Restore re-invokes pending calls (mock rpc.invoke)
- [x] 7.6 Integration test: Vite HMR cycle with bridge client — state survives (manual or E2E)
- [x] 7.7 Integration test: webpack HMR cycle (if webpack template exists)
