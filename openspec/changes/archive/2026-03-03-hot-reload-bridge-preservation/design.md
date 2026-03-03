# Hot Reload Bridge Preservation — Design

## Context

During SPA development with HMR (Hot Module Replacement), the page is updated in-place without a full reload. However, the JavaScript module graph is re-executed, which recreates the bridge client. Pending RPC calls are lost, event subscriptions are dropped, and the developer must manually re-trigger state (e.g., re-fetch data, re-subscribe to events). Preserving bridge state across HMR reloads would improve the dev loop. Goal: E3 (Hot Reload Integration).

**Existing contracts**: `@agibuild/bridge` npm package, `BridgeClient`, `BridgeRpc`, `createBridgeClient`, `RuntimeBridgeService`, Vite HMR API (`import.meta.hot`), webpack HMR API (`module.hot`).

## Goals / Non-Goals

### Goals

- Bridge client detects HMR reload events (Vite, webpack)
- On HMR dispose: serialize pending call queue and event subscriptions to sessionStorage
- On reconnect (after HMR apply): restore pending calls (re-invoke) and re-subscribe events
- Host-side bridge service preserves registration state (services remain exposed; no re-expose needed)
- `BridgeOptions.PreserveStateOnReload = true` (default in dev mode)
- Works with Vite and webpack HMR

### Non-Goals

- Full page reload preservation (only HMR; full reload clears state)
- C# hot reload integration
- Preserving in-memory JS state beyond bridge calls and subscriptions

## Decisions

### D1: HMR detection via module hot API

**Decision**: The bridge client SHALL detect HMR by checking for `import.meta.hot` (Vite) or `module.hot` (webpack). When `import.meta.hot?.dispose` or `module.hot?.dispose` is available, register a dispose callback to serialize state before the module is replaced.

**Rationale**: Both Vite and webpack expose a standard-ish HMR API. Vite uses `import.meta.hot`; webpack uses `module.hot`. The bridge package runs in the SPA context, so it has access to these. No build-time configuration required; runtime detection suffices.

### D2: State to preserve

**Decision**: Preserve (A) pending RPC call queue: calls that were in-flight when HMR triggered; (B) event subscriptions: service.eventName → callback references. Callbacks cannot be serialized; instead, store subscription metadata (service, event name) and rely on the app to re-register callbacks on reconnect. For pending calls: store method, params, and a correlation ID; on reconnect, re-invoke and resolve the original promise if the client instance is still "logical" (see D4).

**Rationale**: Pending calls can be re-invoked; the host is still running. Event callbacks are functions and cannot be serialized; the app must re-subscribe. We can provide a hook (e.g., `onHmrRestore`) for the app to re-register subscriptions. Alternatively: store only subscription keys; the bridge client's `use()` middleware or a restore hook can re-apply subscriptions from app state.

### D3: sessionStorage for serialization

**Decision**: Use `sessionStorage` with a key like `agibuild.bridge.hmr.state` to store serialized state. State SHALL be JSON-serializable: `{ pendingCalls: [{ id, method, params }], subscriptions: [{ service, event }] }`. Clear the key after successful restore to avoid stale state on next load.

**Rationale**: sessionStorage survives HMR (same tab, same origin). Survives module re-execution. Not shared across tabs. Size limit (~5MB) is sufficient for typical pending call counts and subscription metadata.

### D4: Pending call restoration semantics

**Decision**: Pending calls are stored with `{ id, method, params, timestamp }`. On reconnect, the bridge client SHALL re-invoke each pending call via `rpc.invoke(method, params)`. The original promises from the pre-HMR client are lost (the client instance was replaced). Instead: the restored client exposes a way for the app to await "restored" calls—e.g., `getRestoredPendingCalls()` returns a list of promises that resolve when the re-invoked calls complete. The app can use this to re-apply state (e.g., set React state from restored call results). Simpler alternative: just re-invoke and let the app's normal data flow handle it; if the app re-mounts and re-fetches, the re-invoked calls may be redundant. Document that "restore" means "re-invoke so host processes them"; the app is responsible for re-binding results to UI.

**Rationale**: Promise identity cannot be preserved across module re-execution. The cleanest approach: re-invoke pending calls, and either (a) provide a callback/event when restored calls complete, or (b) let the app re-fetch on mount. For event subscriptions: the app re-subscribes in a `useEffect` that runs on mount; if we fire a "bridge restored" event, the app can run its subscription logic. We'll provide `onBridgeRestored` or similar callback.

### D5: Host-side preservation

**Decision**: The host-side `RuntimeBridgeService` (and WebView) are NOT reloaded during HMR—only the JS in the WebView is. Therefore, exposed services remain registered. The bridge client reconnects to the same RPC; no host changes needed for "service stays exposed". The host may need to handle reconnection gracefully: if the client was mid-call, the host might have completed the call; the re-invoked call from the restored client would be a duplicate. We accept that: for idempotent calls (e.g., GetData), duplicate is fine; for non-idempotent, the app should avoid leaving such calls pending during HMR. Document this caveat.

**Rationale**: Host is C# process; HMR only affects the WebView's JS. Services stay exposed. Duplicate re-invocation is a minor risk; most dev-time calls are reads. For writes, the app can debounce or avoid re-invoking non-idempotent calls (e.g., by not storing them in pending state).

### D6: PreserveStateOnReload option

**Decision**: Add `PreserveStateOnReload: boolean` to `BridgeOptions` (or `createBridgeClient` options). Default: `true` in dev (when `import.meta.env?.DEV` or `process.env.NODE_ENV === 'development'`), `false` otherwise. When false, do not register HMR dispose or restore logic.

**Rationale**: Production builds typically don't use HMR; no need for preservation logic. Dev mode benefits from it. Explicit option allows override.

### D7: Event subscription restore

**Decision**: Store subscription metadata `{ service, event }`. On restore, the bridge client SHALL emit an event or invoke a callback (e.g., `onRestore(subscriptions)`) so the app can re-subscribe. The bridge client does NOT store callback references; the app registers them. Alternatively: if the app uses a pattern where subscriptions are registered in a single place (e.g., a `useBridgeSubscriptions` hook), that hook can re-run on `bridgeRestored` event. The bridge client provides `emit('bridgeRestored', { subscriptions })` or similar.

**Rationale**: Callbacks cannot be serialized. The app owns subscription logic. We provide the restore signal; the app re-subscribes.

## Risks / Trade-offs

### R1: Duplicate re-invocation

**Risk**: Re-invoking pending calls may duplicate side effects (e.g., CreateOrder called twice).

**Mitigation**: Document that non-idempotent calls should be avoided in pending state, or not stored. Consider filtering: only restore "read" methods (heuristic: Get*, Load*, Fetch*). Configurable allow-list is complex; document best practices.

### R2: Stale state in sessionStorage

**Risk**: If restore fails or is skipped, sessionStorage may hold stale state that affects future loads.

**Mitigation**: Clear sessionStorage key after successful restore. On next full load, key is empty. Add TTL (e.g., 1 minute) to stored state; ignore if expired.

### R3: webpack vs Vite API differences

**Risk**: webpack's `module.hot` and Vite's `import.meta.hot` have slightly different APIs.

**Mitigation**: Abstract behind a small adapter; implement both. Test with Vite and webpack templates.
